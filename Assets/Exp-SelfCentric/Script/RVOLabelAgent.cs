using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RVOLabelAgent : Agent
{
    struct Rewards
    {
        public float rew_turn;
        public float rew_y;
        public float rew_z;
        public float rew_x;
        public float rew_occlude;
        public float rew_interse;
        public float rew_dist;
    }

    RVOSettings m_RVOSettings;
    Rewards rwd = new Rewards();
    Label m_label;

    // sensor
    BufferSensorComponent bSensor;

    float xzDistThres;
    float maxLabelSpeed;
    float moveUnit;
    float moveSmooth;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        m_label = GetComponent<Label>();
        bSensor = GetComponent<BufferSensorComponent>();

        Academy.Instance.AgentPreStep += UpdateReward;
        // rewards
        rwd.rew_z = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_z", -0.00025f);
        rwd.rew_x = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_x", -0.00025f);
        rwd.rew_occlude = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_occlude", -0.1f);
        rwd.rew_interse = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_interse", -0.1f);
        rwd.rew_dist = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_dist", -0.01f);
        
        // action params
        xzDistThres = Academy.Instance.EnvironmentParameters.GetWithDefault("xzDistThres", 1.5f);
        moveUnit = Academy.Instance.EnvironmentParameters.GetWithDefault("moveUnit", 3f);
        moveSmooth = Academy.Instance.EnvironmentParameters.GetWithDefault("moveSmooth", 0.01f);
        maxLabelSpeed = Academy.Instance.EnvironmentParameters.GetWithDefault("maxLabelSpeed", 5f);

        // decision period
        GetComponent<DecisionRequester>().DecisionPeriod = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("DecisionPeriod", 5f);
    }

    private void OnDestroy()
    {
        Academy.Instance.AgentPreStep -= UpdateReward;
    }

    public override void Initialize()
    {
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0f, m_RVOSettings.labelY, 0f);
    }

    //public float normalizedDist => Vector3.Distance(
    //        transform.position, 
    //        new Vector3(PlayerLabel.transform.position.x, m_RVOSettings.labelY + PlayerLabel.transform.position.y, PlayerLabel.transform.position.z)
    //) / maxDist;

    /** ------------------ Observation ---------------------**/
    void OBPureRel(VectorSensor sensor)
    {
        // 6 = 3_camforward + 3_end point
        float maxZInCam = m_RVOSettings.maxZInCam;
        float minZInCam = m_RVOSettings.minZInCam;
        float scaleZInCam = maxZInCam - minZInCam;
        Vector3 scaleSpeed = new Vector3(maxLabelSpeed + m_RVOSettings.playerSpeedX, 0, maxLabelSpeed + m_RVOSettings.playerSppedZ);

        // 3, screen x y
        Vector3 posInViewport = m_label.cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
        sensor.AddObservation(posInViewport.z / scaleZInCam);
        // 3, cam to forward
        sensor.AddObservation(m_label.m_Panel.forward);
        sensor.AddObservation(m_label.velocity.x / scaleSpeed.x);
        sensor.AddObservation(m_label.velocity.z / scaleSpeed.z);

        // 3. endpoint
        Vector3 relativeTPosInviewport = m_label.cam.WorldToViewportPoint(m_label.PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        sensor.AddObservation(relativeTPosInviewport.z / scaleZInCam);
        sensor.AddObservation(m_label.PlayerLabel.transform.forward);
        Vector3 relativeVel = m_label.PlayerLabel.velocity - m_label.velocity;
        sensor.AddObservation(relativeVel.x / scaleSpeed.x);
        sensor.AddObservation(relativeVel.z / scaleSpeed.z);


        // attentions to others
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            if (!other.gameObject.activeSelf) continue;

            // 11 = 1_type + 3_pos + 3_camforward + 2_vel + 2_endpoint
            Transform player = other.Find("player");
            List<float> playerOBs = new List<float>();
            // 1_type
            playerOBs.Add(1);
            // 3_relative pos
            Vector3 playerRelativePos = m_label.cam.WorldToViewportPoint(player.position) - posInViewport;
            playerOBs.Add(playerRelativePos.x);
            playerOBs.Add(playerRelativePos.y);
            playerOBs.Add(playerRelativePos.z / scaleZInCam);

            // 3_cam forward for occlusion
            playerOBs.Add(player.forward.x);
            playerOBs.Add(player.forward.y);
            playerOBs.Add(player.forward.z);
            // 2_relative vel
            Vector3 playerRelativeVel = other.GetComponent<RVOplayer>().velocity - m_label.velocity;
            playerOBs.Add(playerRelativeVel.x / scaleSpeed.x);
            playerOBs.Add(playerRelativeVel.z / scaleSpeed.z);

            Label labelAgent = other.GetComponentInChildren<Label>();
            List<float> labelOBs = new List<float>();
            // 1_type
            labelOBs.Add(0);
            // 3_relative pos
            Vector3 labelRelativePos = m_label.cam.WorldToViewportPoint(labelAgent.transform.position) - posInViewport;
            labelOBs.Add(labelRelativePos.x);
            labelOBs.Add(labelRelativePos.y);
            labelOBs.Add(labelRelativePos.z / scaleZInCam);
            // 3_cam forward for occlusion
            labelOBs.Add(labelAgent.m_Panel.forward.x);
            labelOBs.Add(labelAgent.m_Panel.forward.y);
            labelOBs.Add(labelAgent.m_Panel.forward.z);
            // 2_relative vel
            Vector3 labelRelativeVel = labelAgent.velocity - m_label.velocity;
            labelOBs.Add(labelRelativeVel.x / scaleSpeed.x);
            labelOBs.Add(labelRelativeVel.z / scaleSpeed.z);

            // another endpoints
            playerOBs.Add(labelRelativePos.x);
            playerOBs.Add(labelRelativePos.y);
            bSensor.AppendObservation(playerOBs.ToArray());

            labelOBs.Add(playerRelativePos.x);
            labelOBs.Add(playerRelativePos.y);
            bSensor.AppendObservation(labelOBs.ToArray());

        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        OBPureRel(sensor);
    }

    /*-----------------------Action-----------------------*/
    void addForceMove(ActionBuffers actionBuffers)
    {
        float moveZ = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        if (Mathf.Abs(moveZ) > moveSmooth)
        {
            AddReward(rwd.rew_z);
            m_label.m_Rbody.AddForce(new Vector3(0, 0, moveZ * moveUnit), ForceMode.VelocityChange);
        }
        else
        {
            AddReward(-rwd.rew_z);
        }

        float moveX = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        if (Mathf.Abs(moveX) > moveSmooth)
        {
            AddReward(rwd.rew_x);
            m_label.m_Rbody.AddForce(new Vector3(moveUnit * moveX, 0, 0f), ForceMode.VelocityChange);
        }
        else
        {
            AddReward(-rwd.rew_x);
        }

        m_label.m_Rbody.velocity = new Vector3(
            Mathf.Clamp(m_label.m_Rbody.velocity.x, -maxLabelSpeed, maxLabelSpeed),
            m_label.m_Rbody.velocity.y,
            Mathf.Clamp(m_label.m_Rbody.velocity.z, -maxLabelSpeed, maxLabelSpeed)
        );

        float distToTarget = transform.position.z - m_label.PlayerLabel.transform.position.z;
        if (Mathf.Abs(distToTarget) > xzDistThres)
        {
            transform.position = new Vector3(
                transform.position.x, 
                transform.position.y, 
                m_label.PlayerLabel.transform.position.z + (distToTarget > 0 ? xzDistThres : -xzDistThres)
            );
            m_label.m_Rbody.velocity = new Vector3(m_label.m_Rbody.velocity.x, 0f, 0f);
        }

        distToTarget = transform.position.x - m_label.PlayerLabel.transform.position.x;
        if (Mathf.Abs(distToTarget) > xzDistThres)
        {
            transform.position = new Vector3(
                m_label.PlayerLabel.transform.position.x + (distToTarget > 0 ? xzDistThres : -xzDistThres), 
                transform.position.y, 
                transform.position.z
            );
            m_label.m_Rbody.velocity = new Vector3(0f, 0f, m_label.m_Rbody.velocity.z);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        addForceMove(actionBuffers);
    }

    /*-----------------------Reward-----------------------*/
    //int accOcc = 0;
    //int accIntersect = 0;
    //int updateCount = 0;
    public void SyncReset()
    {
        //SetReward(1.0f);
        Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", c_reward is " + GetCumulativeReward());
        //Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", c_reward is " + GetCumulativeReward() + ", occ:" + accOcc + ", int:" + accIntersect + ", updateCount:" + updateCount);
        //accOcc = 0;
        //accIntersect = 0;
        //updateCount = 0;
        EpisodeInterrupted();
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0 || !gameObject.activeSelf)
        {
            return;
        }

        // being occluded
        float rew = 0f;
        var numOcc = m_label.rewOcclusions();
        rew += rwd.rew_occlude * numOcc;
        if (rew == 0) rew += 0.01f;
        //accOcc += numOcc;
 
        int numOfIntersections = m_label.numOfIntersection();
        if (numOfIntersections == 0) rew += 0.01f;
        else rew += rwd.rew_interse * numOfIntersections;
        //accIntersect += numOfIntersections;

        //updateCount += 1;
        //Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", occ:" + numOcc + ", accOcc:" + accOcc + "/" + updateCount +", int:" + accIntersect);
        //Debug.Break();

        AddReward(rew);
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
    }

}
