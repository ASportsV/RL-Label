using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using Unity.MLAgents;

public class StudentGroups : MonoBehaviour
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
        //public RVOplayer player;
    }

    public List<Student> students = new List<Student>();
    private Dictionary<int, RVOplayer> m_playerMap = new Dictionary<int, RVOplayer>();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        cam = transform.parent.Find("Camera").GetComponent<Camera>();
        
        bool movingCam = Academy.Instance.EnvironmentParameters.GetWithDefault("movingCam", 0.0f) == 1.0f;
        if(movingCam) 
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

        if(cam.GetComponent<MovingCamera>()) {
            var tmp = cam.transform.forward;
            cam.transform.LookAt(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
            maxZInCam = cam.WorldToViewportPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ)).z;
            cam.transform.forward = tmp;
        }

        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + "), old max: " + cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z);

        LoadPosInTrack();

        for (int i = 0, len = students.Count; i < len; ++i)
        {
            var student = students[i];
            if (currentStep == student.startStep)
            {
                CreatePlayerLabelFromPos(i, student.positions[0]);
            }
        }

        currentStep = 0;
    }

    public void CreatePlayerLabelFromPos(int sid, Vector3 pos)
    {

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
        player.velocities = students[sid].velocities;
        player.positions = students[sid].positions;

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

            for (int i = 0, len = students.Count; i < len; ++i)
            {
                var student = students[i];
                if (currentStep == student.startStep)
                {
                    CreatePlayerLabelFromPos(i, student.positions[0]);
                }
                else if (currentStep > student.startStep && currentStep < (student.startStep + student.totalStep))
                {
                    m_playerMap[i].step(currentStep - student.startStep);
                }
                else if (currentStep >= (student.startStep + student.totalStep) && m_playerMap.ContainsKey(i))
                {
                    // remove
                    var go = m_playerMap[i].gameObject;
                    Destroy(go);
                    m_playerMap.Remove(i);
                }
            }
        }



    }

    private void LoadPosInTrack()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, "student_all.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();

        string[] records = pos_data.Split('\n');

        List<List<Vector3>> students = new List<List<Vector3>>();
        List<float> startedTime = new List<float>();

        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            float startTime = float.Parse(array[0]);
            int playerIdx = int.Parse(array[1]);
            float px = float.Parse(array[2]);
            float py = float.Parse(array[3]);

            if((playerIdx + 1) > students.Count)
            {
                students.Add(new List<Vector3>());
                startedTime.Add(startTime);
            }
            students[playerIdx].Add(new Vector3(px, 0.5f, py));
        }

        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        List<Vector3[]> velocities = new List<Vector3[]>();
        for(int sIdx = 0; sIdx < students.Count; ++sIdx)
        {
            var positions = students[sIdx];
            Vector3[] vel = new Vector3[positions.Count];
            for(int i = 0; i < positions.Count-1; ++i)
            {
                Vector3 cur = positions[i];
                Vector3 next = positions[i + 1];
                vel[i] = (next - cur) / timeStep;

                maxVel = new Vector3(Mathf.Max(vel[i].x, maxVel.x), 0, Mathf.Max(vel[i].z, maxVel.z));
                minVel = new Vector3(Mathf.Min(vel[i].x, minVel.x), 0, Mathf.Min(vel[i].z, minVel.z));
            }
            vel[positions.Count - 1] = vel[positions.Count - 2];
            velocities.Add(vel);
        }

        for(int sIdx = 0; sIdx < students.Count; ++sIdx)
        {
            var positions = students[sIdx];
            var vel = velocities[sIdx];
            var studentStruct = new Student();
            int totalStep = vel.Count();

            studentStruct.positions = positions.ToArray();
            studentStruct.velocities = vel;
            studentStruct.totalStep = totalStep;
            studentStruct.startStep = (int) (startedTime[sIdx] / timeStep);
            this.students.Add(studentStruct);
        }

        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;
    }
}
