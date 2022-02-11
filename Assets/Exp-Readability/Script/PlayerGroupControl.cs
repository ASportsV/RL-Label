using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerGroupControl : MonoBehaviour
{
    ARLabelSettings m_ARLabelSettings;
    // ball
    public GameObject ball_prefab;
    // player
    public GameObject player_prefab;
    // label
    public GameObject label_prefab;

    //public int reachedNum = 0;
    //public int agentTurns = 0;

    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < m_ARLabelSettings.numOfPlayers; ++i)
        {
            this.CreatePlayer();
        }

        for (int i = 0; i < m_ARLabelSettings.numOfAgents; ++i)
        {
            this.CreateLabel();
        }
    }

    /**==================== Player ================== */
    public Player CreatePlayer()
    {

        GameObject playerObj = Instantiate(player_prefab, Vector3.zero, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.tag = "player";
       
        Color color = new Color(226/255f, 108/255f, 76/255f);

        //Get the Renderer component from the new cube
        var cubeRenderer = playerObj.GetComponent<Renderer>();
        cubeRenderer.material.SetColor("_Color", color);

        return playerObj.GetComponent<Player>();
    }

    /**==================== Label ================== */
    public ARLabelAgent CreateLabel()
    {
        GameObject playerObj = Instantiate(player_prefab, Vector3.zero, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
        playerObj.tag = "player_agent";
        playerObj.layer = LayerMask.NameToLayer("player_agent");

        Color color = new Color(69 / 255f, 154 / 255f, 224 / 255f);
        var cubeRenderer = playerObj.GetComponent<Renderer>();
        cubeRenderer.material.SetColor("_Color", color);
        Player player = playerObj.GetComponent<Player>();

        GameObject arLabel = Instantiate(label_prefab, player.transform.position, Quaternion.identity) as GameObject;
        arLabel.transform.SetParent(gameObject.transform, false);
        arLabel.name = "label_" + player.name;
        arLabel.tag = "agent";
        
        ARLabelAgent agent = arLabel.GetComponent<ARLabelAgent>();
        agent.player = player;

        return agent;
    }

    //private void FixedUpdate()
    //{
    //    if(agentTurns == 2* players.Length)
    //    {
    //        // can reset now
    //        print("One turn");
    //        agentTurns = 0;
    //        foreach(var agent in agents)
    //        {
    //            agent.EpisodeInterrupted();
    //        }
    //        //ResetReachNum();
    //    }
    //}
}
