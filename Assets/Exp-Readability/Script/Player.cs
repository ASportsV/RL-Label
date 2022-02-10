using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{

    ARLabelSettings m_ARLabelSettings;
    PlayerGroupControl m_PlayerGroupControl;

    Rigidbody m_Rbody;  //cached on initialization

    //public int currentDestIdx;
    //Vector3[] destinations;
    //Vector3 currentDest
    //{
    //    get {
    //        return destinations[currentDestIdx]; 
    //    }
    //}
    int reachedTime = 0;
    Vector3 destination;


    public bool isMoving = true;

    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
        m_PlayerGroupControl = FindObjectOfType<PlayerGroupControl>();
    }

    // Start is called before the first frame update
    void Start()
    {
        m_Rbody = GetComponent<Rigidbody>();
        Reset();
    }

    public void Reset()
    {
        this.transform.localPosition = GetRandomSpawnPos();
        m_Rbody.velocity = Vector3.zero;
        m_Rbody.angularVelocity = Vector3.zero;
        reachedTime = 0;
        UpdateDestination();
    }

    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-m_ARLabelSettings.courtX, -m_ARLabelSettings.courtX * 0.5f);
            var randomPosZ = Random.Range(-m_ARLabelSettings.courtZ, m_ARLabelSettings.courtZ);
            randomSpawnPos = new Vector3(randomPosX, 0.6f, randomPosZ);
            foundNewSpawnLocation = true;

            //if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            //{
            //    foundNewSpawnLocation = true;
            //}
        }
        return randomSpawnPos;
    }


    public Vector3 GetRandomDestinations()
    {
        var randomPosX = reachedTime % 2 == 1
            ? Random.Range(-m_ARLabelSettings.courtX, -m_ARLabelSettings.courtX * 0.5f)
            : Random.Range(m_ARLabelSettings.courtX * 0.5f, m_ARLabelSettings.courtX);

        var randomPosZ = reachedTime % 2 == 1
            ? Random.Range(-m_ARLabelSettings.courtZ, 0)
            : Random.Range(0, m_ARLabelSettings.courtZ);

        Vector3 destination = new Vector3(randomPosX, 0.6f, randomPosZ);
        return destination;
    }

    // update forward, velocity, set isMoving as true
    private void UpdateDestination()
    {
        destination = this.GetRandomDestinations();
        transform.forward = (destination - this.transform.localPosition).normalized;
        
        // velocity
        if (m_ARLabelSettings.playerMovingMode == ARLabelSettings.PlayerMovingModeType.FixedSpeed)
        {
            m_Rbody.velocity = this.transform.forward * m_ARLabelSettings.playerSpeed;
        }
        else if(m_ARLabelSettings.playerMovingMode == ARLabelSettings.PlayerMovingModeType.FixedTime)
        {
            // fixed time
            m_Rbody.velocity = this.transform.forward * (Vector3.Distance(this.transform.localPosition, destination) / m_ARLabelSettings.playerMovingTime);
        }

        isMoving = true;
    }


    float waitedTime = 0f;
    private void FixedUpdate()
    {
        // if reach the destination, move to the next
        if(Vector3.Distance(this.transform.localPosition, destination)< 0.1)
        {
            if(m_ARLabelSettings.syncPlayerMoving && !m_PlayerGroupControl.AllPlayerReached())
            {
                // wait
                m_Rbody.velocity = Vector3.zero;
                waitedTime += Time.fixedDeltaTime;
                if(waitedTime >= m_ARLabelSettings.waitAfterReached)
                {
                    isMoving = false;
                    m_PlayerGroupControl.AddReachNum();
                    waitedTime = 0f;
                }
            }
            else
            {
                ++reachedTime;

                // if reach the end, reset
                if(m_ARLabelSettings.numOfDestinations != -1 && reachedTime >= m_ARLabelSettings.numOfDestinations)
                {
                    Reset();
                    return;
                }

                // update forward
                UpdateDestination();
                m_PlayerGroupControl.MinusReachNum();

            }
        } 

    }
}
