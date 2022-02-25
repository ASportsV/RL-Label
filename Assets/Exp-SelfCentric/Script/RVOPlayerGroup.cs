using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class RVOPlayerGroup : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    // player
    public GameObject player_prefab;
    // label
    public GameObject label_prefab;
    // player + label
    public GameObject playerLabel_prefab;
    public Sprite redLabel;
    public Sprite blueLabel;

    public Transform court;
    public Camera cam;
    float minZInCam;
    float maxZInCam;
    public int totalStep = -1;
    int currentStep = 0;

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
        maxZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z;
        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + ")");

        if(m_RVOSettings.Dataset == 0)
        {
            //var rnd = new System.Random();
            //var randomized = Enumerable.Range(0, m_RVOSettings.numOfPlayer).OrderBy(item => rnd.Next()).ToList();
            //for (int i = 0; i < randomized.Count; ++i)
            //{
            //    CreatePlayerLabel(randomized[i]);
            //}
        }
        else if (m_RVOSettings.Dataset == 1)
        {
            var positions = GetPos();
            for(int i = 0; i < positions.Count; ++i)
            {
                this.CreatePlayerLabelFromPos(i, positions[i].ToArray());
            }
            // max velocity
            Vector3 maxVel = Vector3.zero;
            Vector3 minVel = new Vector3(Mathf.Infinity, 0, Mathf.Infinity);
            foreach(var player in m_playerMap)
            {
                float maxX = player.velocities.Max(v => Mathf.Abs(v.x));
                float minX = player.velocities.Min(v => Mathf.Abs(v.x));
                float maxZ = player.velocities.Max(v => Mathf.Abs(v.z));
                float minZ = player.velocities.Min(v => Mathf.Abs(v.z));

                maxVel = new Vector3(Mathf.Max(maxX, maxVel.x), 0, Mathf.Max(maxZ, maxVel.z));
                minVel = new Vector3(Mathf.Min(minX, minVel.x), 0, Mathf.Min(minZ, minVel.z));
            }
            Debug.Log("Max Vel:" + maxVel.ToString());
            Debug.Log("Min Vel:" + minVel.ToString());
            m_RVOSettings.playerSpeedX = maxVel.x - minVel.x;
            m_RVOSettings.playerSppedZ = maxVel.z - minVel.z;
        }
    }

    public Vector3 GetRandomSpawnPos(int idx)
    {
        var randomSpawnPos = Vector3.zero;
        if (m_RVOSettings.CrossingMode)
        {
            float radius = Mathf.Min(m_RVOSettings.courtX * 0.95f, m_RVOSettings.courtZ * 0.95f);
            float variance = 1.0f;
       
            var angle = (idx + 0.8f * Random.value) * Mathf.PI * 2 / m_RVOSettings.numOfPlayer;
            var randomPosX = Mathf.Cos(angle) * radius;
            var randomPosZ = Mathf.Sin(angle) * radius;
            randomPosX += Random.value * variance;
            randomPosZ += Random.value * variance;

            randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
        }
        else
        {
            float oneLine = m_RVOSettings.courtZ / 8f;
            float posZ = idx * oneLine - m_RVOSettings.numOfPlayer * 0.5f * oneLine;
            float randomPosX = -m_RVOSettings.courtX + Random.value * 0.3f * m_RVOSettings.courtX;
            randomSpawnPos = new Vector3(randomPosX, 0.5f, posZ);
        }
  
        return randomSpawnPos;
    }

    public void CreatePlayerLabelFromPos(int sid, Vector3[] positions)
    {

        GameObject playerObj = Instantiate(playerLabel_prefab, positions[0], Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";

        var text = playerObj.transform.Find("player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();
        text = playerObj.transform.Find("player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();

        RVOplayer player = playerObj.GetComponent<RVOplayer>();
        player.positions = positions;

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
            //agent.minY = 1.2f;
        }
    }

    //public void CreatePlayerLabel(int idx)
    //{
    //    Vector3 rndPos = GetRandomSpawnPos(idx);
    //    int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));

    //    GameObject playerObj = Instantiate(playerLabel_prefab, rndPos, Quaternion.identity);
    //    playerObj.transform.SetParent(gameObject.transform, false);
    //    playerObj.name = sid + "_PlayerLabel";
    //    var text = playerObj.transform.Find("player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
    //    text.text = sid.ToString();
    //    text = playerObj.transform.Find("player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
    //    text.text = sid.ToString();

    //    //playerObj.tag = "player_agent";
    //    //playerObj.layer = LayerMask.NameToLayer("player_agent");

    //    RVOplayer player = playerObj.GetComponent<RVOplayer>();

    //    player.sid = sid;
    //    m_playerMap.Add(player);

    //    Transform label = playerObj.gameObject.transform.Find("label");
    //    label.name = sid + "_label";

    //    Debug.Log("Finish initialize " + label.name);
    //    var name = label.Find("panel/Player_info/Name").GetComponent<TMPro.TextMeshProUGUI>();
    //    name.text = Random.Range(10, 99).ToString();
    //    var iamge = label.Find("panel/Player_info").GetComponent<Image>();
    //    iamge.sprite = (idx % 2 == 0) ? blueLabel : redLabel;
    //    if(idx % 2 != 0)
    //    {
    //        Color color = new Color(239f / 255f, 83f / 255f, 80f / 255f);
    //        var cubeRenderer = player.player.GetComponent<Renderer>();
    //        cubeRenderer.material.SetColor("_Color", color);
    //    }

    //    RVOLabelAgent agent = player.GetComponentInChildren<RVOLabelAgent>();
    //    agent.PlayerLabel = player;
    //    agent.court = court;
    //    agent.cam = cam;
    //    agent.minZInCam = minZInCam;
    //    agent.maxZInCam = maxZInCam;

    //    // if (idx == 5)
    //    // {
    //    //     DemonstrationRecorder dr = label.gameObject.AddComponent<DemonstrationRecorder>();
    //    //     dr.DemonstrationDirectory = "Assets/Exp-SelfCentric/Demo";
    //    //     dr.DemonstrationName = "RVOLabel";
    //    //     dr.Record = true;
    //    //     BehaviorParameters bp = label.GetComponent<BehaviorParameters>();
    //    //     bp.BehaviorType = BehaviorType.HeuristicOnly;
    //    //     Color color = new Color(255f / 255f, 154 / 255f, 224 / 255f);
    //    //     var cubeRenderer = player.player.GetComponent<Renderer>();
    //    //     cubeRenderer.material.SetColor("_Color", color);
    //    // }
    //}

    // Update is called once per frame
    public int step = 0;

    private float time = 0.0f;
    private float timeStep = 0.04f;
    List<Metrics> metrics = new List<Metrics>();
    private void FixedUpdate()
    {
        // if sync and all reached
        // reset all
        //if (m_RVOSettings.sync && (m_playerMap.All(p => p.reached()) || step >= m_RVOSettings.MaxSteps))
        //{
        //    foreach (var p in m_playerMap)
        //    {
        //        p.GetComponentInChildren<RVOLabelAgent>().SyncReset(step >= m_RVOSettings.MaxSteps);
        //    }

        //    // get new number of agents
        //    int numOfAgent = UnityEngine.Random.Range(m_RVOSettings.minNumOfPlayer, m_RVOSettings.maxNumOfPlayer + 1);
        //    m_RVOSettings.numOfPlayer = numOfAgent;
        //    if (numOfAgent > m_playerMap.Count)
        //    {
        //        // should spawn
        //        for (int i = m_playerMap.Count; i < numOfAgent; ++i)
        //        {
        //            CreatePlayerLabel(i);
        //        }
        //    }
        //    else if (numOfAgent < m_playerMap.Count)
        //    {
        //        // should destory   
        //        for (int i = m_playerMap.Count - 1; i >= numOfAgent; --i)
        //        {
        //            var p = m_playerMap[i];
        //            m_playerMap.RemoveAt(i);
        //            Destroy(p.gameObject);
        //        }
        //    }

        //    // if circle
        //    var rnd = new System.Random();
        //    var randomized = m_playerMap.OrderBy(item => rnd.Next()).ToList();

        //    for (int i = 0, len = randomized.Count; i < len; ++i)
        //    {
        //        var p = randomized[i];
        //        Vector3 rndPos = GetRandomSpawnPos(i);
        //        int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
        //        p.transform.localPosition = rndPos;
        //        p.sid = sid;
        //        p.resetDestination();
        //    }

        //    // -----------> EVALUATION <------------ save occlusion rate
        //    if(m_RVOSettings.evaluate)
        //    {
        //        StreamWriter writer = new StreamWriter(System.DateTime.Now.ToFileTime() + ".txt", true);
        //        writer.Write(string.Join('\n', metrics));
        //        writer.Close();
        //        metrics.Clear();
        //    }

        //    step = 0;
        //}
        //++step;
        time += Time.fixedDeltaTime;

        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }

        if(currentStep < totalStep)
        {
            m_playerMap.ForEach(p => p.step(currentStep));
        }
        else
        {
            currentStep = 0;
            // reset all 
            m_playerMap.ForEach(p => p.GetComponentInChildren<RVOLabelAgent>().SyncReset());
        }

        // -----------> EVALUATION <------------ save occlusion rate
        // calculate occlusion rate here
        if (m_RVOSettings.evaluate)
        {
            GameObject[] occluded = m_playerMap
                .SelectMany(p => p.GetComponentInChildren<RVOLabelAgent>().occluding())
                .Distinct().ToArray();

            int numOfOcclusion = occluded
                .Count();

            Metrics m = new Metrics();
            m.occlusionRate = (float)numOfOcclusion / (2 * m_RVOSettings.numOfPlayer - 1);
            int numOfIntersection = (int) (m_playerMap.Sum(p => p.GetComponentInChildren<RVOLabelAgent>().numOfIntersection()) * 0.5f);
            m.intersections = numOfIntersection;
            metrics.Add(m);
        }
    }

    private List<List<Vector3>> GetPos()
    {

        string fileName = Path.Combine(Application.dataPath, "Exp-SelfCentric/Data/nba_7501.csv");
        StreamReader r = new StreamReader(fileName);
        string pos_data = r.ReadToEnd();

        string[] records = pos_data.Split('\n');

        List<List<Vector3>> playerPos = new List<List<Vector3>>();
        for(int i = 0; i < records.Length; ++i)
        {
            string[] array = records[i].Split(',');
            int playerIdx = int.Parse(array[0]);

            if ((playerIdx + 1) > playerPos.Count)
            {
                playerPos.Add(new List<Vector3>());
            }
            
            playerPos[playerIdx].Add(new Vector3(
                float.Parse(array[1]),
                0.5f,
                float.Parse(array[2])
            ));
        }

        totalStep = playerPos.Min(p => p.Count);

        return playerPos;
    }
}
