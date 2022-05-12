using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

public struct PlayerData
{
    public int id;
    public Vector3[] positions;
    public Vector3[] velocities;
    public int totalStep;
    public int startStep;
}

public struct Metrics
{
    public int trackId;
    public List<string> occludedObjPerStep; // sid
    public List<string> intersectedObjPerStep;
    public List<string> labelPositions;
    public List<string> targetPositions;

}

public abstract class PlayerGroup : MonoBehaviour
{
    protected abstract string sceneName { get; }
    protected abstract string dataFileName { get; }

    protected RVOSettings m_RVOSettings;
    // player + label
    public GameObject playerLabel_prefab, playerLabel_prefab_rl, playerLabel_baseline_prefab;
    public Sprite redLabel, blueLabel;

    private int _currentStep = 0;
    public int currentStep {
        get { return _currentStep; }
        set {
            _currentStep = value;
            transform.parent.Find("Canvas_2/Panel/Slider").GetComponent<Slider>().value = totalStep != 0 ? ((float)_currentStep / (float)totalStep) : 0;
        }
    }
    public int currentScene;
    [HideInInspector] public string root;


    protected Queue<int> trainingTrack;
    protected Queue<int> doneTrack = new Queue<int>();

    public List<List<PlayerData>> scenes = new List<List<PlayerData>>();
    protected Dictionary<int, RVOplayer> m_playerMap = new Dictionary<int, RVOplayer>();
    protected int numOfAgent = 10;
    protected HashSet<int> agentSet = new HashSet<int>();
    protected float time = 0.0f;
    protected float timeStep = 0.04f;
    public int totalStep = 0;

    StringLogSideChannel stringChannel;
    public NNModel brain;

    // baseline
    protected bool useBaseline => m_RVOSettings.CurrentTech == Tech.Opti;
    public BaselineForce b;

    abstract protected (int, int, int, float, float) parseRecord(string record);
    abstract protected void LoadParameters();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        root = "player";
        Camera cam = transform.parent.Find("Camera").GetComponent<Camera>();
        //court = transform.parent.Find("fancy_court");
        if (m_RVOSettings.evaluate && m_RVOSettings.evaluate_metrics)
        {
            stringChannel = new StringLogSideChannel();
            SideChannelManager.RegisterSideChannel(stringChannel);
        }
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;

        bool onlyTestMC = Academy.Instance.EnvironmentParameters.GetWithDefault("onlyTestMC", 0.0f) == 1.0f;
        bool movingCam = Academy.Instance.EnvironmentParameters.GetWithDefault("movingCam", 0.0f) == 1.0f;
        GameObject camGo = transform.parent.Find("Camera").gameObject;
        if (onlyTestMC ? (m_RVOSettings.evaluate && movingCam) : movingCam)
        {
            camGo.AddComponent<MovingCamera>();
        }

        // geometry min and max
        m_RVOSettings.sceneName = sceneName;
        m_RVOSettings.minZInCam = Mathf.Abs(camGo.transform.localPosition.z - -m_RVOSettings.courtZ);
        var tmp = camGo.transform.forward;
        var cornerInWorld = camGo.transform.parent.TransformPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
        camGo.transform.LookAt(cornerInWorld);
        m_RVOSettings.maxZInCam = camGo.transform.InverseTransformPoint(cornerInWorld).z; //cam.WorldToViewportPoint(cornerInWorld).z;
        camGo.transform.forward = tmp;

        Debug.Log("Min and Max Z in Cam: (" + m_RVOSettings.minZInCam.ToString() + "," + m_RVOSettings.maxZInCam.ToString() + ")");

        LoadDataset();
        EnvironmentReset();
        //gameObject.SetActive(false);
    }

    private void EnvironmentReset()
    {
        LoadParameters();
        LoadTrack(getNextTrack());
    }

    protected void LoadDataset()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, dataFileName);
        string pos_data;
#if UNITY_EDITOR || !UNITY_ANDROID
        StreamReader r = new StreamReader(fileName);
        pos_data = r.ReadToEnd();
#else
    // streamingAssets are compressed in android (not readable with File).
        WWW reader = new WWW (fileName);
        while (!reader.isDone) {}
        pos_data = reader.text;
