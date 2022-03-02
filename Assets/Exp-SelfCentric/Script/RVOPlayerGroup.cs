using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;
using Unity.MLAgents;

public class RVOPlayerGroup : MonoBehaviour
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
    public int currentTrack;
    Queue<int> testingTrack = new Queue<int>(new[] { 0, 5, 13, 15, 21, 22 });
    Queue<int> trainingTrack;

    public struct Track
    {
        public Dictionary<int, Vector3[]> positions;
        public Dictionary<int, Vector3[]> velocities;
        public int totalStep;
    }

    public List<Track> tracks = new List<Track>();


    private List<RVOplayer> m_playerMap = new List<RVOplayer>();

    struct Metrics
    {
        public int trackId;
        public List<string> occludedObjPerStep; // sid
        public List<string> intersectedObjPerStep;
        public List<string> labelPositions;
        public List<string> labelDistToTarget;
    }


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

    void LoadTrack()
    {
        var queue = m_RVOSettings.evaluate ? testingTrack : trainingTrack;
        if(queue.Count > 0)
            currentTrack = queue.Dequeue();
        // for trainiing
        if (!m_RVOSettings.evaluate) queue.Enqueue(currentTrack);

        int idx = currentTrack;
        if (idx >= tracks.Count) Debug.LogWarning("Idx " + idx + " out of tracks range");
        var track = tracks[idx];
        int numPlayers = track.positions.Count;
        int curNumPlayers = m_playerMap.Count;

        if(numPlayers > curNumPlayers)
        {
            // should spawn
            for (int i = curNumPlayers; i < numPlayers; ++i) 
                CreatePlayerLabelFromPos(i, track.positions[i][0]);
        }
        else if (numPlayers < curNumPlayers)
        {
            // should destory
            for(int i = m_playerMap.Count -1; i >= numPlayers; --i)
            {
                var p = m_playerMap[i];
                m_playerMap.RemoveAt(i);
                Destroy(p.gameObject);
            }
        }

        for(int i = 0, len = m_playerMap.Count; i < len; ++i)
        {
            var player = m_playerMap[i];
            player.positions = track.positions[player.sid];
            player.velocities = track.velocities[player.sid];
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
        m_playerMap.Add(player);

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
    Dictionary<int, Metrics> metricsPerTrack = new Dictionary<int, Metrics>();

    private void FixedUpdate()
    {
        time += Time.fixedDeltaTime;

        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }

        if(currentStep < tracks[currentTrack].totalStep)
        {
            m_playerMap.ForEach(p => p.step(currentStep));
        }
        else
        {
            if(m_RVOSettings.evaluate)
            {
                // should calculate the metrix, including occlution rate, intersection rate, distance to the target, moving distance relative to the target
                var labelAgents = m_playerMap.Select(p => p.GetComponentInChildren<RVOLabelAgent>());

                // collect the intersection, occlusions over time
                List<HashSet<string>> accumulatedOcclusion = new List<HashSet<string>>();
                List<HashSet<string>> accumulatedIntersection = new List<HashSet<string>>();

                for(int i = 0; i < tracks[currentTrack].totalStep; ++i)
                {
                    var occluded = new HashSet<string>();
                    var intersected = new HashSet<string>();
                    foreach (var labelAgent in labelAgents)
                    {
                        occluded.UnionWith(labelAgent.occludedObjectOverTime[i]);
                        intersected.UnionWith(labelAgent.intersectionsOverTime[i]);
                    }
                    accumulatedOcclusion.Add(occluded);
                    accumulatedIntersection.Add(intersected);   
                }

                List<string> labelPositions = new List<string>();
                List<string> labelDistToTarget = new List<string>();
                foreach (var labelAgent in labelAgents)
                {
                    labelPositions.AddRange(labelAgent.posOverTime.Select(v => labelAgent.PlayerLabel.sid + "," + v.x + "," + v.y));
                    labelDistToTarget.AddRange(labelAgent.distToTargetOverTime.Select(d => labelAgent.PlayerLabel.sid + "," + d));
                }

                // collect
                Metrics met = new Metrics();
                met.trackId = currentTrack;
                met.occludedObjPerStep = accumulatedOcclusion.Select(p => string.Join(',', p)).ToList();
                met.intersectedObjPerStep = accumulatedIntersection.Select(p => string.Join(',', p)).ToList();
                met.labelPositions = labelPositions;
                met.labelDistToTarget = labelDistToTarget;
                metricsPerTrack[currentTrack] = met;
                
                // save 
                using (StreamWriter writer = new StreamWriter("nba_track" + currentTrack + "_met.json", false))
                {
                    writer.Write(JsonUtility.ToJson(met));
                    writer.Close();
                }
            }
            
            // reset all 
            m_playerMap.ForEach(p => p.GetComponentInChildren<RVOLabelAgent>().SyncReset());
            // load another track
            LoadTrack();
        }
    }

    private void LoadPosInTrack()
    {
        string fileName = Path.Combine(Application.streamingAssetsPath, "nba_full_split.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();

        string[] records = pos_data.Split('\n');

        List<List<List<Vector3>>> tracks = new List<List<List<Vector3>>>();
        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int trackIdx = int.Parse(array[0]);
            int playerIdx = int.Parse(array[1]);
            float px = float.Parse(array[2]);
            float py = float.Parse(array[3]);

            if((trackIdx +1) > tracks.Count)
            {
                tracks.Add(new List<List<Vector3>>());
            }

            var track = tracks[trackIdx];
            if((playerIdx + 1) > track.Count)
            {
                track.Add(new List<Vector3>());
            }
            var playerPos = track[playerIdx];
            playerPos.Add(new Vector3(px, 0.5f, py));
        }

        // positions, velocitye, max velocity
        Vector3 maxVel = Vector3.zero;
        Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);

        for (int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            var track = tracks[tIdx];
            var trackStruct = new Track();
            int totalStep = track.Max(p => p.Count);
            Dictionary<int, Vector3[]> positions = track.Select((p, i) => new { i, p }).ToDictionary(x => x.i, x => x.p.ToArray());
            Dictionary<int, Vector3[]> velocities = new Dictionary<int, Vector3[]>();

            for(int pIdx = 0; pIdx < positions.Count; ++pIdx)
            {
                var pos = positions[pIdx];
                Vector3[] vel = new Vector3[pos.Length];
                for (int i = 0; i < pos.Length - 1; ++i)
                {
                    Vector3 cur = pos[i];
                    Vector3 next = pos[i + 1];
                    vel[i] = (next - cur) / timeStep;

                    maxVel = new Vector3(Mathf.Max(vel[i].x, maxVel.x), 0, Mathf.Max(vel[i].z, maxVel.z));
                    minVel = new Vector3(Mathf.Min(vel[i].x, minVel.x), 0, Mathf.Min(vel[i].z, minVel.z));

                }
                vel[pos.Length - 1] = vel[pos.Length - 2];
                velocities[pIdx] = vel;
            }

            trackStruct.totalStep = totalStep;
            trackStruct.positions = positions;
            trackStruct.velocities = velocities;
            this.tracks.Add(trackStruct);
        }

        Debug.Log("Max Vel:" + maxVel.ToString());
        Debug.Log("Min Vel:" + minVel.ToString());
        m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
        m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;

    }
}
