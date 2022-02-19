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

    private List<RVOplayer> m_playerMap = new List<RVOplayer>();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(1f, 10, 5.0f, 5.0f, 0.5f, m_RVOSettings.playerSpeed, new Vector2(0.0f, 0.0f));
        Simulator.Instance.processObstacles();
        court = transform.parent.Find("fancy_court");
        cam = transform.parent.Find("Camera").GetComponent<Camera>();

        for(int i = 0; i < m_RVOSettings.numOfPlayer; ++i)
        {
            CreatePlayerLabel(i);
        }
    }

    public Vector3 GetRandomSpawnPos(int idx)
    {
        float radius = Mathf.Min(m_RVOSettings.courtX * 0.95f, m_RVOSettings.courtZ * 0.95f);
        float variance = 1.0f;
       
        var angle = (idx + Random.value) * Mathf.PI * 2 / m_RVOSettings.numOfPlayer;
        var randomPosX = Mathf.Cos(angle) * radius;
        var randomPosZ = Mathf.Sin(angle) * radius;
        randomPosX += Random.value * variance;
        randomPosZ += Random.value * variance;

        var randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
  
        return randomSpawnPos;
    }

    public void CreatePlayerLabel(int idx)
    {
        Vector3 rndPos = GetRandomSpawnPos(idx);
        int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));

        GameObject playerObj = Instantiate(playerLabel_prefab, rndPos, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.name = idx + "_PlayerLabel";
        //playerObj.tag = "player_agent";
        //playerObj.layer = LayerMask.NameToLayer("player_agent");

        RVOplayer player = playerObj.GetComponent<RVOplayer>();

        player.sid = sid;
        m_playerMap.Add(player);

        Transform label = playerObj.gameObject.transform.Find("label");
        label.name = idx + "_label";
        Text name = label.Find("panel/Player_info/Name").GetComponent<Text>();
        name.text = label.name;

        RVOLabelAgent agent = player.GetComponentInChildren<RVOLabelAgent>();
        agent.PlayerLabel = player;
        agent.court = court;
        agent.cam = cam;
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
            Simulator.Instance.Clear();
            Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
            Simulator.Instance.setAgentDefaults(1f, 10, 5.0f, 5.0f, 0.5f, m_RVOSettings.playerSpeed, new Vector2(0.0f, 0.0f));
            Simulator.Instance.processObstacles();

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

            foreach(var p in m_playerMap)
            {
                p.GetComponentInChildren<RVOLabelAgent>().SyncReset(step >= m_RVOSettings.MaxSteps);
            }
            step = 0;
        }
        ++step;
        Simulator.Instance.doStep();
    }
}
