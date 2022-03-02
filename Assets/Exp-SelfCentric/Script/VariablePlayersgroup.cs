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
        public int id;
        public Vector3[] positions;
        public Vector3[] velocities;
        public int totalStep;
        public int startStep;
    }

    public List<List<Student>> tracks = new List<List<Student>>();
  
    public int currentTrack;
    Queue<int> testingTrack = new Queue<int>(new[] { 1, 3, 10, 11, 19, 20});
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
        // geometry min and max
        minZInCam = Mathf.Abs(cam.transform.localPosition.z - -m_RVOSettings.courtZ);
        var tmp = cam.transform.forward;
        cam.transform.LookAt(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
        maxZInCam = cam.WorldToViewportPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ)).z;
        cam.transform.forward = tmp;

        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + "), old max: " + cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z);

        LoadPosInTrack();

        var rnd = new System.Random();
        trainingTrack = new Queue<int>(Enumerable.Range(0, tracks.Count)
            .Where(i => !testingTrack.Contains(i))
            .OrderBy(item => rnd.Next())
            .ToList());

        LoadTrack();
    }

    public void CreatePlayerLabelFromPos(Student student)
    {
        int sid = student.id;
        var pos = student.positions[0];
        GameObject playerObj = Instantiate(playerLabel_prefab, pos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";

        var text = playerObj.transform.Find("player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = playerObj.transform.GetSiblingIndex().ToString(); //sid.ToString();
        text = playerObj.transform.Find("player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = playerObj.transform.GetSiblingIndex().ToString();

        RVOplayer player = playerObj.GetComponent<RVOplayer>();

        player.sid = sid;
        m_playerMap[sid] = player;
        player.velocities = student.velocities;
        player.positions = student.positions;

        Transform label = playerObj.gameObject.transform.Find("label");
        label.name = sid + "_label";

        //Debug.Log("Finish initialize " + label.name);
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
            int totalStep = students.Max(s => s.startStep + s.totalStep);

            //for (int i = 0, len = students.Count; i < len; ++i)
            foreach(var student in students)
            {
                //var student = students[i];
                if (currentStep == student.startStep)
                {
                    CreatePlayerLabelFromPos(student);
                }
                else if (currentStep > student.startStep && currentStep < (student.startStep + student.totalStep))
                {
                    m_playerMap[student.id].step(currentStep - student.startStep);
                }
                else if (currentStep >= (student.startStep + student.totalStep) && m_playerMap[student.id].gameObject.activeSelf)
                {
                    // deactivate
                    var go = m_playerMap[student.id].gameObject;
                    var labelAgent = go.GetComponentInChildren<RVOLabelAgent>();
                    labelAgent.SyncReset();
                    go.SetActive(false);
                    foreach (Transform child in go.transform)
                        child.gameObject.SetActive(false);
                }
            }

            if (currentStep >= totalStep)
            {
                if (m_RVOSettings.evaluate)
                {
                    // should calculate the metrix, including occlution rate, intersection rate, distance to the target, moving distance relative to the target

                    // collect the intersection, occlusions over time
                    List<HashSet<string>> accumulatedOcclusion = new List<HashSet<string>>();
                    List<HashSet<string>> accumulatedIntersection = new List<HashSet<string>>();
                    foreach(var entry in m_playerMap)
                    {
                        foreach (Transform child in entry.Value.transform)
                            child.gameObject.SetActive(true);
                    }

                    for (int i = 0; i < totalStep; ++i)
                    {
                        var occluded = new HashSet<string>();
                        var intersected = new HashSet<string>();
 
                        foreach (var student in students.Where(s => i >= s.startStep && i < (s.startStep + s.totalStep)))
                        {
                            var labelAgent = m_playerMap[student.id].gameObject.GetComponentInChildren<RVOLabelAgent>();

                            occluded.UnionWith(labelAgent.occludedObjectOverTime[i - student.startStep]);
                            intersected.UnionWith(labelAgent.intersectionsOverTime[i - student.startStep]);
                        }
                        accumulatedOcclusion.Add(occluded);
                        accumulatedIntersection.Add(intersected);
                    }

                    List<string> labelPositions = new List<string>();
                    List<string> labelDistToTarget = new List<string>();
                    foreach(var student in students)
                    {
                        var labelAgent = m_playerMap[student.id].GetComponentInChildren<RVOLabelAgent>();
                        labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y));
                        labelDistToTarget.AddRange(labelAgent.distToTargetOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d));
                        Debug.Log("Occ Step of " + student.id + " is " + labelAgent.occludedObjectOverTime.Count + " / " + student.totalStep);

                    }

                    // collect
                    Metrics met = new Metrics();
                    met.trackId = currentTrack;
                    met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
                    met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
                    met.labelPositions = labelPositions;
                    met.labelDistToTarget = labelDistToTarget;

                    // save 
                    using (StreamWriter writer = new StreamWriter("student_track" + currentTrack + "_met.json", false))
                    {
                        writer.Write(JsonUtility.ToJson(met));
                        writer.Close();
                    }
                }

                LoadTrack();
            }
        }
    }

    void LoadTrack()
    {
        var queue = m_RVOSettings.evaluate ? testingTrack : trainingTrack;
        if (queue.Count > 0)
            currentTrack = queue.Dequeue();
        // for trainiing
        if (!m_RVOSettings.evaluate) queue.Enqueue(currentTrack);

        int idx = currentTrack;
        if (idx >= tracks.Count) Debug.LogWarning("Idx " + idx + " out of tracks range");
        currentStep = 0;

        // remove all existing
        foreach (var entry in m_playerMap.Where(p => p.Value.gameObject.activeSelf))
        {
            var p = entry.Value;
            p.GetComponentInChildren<RVOLabelAgent>().SyncReset();
            Destroy(p.gameObject);
        }
        m_playerMap.Clear();

        var students = tracks[idx];
        for (int i = 0, len = students.Count; i < len; ++i)
        {
            var student = students[i];
            if (currentStep == student.startStep)
            {
                CreatePlayerLabelFromPos(student);
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
                student.id = playerIdx;
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
