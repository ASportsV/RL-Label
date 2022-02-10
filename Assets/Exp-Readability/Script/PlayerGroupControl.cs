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

    Player[] players;
    ARLabelAgent[] agents;
    public int reachedNum = 0;
    public int agentTurns = 0;

    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
    }

    // Start is called before the first frame update
    void Start()
    {

        List<Player> newPlayer = new List<Player>();
        List<ARLabelAgent> newAgent = new List<ARLabelAgent>();
        if(m_ARLabelSettings.numOfPlayers == 0)
        {
            players = GetComponentsInChildren<Player>();
            agents = GetComponentsInChildren<ARLabelAgent>();
        }
        else
        {
            for (int i = 0; i < m_ARLabelSettings.numOfPlayers; ++i)
            {
                Player player = CreatePlayer();
         
                newPlayer.Add(player);
                newAgent.Add(CreateLabel(player));
            }
            players = newPlayer.ToArray();
            agents = newAgent.ToArray();
        }

    }

    /**==================== Player ================== */
    public Player CreatePlayer()
    {

        GameObject playerObj = Instantiate(player_prefab, Vector3.zero, Quaternion.identity);
        playerObj.transform.SetParent(gameObject.transform, false);
       
        //Transform cam = this.transform.parent.Find("Camera");
        //playerData.sceneCamera = cam;
        //playerData.overlay = this.transform.parent.Find("Canvas");

        Color color = Random.value > 0.5 ? new Color(69/255f, 154/255f, 224/255f) : new Color(226/255f, 108/255f, 76/255f);

        //Get the Renderer component from the new cube
        var cubeRenderer = playerObj.GetComponent<Renderer>();
        cubeRenderer.material.SetColor("_Color", color);

        return playerObj.GetComponent<Player>();
    }


    public ARLabelAgent CreateLabel(Player player)
    {

        GameObject arLabel = Instantiate(label_prefab, player.transform.position, Quaternion.identity) as GameObject;
        arLabel.transform.SetParent(gameObject.transform, false);
        arLabel.name = "label_" + player.name;
        
        ARLabelAgent agent = arLabel.GetComponent<ARLabelAgent>();
        agent.player = player;

        return agent;
    }

    public bool reachLock = false;
    public bool AllPlayerReached()
    {

        if (reachedNum == players.Length)
        {
            reachLock = true;
            agentTurns += reachedNum;
        }

        if(reachedNum == 0 && reachLock)
        {
            reachLock = false;
        }

        return reachLock;
    }

    public void AddReachNum()
    {
        ++reachedNum;
    }

    public void MinusReachNum()
    {
        --reachedNum;
    }

    public void ResetReachNum()
    {
        reachedNum = 0;
        reachLock = false;
    }

    private void FixedUpdate()
    {
        //reachedNum = players.Count(p => !p.isMoving);
        //if(reachedNum == players.Length)
        //{
        //    agentTurns += reachedNum;
        //}
        if(agentTurns == 2* players.Length)
        {
            // can reset now
            print("One turn");
            agentTurns = 0;
            foreach(var agent in agents)
            {
                agent.EpisodeInterrupted();
            }
            ResetReachNum();
        }
    }
}
