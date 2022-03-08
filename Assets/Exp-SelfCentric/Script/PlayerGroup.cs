using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.UI;

public struct Student
{
    public int id;
    public Vector3[] positions;
    public Vector3[] velocities;
    public int totalStep;
    public int startStep;
}

public abstract class PlayerGroup : MonoBehaviour
{

    protected RVOSettings m_RVOSettings;
    // player + label
    public GameObject playerLabel_prefab_rl;
    public Sprite redLabel;
    public Sprite blueLabel;

    public Transform court;
    public Camera cam;
    protected float minZInCam;
    protected float maxZInCam;
    public int currentStep = 0;
    public int currentScene;

    [HideInInspector] public string root;


    protected Queue<int> testingTrack;
    protected Queue<int> trainingTrack;

    public List<List<Student>> scenes = new List<List<Student>>();
    protected Dictionary<int, RVOplayer> m_playerMap = new Dictionary<int, RVOplayer>();

    protected float time = 0.0f;
    protected float timeStep = 0.04f;

    abstract protected void LoadTasks();
    abstract protected void LoadDataset();
    abstract public void LoadScene(int sceneIdx);

    private void Awake()
    {
        root = "player";
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        cam = transform.parent.Find("Camera").GetComponent<Camera>();
        court = transform.parent.Find("fancy_court");

        bool movingCam = Academy.Instance.EnvironmentParameters.GetWithDefault("movingCam", 0.0f) == 1.0f;
        if (movingCam)
        {
            cam.gameObject.AddComponent<MovingCamera>();
        }

        // geometry min and max
        minZInCam = Mathf.Abs(cam.transform.localPosition.z - -m_RVOSettings.courtZ);
        var tmp = cam.transform.forward;
        cam.transform.LookAt(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
        maxZInCam = cam.WorldToViewportPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ)).z;
        cam.transform.forward = tmp;

        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + "), old max: " + cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z);

        LoadDataset();
        LoadTasks();
    }

    protected int getNextTask()
    {
        int nextTask = currentScene;
        var queue = m_RVOSettings.evaluate ? testingTrack : trainingTrack;
        if (queue.Count > 0) nextTask = queue.Dequeue();
        // for trainiing
        if (!m_RVOSettings.evaluate) queue.Enqueue(nextTask);

        return nextTask;
    }

    protected (GameObject, GameObject) CreatePlayerLabelFromPos(Student student)
    {
        int sid = student.id;
        var pos = student.positions[0];
        GameObject toInstantiate = playerLabel_prefab_rl;
        GameObject playerObj = Instantiate(toInstantiate, pos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";

        RVOplayer player = playerObj.GetComponent<RVOplayer>();
        player.root = root;
        player.Init(sid);
        player.positions = student.positions;
        player.velocities = student.velocities;
        m_playerMap[sid] = player;

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
        return (playerObj, label.gameObject);
    }

    protected virtual void Clean()
    {
        // remove all existing
        foreach (var entry in m_playerMap)
        {
            var p = entry.Value;
            // if the player still exit, sync
            if (p.gameObject.activeSelf) p.GetComponentInChildren<RVOLabelAgent>()?.SyncReset();
            // else, ensure the agent is active
            else p.transform.GetChild(1).gameObject.SetActive(true);
  
            p.GetComponentInChildren<RVOLabelAgent>()?.cleanMetrics();
            Destroy(p.gameObject);
        }

        m_playerMap.Clear();
    }

    protected void SaveMetricToJson(string prefix, int totalStep, List<Student> players)
    {
        // collect the intersection, occlusions over time
        List<HashSet<string>> accumulatedOcclusion = new List<HashSet<string>>();
        List<HashSet<string>> accumulatedIntersection = new List<HashSet<string>>();
        foreach (var entry in m_playerMap)
        {
            foreach (Transform child in entry.Value.transform)
                child.gameObject.SetActive(true);
        }

        for (int i = 0; i < totalStep; ++i)
        {
            var occluded = new HashSet<string>();
            var intersected = new HashSet<string>();

            foreach (var student in players.Where(s => i >= s.startStep && i < (s.startStep + s.totalStep)))
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
        foreach (var student in players)
        {
            var labelAgent = m_playerMap[student.id].GetComponentInChildren<RVOLabelAgent>();
            labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y));
            labelDistToTarget.AddRange(labelAgent.distToTargetOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d));
            // Debug.Log("Occ Step of " + student.id + " is " + labelAgent.occludedObjectOverTime.Count + " / " + student.totalStep);
        }
    
        Metrics met = new Metrics();
        met.trackId = currentScene;
        met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
        met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
        met.labelPositions = labelPositions;
        met.labelDistToTarget = labelDistToTarget;

        // save 
        using (StreamWriter writer = new StreamWriter(prefix + "_track" + currentScene + "_met.json", false))
        {
            writer.Write(JsonUtility.ToJson(met));
            writer.Close();
        }
    }
}