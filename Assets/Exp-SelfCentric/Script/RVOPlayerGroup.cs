using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = RVO.Vector2;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

public class RVOPlayerGroup : MonoBehaviour
{
    RVOSettings m_RVOSettings;
    // player
    public GameObject player_prefab;
    // label
    public GameObject label_prefab;
    // player + label
    public GameObject playerLabel_prefab;

    public Transform court;
    public Camera cam;
    float minZInCam;
    float maxZInCam;

    private List<RVOplayer> m_playerMap = new List<RVOplayer>();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(1f, 10, 5.0f, 5.0f, 1f, m_RVOSettings.playerSpeed, new Vector2(0.0f, 0.0f));
        Simulator.Instance.processObstacles();
        court = transform.parent.Find("fancy_court");
        cam = transform.parent.Find("Camera").GetComponent<Camera>();

        minZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, -m_RVOSettings.courtZ)).z;
        maxZInCam = cam.WorldToViewportPoint(new Vector3(0, 0, m_RVOSettings.courtZ)).z;
        Debug.Log("Min and Max Z in Cam: (" + minZInCam.ToString() + "," + maxZInCam.ToString() + ")");

        var rnd = new System.Random();
        var randomized = Enumerable.Range(0, m_RVOSettings.numOfPlayer).OrderBy(item => rnd.Next()).ToList();
        for (int i = 0; i < randomized.Count; ++i)
        {
            CreatePlayerLabel(randomized[i]);
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
            float oneLine = m_RVOSettings.courtZ / 7f;
            float posZ = idx * oneLine - m_RVOSettings.numOfPlayer * 0.5f * oneLine;
            float randomPosX = -m_RVOSettings.courtX + Random.value * 0.3f * m_RVOSettings.courtX;
            randomSpawnPos = new Vector3(randomPosX, 0.5f, posZ);
        }
  
        return randomSpawnPos;
    }

    public void CreatePlayerLabel(int idx)
    {
        Vector3 rndPos = GetRandomSpawnPos(idx);
        int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));

        GameObject playerObj = Instantiate(playerLabel_prefab, rndPos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = sid + "_PlayerLabel";
        var text = playerObj.transform.Find("player/BackCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();
        text = playerObj.transform.Find("player/TopCanvas/Text").GetComponent<TMPro.TextMeshProUGUI>();
        text.text = sid.ToString();

        //playerObj.tag = "player_agent";
        //playerObj.layer = LayerMask.NameToLayer("player_agent");

        RVOplayer player = playerObj.GetComponent<RVOplayer>();

        player.sid = sid;
        m_playerMap.Add(player);

        Transform label = playerObj.gameObject.transform.Find("label");
        label.name = sid + "_label";

        Debug.Log("Finish initialize " + label.name);
        var name = label.Find("panel/Player_info/Name").GetComponent<TMPro.TextMeshProUGUI>();
        name.text = Random.Range(10, 99).ToString();

        RVOLabelAgent agent = player.GetComponentInChildren<RVOLabelAgent>();
        agent.PlayerLabel = player;
        agent.court = court;
        agent.cam = cam;
        agent.minZInCam = minZInCam;
        agent.maxZInCam = maxZInCam;

        // if (idx == 5)
        // {
        //     DemonstrationRecorder dr = label.gameObject.AddComponent<DemonstrationRecorder>();
        //     dr.DemonstrationDirectory = "Assets/Exp-SelfCentric/Demo";
        //     dr.DemonstrationName = "RVOLabel";
        //     dr.Record = true;
        //     BehaviorParameters bp = label.GetComponent<BehaviorParameters>();
        //     bp.BehaviorType = BehaviorType.HeuristicOnly;
        //     Color color = new Color(255f / 255f, 154 / 255f, 224 / 255f);
        //     var cubeRenderer = player.player.GetComponent<Renderer>();
        //     cubeRenderer.material.SetColor("_Color", color);
        // }
    }

    // Update is called once per frame
    public int step = 0;
    private void FixedUpdate()
    {
        // if sync and all reached
        // reset all
        if(m_RVOSettings.sync && (m_playerMap.All(p => p.reached()) || step >= m_RVOSettings.MaxSteps))
        {
            foreach (var p in m_playerMap)
            {
                p.GetComponentInChildren<RVOLabelAgent>().SyncReset(step >= m_RVOSettings.MaxSteps);
            }

            // get new number of agents
            int numOfAgent = UnityEngine.Random.Range(m_RVOSettings.minNumOfPlayer, m_RVOSettings.maxNumOfPlayer+1);
            m_RVOSettings.numOfPlayer = numOfAgent;
            if(numOfAgent > m_playerMap.Count)
            {
                // should spawn
                for (int i = m_playerMap.Count; i < numOfAgent; ++i)
                {
                    CreatePlayerLabel(i);
                }
            }
            else if (numOfAgent < m_playerMap.Count)
            {
                // should destory   
                for(int i = m_playerMap.Count -1; i >= numOfAgent; --i)
                {
                    var p = m_playerMap[i];
                    m_playerMap.RemoveAt(i);
                    Destroy(p.gameObject);
                }
            }

            Simulator.Instance.Clear();
            Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
            Simulator.Instance.setAgentDefaults(1f, 10, 5.0f, 5.0f, 0.5f, m_RVOSettings.playerSpeed, new Vector2(0.0f, 0.0f));
            Simulator.Instance.processObstacles();

            // if circle
            var rnd = new System.Random();
            var randomized = m_playerMap.OrderBy(item => rnd.Next()).ToList();

            for (int i = 0, len = randomized.Count; i < len; ++i)
            {
                var p = randomized[i];
                Vector3 rndPos = GetRandomSpawnPos(i);
                int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
                p.transform.localPosition = rndPos;
                p.sid = sid;
                p.resetDestination();
            }

            step = 0;
        }
        ++step;
        Simulator.Instance.doStep();
    }
}