#endif
        string[] records = pos_data.Split('\n');

        List<Dictionary<int, List<Vector3>>> tracks = new List<Dictionary<int, List<Vector3>>>();
        Dictionary<string, int> playerStartedInTrack = new Dictionary<string, int>();
        for (int i = 0; i < records.Length; ++i)
        {

            var (trackIdx, currentStep, playerIdx, px, py) = parseRecord(records[i]);

            if ((trackIdx + 1) > tracks.Count) tracks.Add(new Dictionary<int, List<Vector3>>());
            var track = tracks[trackIdx];

            if (!track.ContainsKey(playerIdx))
            {
                track[playerIdx] = new List<Vector3>();
                playerStartedInTrack[trackIdx.ToString() + '_' + playerIdx.ToString()] = currentStep;
            }
            var playerPos = track[playerIdx];
            playerPos.Add(new Vector3(px, 0.5f, py));
        }

        // set max speed
        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        for (int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            List<PlayerData> track = new List<PlayerData>();
            // int minIdxInthisTrack = tracks[tIdx].Keys.Min();

            int eIdx = 0;
            foreach (var entry in tracks[tIdx].OrderBy(t => t.Key))
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
                if (pos.Length > 1)
                    vel[pos.Length - 1] = vel[pos.Length - 2];

                PlayerData student = new PlayerData();
                student.id = eIdx++; // playerIdx - minIdxInthisTrack;
                student.positions = pos;
                student.velocities = vel;
                student.startStep = playerStartedInTrack[tIdx.ToString() + '_' + playerIdx.ToString()];
                student.totalStep = pos.Length;
                track.Add(student);
            }
            this.scenes.Add(track);
        }

        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = Mathf.Max(Mathf.Abs(maxVel.x), Mathf.Abs(minVel.x)); //  maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = Mathf.Max(Mathf.Abs(maxVel.z), Mathf.Abs(minVel.z)); // maxVel.z - minVel.z;
    }

    public virtual void LoadTrack(int sceneIdx)
    {
        Clean();
        currentScene = sceneIdx;
        currentStep = 0;
        numOfAgent = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("num_agents", 10);
        var players = scenes[currentScene];

        int agentIdx = Random.Range(0, players.Count());
        var rnd = new System.Random();

        var startedPlayers = players
            .Where(s => s.startStep == currentStep)
            .OrderBy(_ => rnd.Next());

        List<GameObject> labelGroups = new List<GameObject>(),
            labels = new List<GameObject>();
        List<bool> isAgents = new List<bool>();

        foreach (var student in startedPlayers)
        {
            var playerLab = CreatePlayerLabelFromPos(student, agentSet.Count() < numOfAgent);
            isAgents.Add(playerLab.Item1);
            labelGroups.Add(playerLab.Item2);
            labels.Add(playerLab.Item3);
        }

        totalStep = players.Max(s => s.startStep + s.totalStep);

        if (useBaseline)
        {
            b.InitFrom(labelGroups, labels, isAgents);
        }
    }

    protected int getNextTrack()
    {
        int nextTask = currentScene;
        var queue = m_RVOSettings.evaluate ? m_RVOSettings.testingTrack : trainingTrack;
        if (queue.Count <= 0)
        {
            if (m_RVOSettings.evaluate)
            {
                m_RVOSettings.FinishACourt();
                m_RVOSettings.testingTrack = doneTrack;
            }
            else trainingTrack = doneTrack;
            doneTrack = new Queue<int>();
            queue = m_RVOSettings.evaluate ? m_RVOSettings.testingTrack : trainingTrack;
        }

        nextTask = queue.Dequeue();
        doneTrack.Enqueue(nextTask);
        return nextTask;
    }

    protected (bool, GameObject, GameObject) CreatePlayerLabelFromPos(PlayerData student, bool isAgent)
    {
        int sid = student.id;
        var pos = student.positions[0];
        var agentSetting = m_RVOSettings.CurrentTask.setting.Find(t => t.id == sid);
        isAgent = true; //agentSetting.isAgent;
        GameObject toInstantiate = isAgent
            ? useBaseline ? playerLabel_baseline_prefab : playerLabel_prefab_rl
            : playerLabel_prefab;
        GameObject playerObj = Instantiate(toInstantiate, pos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";
        playerObj.SetActive(true);

        RVOplayer player = playerObj.GetComponent<RVOplayer>();
        player.Init(sid, (!isAgent && useBaseline) ? "player" : root, student.positions, student.velocities);
        m_playerMap[sid] = player;
        // add to set
        if (isAgent) agentSet.Add(sid);

        Transform label = playerObj.gameObject.transform.Find("label");
        label.localPosition = new Vector3(0f, m_RVOSettings.labelY, 0f);

        // label data
        var cell = label.Find("panel/Player_info/q11").GetComponent<TMPro.TextMeshProUGUI>();
        cell.text = agentSetting.point[0].ToString();
        cell = label.Find("panel/Player_info/q12").GetComponent<TMPro.TextMeshProUGUI>();
        cell.text = agentSetting.point[1].ToString();
        cell = label.Find("panel/Player_info/q21").GetComponent<TMPro.TextMeshProUGUI>();
        cell.text = agentSetting.point[2].ToString();
        cell = label.Find("panel/Player_info/q22").GetComponent<TMPro.TextMeshProUGUI>();
        cell.text = agentSetting.point[3].ToString();

        if (useBaseline && isAgent)
        {
            playerObj.GetComponentInChildren<LabelIdHandler>().sId = sid;
        }

        // set color
        var iamge = label.Find("panel/Player_info").GetComponent<Image>();
        //iamge.sprite = (sid % 2 == 0) ? blueLabel : redLabel;
        iamge.sprite = agentSetting.color == "blue" ? blueLabel : redLabel;

        //if (sid % 2 != 0)
        if (agentSetting.color == "red")
        {
            Color color = new Color(239f / 255f, 83f / 255f, 80f / 255f);
            var cubeRenderer = player.player.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", color);
        }

        return (isAgent, playerObj, label.gameObject);
    }

    protected void TrackFinished()
    {
        if (m_RVOSettings.evaluate && m_RVOSettings.evaluate_metrics)
        {
            SaveMetricToJson();
            Academy.Instance.StatsRecorder.Add("_test/_track_end", currentScene);
        }
        LoadTrack(currentScene);
    }

    protected virtual void Clean()
    {
        b.CleanUp();
        // remove all existing
        foreach (var entry in m_playerMap)
        {
            var p = entry.Value;
            // if the player still exit, sync
            if (p.gameObject.activeSelf) p.GetComponentInChildren<RVOLabelAgent>()?.SyncReset();
            // else, ensure the agent is active
            else p.transform.GetChild(1).gameObject.SetActive(true);

            p.GetComponentInChildren<Label>()?.cleanMetrics();
            Destroy(p.gameObject);
        }
        agentSet.Clear();
        m_playerMap.Clear();
    }

    protected void SaveMetricToJson()
    {
        var players = scenes[currentScene];
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
                var labelAgent = m_playerMap[student.id].gameObject.GetComponentInChildren<Label>();

                occluded.UnionWith(labelAgent.occludedObjectOverTime[i - student.startStep]);
                intersected.UnionWith(labelAgent.intersectionsOverTime[i - student.startStep]);
            }
            accumulatedOcclusion.Add(occluded);
            accumulatedIntersection.Add(intersected);
        }

        List<string> labelPositions = new List<string>();
        List<string> targetPositions = new List<string>();
        foreach (var student in players)
        {
            var labelAgent = m_playerMap[student.id].GetComponentInChildren<Label>();
            labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y + "," + v.z));
            targetPositions.AddRange(labelAgent.targetPosOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d.x + "," + d.y + "," + d.z));
        }

        Metrics met = new Metrics();
        met.trackId = currentScene;
        met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
        met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
        met.labelPositions = labelPositions;
        met.targetPositions = targetPositions;

        stringChannel.SendMetricsToPython(JsonUtility.ToJson(met));

        // save 
        //using (StreamWriter writer = new StreamWriter(sceneName + "_track" + currentScene + "_met.json", false))
        //{
        //    writer.Write(JsonUtility.ToJson(met));
        //    writer.Close();
        //}
    }
}
