using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RVO;
using UnityEngine;
using UnityEngine.UI;
using Vector2 = RVO.Vector2;

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
        var randomSpawnPos = Vector3.zero;
        float radius = Mathf.Min(m_RVOSettings.courtX * 0.95f, m_RVOSettings.courtZ * 0.95f);
        float variance = 1.0f;
       
        var angle = (idx + Random.value) * Mathf.PI * 2 / m_RVOSettings.numOfPlayer;
        var randomPosX = Mathf.Cos(angle) * radius;
        var randomPosZ = Mathf.Sin(angle) * radius;
        randomPosX += Random.value * variance;
        randomPosZ += Random.value * variance;

        randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
  
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

        //Color color = new Color(69 / 255f, 154 / 255f, 224 / 255f);
        //var cubeRenderer = playerObj.GetComponent<Renderer>();
        //cubeRenderer.material.SetColor("_Color", color);
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
    }
    //public void CreateLabel()
    //{
    //    Vector3 rndPos = GetRandomSpawnPos();
    //    int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
    //    if(sid >= 0)
    //    {
    //        //var worldPos = transform.TransformPoint(rndPos);

    //        GameObject playerObj = Instantiate(player_prefab, rndPos, Quaternion.identity);
    //        playerObj.transform.SetParent(gameObject.transform, false);
    //        playerObj.tag = "player_agent";
    //        playerObj.layer = LayerMask.NameToLayer("player_agent");

    //        Color color = new Color(69 / 255f, 154 / 255f, 224 / 255f);
    //        var cubeRenderer = playerObj.GetComponent<Renderer>();
    //        cubeRenderer.material.SetColor("_Color", color);
    //        RVOplayer player = playerObj.GetComponent<RVOplayer>();

    //        player.sid = sid;
    //        m_playerMap.Add(sid, player);

    //        GameObject arLabel = Instantiate(label_prefab, new Vector3(rndPos.x, 1.4f, rndPos.z), Quaternion.identity);
    //        arLabel.transform.SetParent(gameObject.transform, false);
    //        arLabel.name = "label_" + player.name;
    //        arLabel.tag = "agent";

    //        RVOLabelAgent agent = arLabel.GetComponent<RVOLabelAgent>();
    //        agent.PlayerLabel = player;
    //    }
    //}

    // Update is called once per frame
    int step = 0;
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
            int i = 0;
            foreach(var p in m_playerMap)
            {
                Vector3 rndPos = GetRandomSpawnPos(i);
                int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
                p.transform.localPosition = rndPos;
                p.sid = sid;
                p.resetDestination();
                ++i;
            }

            foreach(var p in m_playerMap)
            {
                p.GetComponentInChildren<RVOLabelAgent>().SyncReset();
            }
            step = 0;
        }
        ++step;
        Simulator.Instance.doStep();
    }
}
