using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Player : MonoBehaviour
{

    ARLabelSettings m_ARLabelSettings;
    PlayerGroupControl m_PlayerGroupControl;

    Rigidbody m_Rbody;  //cached on initialization
    Vector3 destination;
    public int step = 0;
    int reachedTime = 0;

    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
        m_PlayerGroupControl = FindObjectOfType<PlayerGroupControl>();
        m_Rbody = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(this.CompareTag("player"))
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
            randomSpawnPos = new Vector3(randomPosX, 0.5f, randomPosZ);
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
        // if obstacle 1, else diagonose
        float xFactor = gameObject.CompareTag("player") ? 1f : 0f;

        var randomPosX = reachedTime % 2 == 1
            ? Random.Range(-m_ARLabelSettings.courtX, -m_ARLabelSettings.courtX * 0.5f)
            : Random.Range(m_ARLabelSettings.courtX * 0.5f, m_ARLabelSettings.courtX);

        var randomPosZ = reachedTime % 2 == 1
            ? Random.Range(-m_ARLabelSettings.courtZ, m_ARLabelSettings.courtZ * xFactor)
            : Random.Range(-xFactor * m_ARLabelSettings.courtZ, m_ARLabelSettings.courtZ);

        Vector3 destination = new Vector3(randomPosX, 0.5f, randomPosZ);
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
        else if(m_ARLabelSettings.playerMovingMode == ARLabelSettings.PlayerMovingModeType.RandomSpeed)
        {
            float[] speedRatio = gameObject.CompareTag("player") ? new float[] { 1f, 0.8f, 0.5f, 0.3f, 0.1f } : new float[] {0.8f};
            m_Rbody.velocity = this.transform.forward * speedRatio[Random.Range(0, speedRatio.Length)] * m_ARLabelSettings.playerSpeed;
        }

        step = 0;
    }

    private void FixedUpdate()
    {

        //// if reach the destination, move to the next
        if (Vector3.Distance(this.transform.localPosition, destination) < 0.1)
        {
            ++reachedTime;
            if (m_ARLabelSettings.syncPlayerMoving)
            {
                // wait
                m_Rbody.velocity = Vector3.zero;
                if (step >= m_ARLabelSettings.MaxSteps) UpdateDestination();
            }
            else
            {
                // directly update
                UpdateDestination();
            }
        }

        ++step;
        //if (step >= m_ARLabelSettings.MaxSteps)
        //{

        //    //// if reach the end, reset
        //    //if (m_ARLabelSettings.numOfDestinations != -1 && reachedTime >= m_ARLabelSettings.numOfDestinations)
        //    //{
        //    //    Reset();
        //    //    return;
        //    //}
        //    UpdateDestination();
        //}

    }
}
