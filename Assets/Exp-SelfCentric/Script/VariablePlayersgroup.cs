using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using Unity.MLAgents;

public class VariablePlayersgroup : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    // player + label
    public GameObject playerLabel_prefab;
    public Sprite redLabel;
    public Sprite blueLabel;

    public Transform court;
    public Camera cam;
    float minZInCam;
    float maxZInCam;
    public int currentStep = 0;

    public struct Student
    {
        public Vector3[] positions;
        public Vector3[] velocities;
        public int totalStep;
        public int startStep;
    }

    public int currentTrack;
    public List<List<Student>> tracks = new List<List<Student>>();
    int[] testingTrack = { };
    Queue<int> trainingTrack;

    private Dictionary<int, RVOplayer> m_playerMap = new Dictionary<int, RVOplayer>();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        cam = transform.parent.Find("Camera").GetComponent<Camera>();

        bool movingCam = Academy.Instance.EnvironmentParameters.GetWithDefault("movingCam", 0.0f) == 1.0f;
        if (movingCam)
        {
            cam.gameObject.AddComponent<MovingCamera>();
        }
        court = transform.parent.Find("fancy_court");
    }

    // Start is called before the first frame update
    void Start()
    {
        minZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, -m_RVOSettings.courtZ)).z;
        maxZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z;

        if (cam.GetComponent<MovingCamera>())
        {
            var tmp = cam.transform.forward;
            cam.transform.LookAt(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
            maxZInCam = cam.WorldToViewportPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ)).z;
            cam.transform.forward = tmp;
        }

        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + "), old max: " + cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z);

        LoadPosInTrack();

        var rnd = new System.Random();
        trainingTrack = new Queue<int>(Enumerable.Range(0, tracks.Count)
            .Where(i => !testingTrack.Contains(i))
            .OrderBy(item => rnd.Next())
            .ToList());
        currentStep = 0;

        currentTrack = trainingTrack.Dequeue();
        trainingTrack.Enqueue(currentTrack);
        LoadTrack(currentTrack);
    }

    public void CreatePlayerLabelFromPos(int sid, Student student)
    {

        var pos = student.positions[0];
        GameObject playerObj = Instantiate(playerLabel_prefab, pos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";

        var text = playerObj.transform.Find("player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();
        text = playerObj.transform.Find("player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();

        RVOplayer player = playerObj.GetComponent<RVOplayer>();

        player.sid = sid;
        m_playerMap[sid] = player;
        player.velocities = student.velocities;
        player.positions = student.positions;

        Transform label = playerObj.gameObject.transform.Find("label");
        label.name = sid + "_label";

        Debug.Log("Finish initialize " + label.name);
        var name = label.Find("panel/Player_info/Name").GetComponent<TMPro.TextMeshProUGUI>();
        name.text = Random.Range(10, 99).ToString();
        var iamge = label.Find("panel/Player_info").GetComponent<Image>();

        RVOLabelAgent agent = player.GetComponentInChildren<RVOLabelAgent>();
        agent.PlayerLabel = player;
        agent.court = court;
        agent.cam = cam;
        agent.minZInCam = minZInCam;
        agent.maxZInCam = maxZInCam;


        iamge.sprite = (sid % 2 == 0) ? blueLabel : redLabel;

        if (sid % 2 != 0)
        {
            Color color = new Color(239f / 255f, 83f / 255f, 80f / 255f);
            var cubeRenderer = player.player.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", color);
        }
    }

    private float time = 0.0f;
    private float timeStep = 0.04f;
    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;
        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;

            var students = tracks[currentTrack];

            for (int i = 0, len = students.Count; i < len; ++i)
            {
                var student = students[i];
                if (currentStep == student.startStep)
                {
                    CreatePlayerLabelFromPos(i, student);
                }
                else if (currentStep > student.startStep && currentStep < (student.startStep + student.totalStep))
                {
                    m_playerMap[i].step(currentStep - student.startStep);
                }
                else if (currentStep >= (student.startStep + student.totalStep) && m_playerMap.ContainsKey(i))
                {
                    // remove
                    var go = m_playerMap[i].gameObject;
                    go.GetComponentInChildren<RVOLabelAgent>().SyncReset();
                    Destroy(go);
                    m_playerMap.Remove(i);
                }
            }

            if (currentStep >= students.Max(s => s.startStep + s.totalStep))
            {
                // reset all 
                currentStep = 0;
                // load another track
                currentTrack = trainingTrack.Dequeue();
                trainingTrack.Enqueue(currentTrack);
                LoadTrack(currentTrack);
            }
        }
    }

    void LoadTrack(int idx)
    {
        if (idx >= tracks.Count) Debug.LogWarning("Idx " + idx + " out of tracks range");
        currentStep = 0;

        // remove all existing
        foreach (var entry in m_playerMap)
        {
            var p = entry.Value;
            m_playerMap.Remove(entry.Key);
            p.GetComponentInChildren<RVOLabelAgent>().SyncReset();
            Destroy(p.gameObject);
        }

        var students = tracks[idx];
        for (int i = 0, len = students.Count; i < len; ++i)
        {
            var student = students[i];
            if (currentStep == student.startStep)
            {
                CreatePlayerLabelFromPos(i, student);
            }
        }
    }


    private void LoadPosInTrack()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, "student_full.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();

        string[] records = pos_data.Split('\n');
        List<Dictionary<int, List<Vector3>>> tracks = new List<Dictionary<int, List<Vector3>>>();
        List<int> trackStarted = new List<int>();
        Dictionary<string, int> playerStartedInTrack = new Dictionary<string, int>();
        
        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int trackIdx = int.Parse(array[0]);
            int currentStep = int.Parse(array[1]);
            int playerIdx = int.Parse(array[2]);
            float px = float.Parse(array[3]);
            float py = float.Parse(array[4]);

            if ((trackIdx + 1) > tracks.Count)
            {
                tracks.Add(new Dictionary<int, List<Vector3>>());
                trackStarted.Add(i);
            }

            var track = tracks[trackIdx];
            if (!track.ContainsKey(playerIdx))
            {
                track[playerIdx] = new List<Vector3>();
                playerStartedInTrack[trackIdx.ToString() + '_' + playerIdx.ToString()] = currentStep;
            }
            var playerPos = track[playerIdx];
            playerPos.Add(new Vector3(px, 0.5f, py));
        }

        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        for(int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            List<Student> track = new List<Student>();
            foreach(var entry in tracks[tIdx])
            {
                int playerIdx = entry.Key;
                Vector3[] pos = entry.Value.ToArray();
                Vector3[] vel = new Vector3[pos.Length];
                for (int i = 0; i < pos.Length - 1; ++i)
                {
                    Vector3 cur = pos[i];
                    Vector3 next = pos[i + 1];
                    vel[i] = (next - cur) / timeStep;

                    maxVel = new Vector3(Mathf.Max(vel[i].x, maxVel.x), 0, Mathf.Max(vel[i].z, maxVel.z));
                    minVel = new Vector3(Mathf.Min(vel[i].x, minVel.x), 0, Mathf.Min(vel[i].z, minVel.z));

                }
                if(pos.Length > 1)
                    vel[pos.Length - 1] = vel[pos.Length - 2];

                Student student = new Student();
                student.positions = pos;
                student.velocities = vel;
                student.startStep = playerStartedInTrack[tIdx.ToString() + '_' + playerIdx.ToString()];
                student.totalStep = pos.Length;
                track.Add(student);
            }
            this.tracks.Add(track);
        }

        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;
    }
}
