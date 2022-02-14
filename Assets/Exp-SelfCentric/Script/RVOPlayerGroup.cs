using System.Collections;
using System.Collections.Generic;
using RVO;
using UnityEngine;
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

    private Dictionary<int, RVOplayer> m_playerMap = new Dictionary<int, RVOplayer>();

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(1f, 10, 5.0f, 5.0f, 0.5f, m_RVOSettings.playerSpeed, new Vector2(0.0f, 0.0f));
        court = transform.parent.Find("fancy_court");

        // add in awake
        Simulator.Instance.processObstacles();

        for(int i = 0; i < m_RVOSettings.numOfPlayer; ++i)
        {
            CreatePlayerLabel();
        }
    }

    public Vector3 GetRandomSpawnPos()
    {
        var randomSpawnPos = Vector3.zero;
        float radius = Mathf.Min(m_RVOSettings.courtX * 0.9f, m_RVOSettings.courtZ * 0.9f);
        float variance = 1.0f;
       
        var angle = Random.value * Mathf.PI * 2;
        var randomPosX = Mathf.Cos(angle) * radius;
        var randomPosZ = Mathf.Sin(angle) * radius;
        randomPosX += Random.value * variance;
        randomPosZ += Random.value * variance;

        randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
            
  
        return randomSpawnPos;
    }

    public void CreatePlayerLabel()
    {
        Vector3 rndPos = GetRandomSpawnPos();
        int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
        if (sid >= 0)
        {
            //var worldPos = transform.TransformPoint(rndPos);

            GameObject playerObj = Instantiate(playerLabel_prefab, rndPos, Quaternion.identity);
            playerObj.transform.SetParent(gameObject.transform, false);
            //playerObj.tag = "player_agent";
            //playerObj.layer = LayerMask.NameToLayer("player_agent");

            //Color color = new Color(69 / 255f, 154 / 255f, 224 / 255f);
            //var cubeRenderer = playerObj.GetComponent<Renderer>();
            //cubeRenderer.material.SetColor("_Color", color);
            RVOplayer player = playerObj.GetComponent<RVOplayer>();

            player.sid = sid;
            m_playerMap.Add(sid, player);

            RVOLabelAgent agent = player.GetComponentInChildren<RVOLabelAgent>();
            agent.player = player;
            agent.court = court;
        }
    }
    public void CreateLabel()
    {
        Vector3 rndPos = GetRandomSpawnPos();
        int sid = Simulator.Instance.addAgent(new Vector2(rndPos.x, rndPos.z));
        if(sid >= 0)
        {
            //var worldPos = transform.TransformPoint(rndPos);

            GameObject playerObj = Instantiate(player_prefab, rndPos, Quaternion.identity);
            playerObj.transform.SetParent(gameObject.transform, false);
            playerObj.tag = "player_agent";
            playerObj.layer = LayerMask.NameToLayer("player_agent");

            Color color = new Color(69 / 255f, 154 / 255f, 224 / 255f);
            var cubeRenderer = playerObj.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", color);
            RVOplayer player = playerObj.GetComponent<RVOplayer>();

            player.sid = sid;
            m_playerMap.Add(sid, player);

            GameObject arLabel = Instantiate(label_prefab, new Vector3(rndPos.x, 1.4f, rndPos.z), Quaternion.identity);
            arLabel.transform.SetParent(gameObject.transform, false);
            arLabel.name = "label_" + player.name;
            arLabel.tag = "agent";

            RVOLabelAgent agent = arLabel.GetComponent<RVOLabelAgent>();
            agent.player = player;
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        Simulator.Instance.doStep();  
    }
}
