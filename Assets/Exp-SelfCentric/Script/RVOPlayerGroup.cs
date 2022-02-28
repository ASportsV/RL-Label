using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEditor;

public class ReadOnlyAttribute : PropertyAttribute
{

}

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;
    }
}

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
    [ReadOnly] public int currentStep = 0;
    [ReadOnly] public int currentTrack = 0;


    public struct Track
    {
        public List<Vector3[]> positions;
        public List<Vector3[]> velocities;
        public int totalStep;
    }

    public List<Track> tracks = new List<Track>();


    private List<RVOplayer> m_playerMap = new List<RVOplayer>();

    struct Metrics
    {
        public float occlusionRate;
        public int intersections;
        public override string ToString()
        {
            return occlusionRate.ToString() + "," + intersections.ToString();
        }
    }


    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        court = transform.parent.Find("fancy_court");
        cam = transform.parent.Find("Camera").GetComponent<Camera>();

        minZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, -m_RVOSettings.courtZ)).z;
        cam.transform.LookAt(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ));
        maxZInCam = cam.WorldToViewportPoint(new Vector3(m_RVOSettings.courtX, 0, m_RVOSettings.courtZ)).z;

        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + "), old max: " + cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z);
        currentStep = 0;
        currentTrack = 0;

        LoadPosInTrack();
        LoadTrack(currentTrack);
    }

    void LoadTrack(int idx)
    {
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
            player.positions = track.positions[i];
            player.velocities = track.velocities[i];
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
    List<Metrics> metrics = new List<Metrics>();
    List<List<HashSet<int>>> occlusionPerStepPerTrack = new List<List<HashSet<int>>>();
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
            var labelAgents = m_playerMap.Select(p => p.GetComponentInChildren<RVOLabelAgent>());
            // collect the occlusion over time
            List<HashSet<int>> accumulatedOcclusion = new List<HashSet<int>>();
            for(int i = 0; i < tracks[currentTrack].totalStep; ++i)
            {
                var occluded = new HashSet<int>();
                foreach (var labelAgent in labelAgents)
                {
                    occluded.UnionWith(labelAgent.occludedObjectOverTime[i]);
                }
                accumulatedOcclusion.Add(occluded);
            }
            if ((currentTrack + 1) > occlusionPerStepPerTrack.Count)
            {
                occlusionPerStepPerTrack.Add(accumulatedOcclusion);
            }
            else occlusionPerStepPerTrack[currentTrack] = accumulatedOcclusion;
            // save 
            using (StreamWriter writer = new StreamWriter("nba_full_split_occlutionRate.txt", false))
            {
                string data = "";
                for(int i = 0; i < occlusionPerStepPerTrack.Count; ++i)
                {
                    var track = occlusionPerStepPerTrack[i];
                    data += string.Join('\n', track.Select(s => i + "," + string.Join(',', s))) + '\n';
                }

                //writer.Write(string.Join('\n', occlusionPerStepPerTrack.Select(t => string.Join('\n', t.Select(s => string.Join(',', s))))));
                writer.Write(data);
                writer.Close();
            }
            
            // reset all 
            currentStep = 0;
            m_playerMap.ForEach(p => p.GetComponentInChildren<RVOLabelAgent>().SyncReset());
            // get the accumulate occlusion over time

            // load another track
            currentTrack = (++currentTrack % tracks.Count);
            LoadTrack(currentTrack);
        }

        // -----------> EVALUATION <------------ save occlusion rate
        // calculate occlusion rate here
        //if (m_RVOSettings.evaluate)
        //{
        //    GameObject[] occluded = m_playerMap
        //        .SelectMany(p => p.GetComponentInChildren<RVOLabelAgent>().occluding())
        //        .Distinct().ToArray();

        //    int numOfOcclusion = occluded
        //        .Count();

        //    Metrics m = new Metrics();
        //    m.occlusionRate = (float)numOfOcclusion / (2 * m_RVOSettings.numOfPlayer - 1);
        //    int numOfIntersection = (int) (m_playerMap.Sum(p => p.GetComponentInChildren<RVOLabelAgent>().numOfIntersection()) * 0.5f);
        //    m.intersections = numOfIntersection;
        //    metrics.Add(m);
        //}
    }

    private void LoadPosInTrack()
    {

        //string fileName = Path.Combine(Application.streamingAssetsPath, "student_20_100.csv");
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
     
        for(int tIdx = 0; tIdx < tracks.Count; ++tIdx)
        {
            var track = tracks[tIdx];
            var trackStruct = new Track();
            int totalStep = track.Max(p => p.Count);
            List<Vector3[]> positions = track.Select(p => p.ToArray()).ToList();
            List<Vector3[]> velocities = new List<Vector3[]>();

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
                velocities.Add(vel);
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


    void CalBounds(Vector3[] BBox, string tag = "")
    {
        Vector3[] anchors = new Vector3[] {
            new Vector3(-m_RVOSettings.courtX, 0.5f, -m_RVOSettings.courtZ),
            new Vector3(0, 0.5f, -m_RVOSettings.courtZ),
            new Vector3(m_RVOSettings.courtX, 0.5f, -m_RVOSettings.courtZ),

            new Vector3(m_RVOSettings.courtX, 0.5f, 0),
            new Vector3(m_RVOSettings.courtX, 0.5f, m_RVOSettings.courtZ),

            new Vector3(0, 0.5f, m_RVOSettings.courtZ),
            new Vector3(-m_RVOSettings.courtX, 0.5f, m_RVOSettings.courtZ),

            new Vector3(-m_RVOSettings.courtX, 0.5f, 0)
        };


        Vector3 min = Vector3.zero;
        Vector3 max = Vector3.zero;
        // player extent
        foreach (var anchor in anchors)
        {
            // cam lookat
            cam.transform.LookAt(anchor);
            // iterate through the bbox points
            Vector3 localMin = Vector3.zero;
            Vector3 localMax = Vector3.zero;
            foreach (var endPoint in BBox)
            {
                var pointInCam = cam.transform.InverseTransformPoint(endPoint);

                min = new Vector3(Mathf.Min(min.x, pointInCam.x), Mathf.Min(min.y, pointInCam.y), Mathf.Min(min.z, pointInCam.z));
                max = new Vector3(Mathf.Max(max.x, pointInCam.x), Mathf.Max(max.y, pointInCam.y), Mathf.Max(max.z, pointInCam.z));

                localMin = new Vector3(Mathf.Min(localMin.x, pointInCam.x), Mathf.Min(localMin.y, pointInCam.y), Mathf.Min(localMin.z, pointInCam.z));
                localMax = new Vector3(Mathf.Max(localMax.x, pointInCam.x), Mathf.Max(localMax.y, pointInCam.y), Mathf.Max(localMax.z, pointInCam.z));
                print(tag + "LocalMin: " + localMin.ToString() + ", LocalMax: " + localMax.ToString() + ", extent: " + (localMax - localMin).ToString());
            }


        }
        print(tag + "Min: " + min.ToString() + ", Max: " + max.ToString() + ", extent: " + (max - min).ToString());
    }
}
