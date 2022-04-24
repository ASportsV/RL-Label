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
    void OBViewport(VectorSensor sensor)
    {
        // 6 = 3_camforward + 3_end point
        float maxZInCam = m_RVOSettings.maxZInCam;
        float minZInCam = m_RVOSettings.minZInCam;
        float scaleZInCam = maxZInCam - minZInCam;
        float maxLabelSpeed = m_RVOSettings.maxLabelSpeed;
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

    void OBWorld(VectorSensor sensor)
    {
        // 16 = (2 + 3 + 2) * 2 + 2
        //float maxZInCam = m_RVOSettings.maxZInCam;
        //float minZInCam = m_RVOSettings.minZInCam;
        //float scaleZInCam = maxZInCam - minZInCam;
        float maxLabelSpeed = m_RVOSettings.maxLabelSpeed;
        Vector3 scaleSpeed = new Vector3(maxLabelSpeed + m_RVOSettings.playerSpeedX, 0, maxLabelSpeed + m_RVOSettings.playerSppedZ);

        Vector3 agentPos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(transform.position);
        // 2, world pos
        sensor.AddObservation(agentPos.x / m_RVOSettings.courtX);
        sensor.AddObservation(agentPos.z / m_RVOSettings.courtZ);
        //sensor.AddObservation(posInViewport.z / scaleZInCam);
        // 3, forward
        sensor.AddObservation(m_label.m_Panel.forward);
        // 2 speed
        sensor.AddObservation(m_label.velocity.x / scaleSpeed.x);
        sensor.AddObservation(m_label.velocity.z / scaleSpeed.z);

        // 2. endpoint
        Vector3 relativeToAgent = m_label.PlayerLabel.transform.localPosition - agentPos;
        sensor.AddObservation(relativeToAgent.x / m_RVOSettings.courtX);
        sensor.AddObservation(relativeToAgent.z / m_RVOSettings.courtZ);
        //sensor.AddObservation(relativeTPosInviewport.z / scaleZInCam);
        // 3, forward
        sensor.AddObservation(m_label.PlayerLabel.transform.forward);
        // 2, speed
        Vector3 relativeVel = m_label.PlayerLabel.velocity - m_label.velocity;
        sensor.AddObservation(relativeVel.x / scaleSpeed.x);
        sensor.AddObservation(relativeVel.z / scaleSpeed.z);

        // dummy
        // sensor.AddObservation(0);
        // sensor.AddObservation(0);

        // attentions to others
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            if (!other.gameObject.activeSelf) continue;

            // 10 = 1_type + 2_pos + 3_camforward + 2_vel + 2_endpoint
            Transform player = other.Find("player");
            List<float> playerOBs = new List<float>();
            // 1_type
            playerOBs.Add(1);
            // 2_relative pos
            Vector3 playerRelativePos = m_label.PlayerLabel.transform.localPosition - agentPos; //m_label.cam.WorldToViewportPoint(player.position) - posInViewport;
            playerOBs.Add(playerRelativePos.x / m_RVOSettings.courtX);
            playerOBs.Add(playerRelativePos.z / m_RVOSettings.courtZ);
            //playerOBs.Add(playerRelativePos.z / scaleZInCam);

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
            Vector3 labelRelativePos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(labelAgent.transform.position) - agentPos;
            labelOBs.Add(labelRelativePos.x / m_RVOSettings.courtX);
            labelOBs.Add(labelRelativePos.z / m_RVOSettings.courtZ);
            //labelOBs.Add(labelRelativePos.z / scaleZInCam);
            // 3_cam forward for occlusion
            labelOBs.Add(labelAgent.m_Panel.forward.x);
            labelOBs.Add(labelAgent.m_Panel.forward.y);
            labelOBs.Add(labelAgent.m_Panel.forward.z);
            // 2_relative vel
            Vector3 labelRelativeVel = labelAgent.velocity - m_label.velocity;
            labelOBs.Add(labelRelativeVel.x / scaleSpeed.x);
            labelOBs.Add(labelRelativeVel.z / scaleSpeed.z);

            // another endpoints
            playerOBs.Add(labelRelativePos.x / m_RVOSettings.courtX);
            playerOBs.Add(labelRelativePos.z / m_RVOSettings.courtZ);
            // dummy
            // playerOBs.Add(0);
            bSensor.AppendObservation(playerOBs.ToArray());

            labelOBs.Add(playerRelativePos.x / m_RVOSettings.courtX);
            labelOBs.Add(playerRelativePos.z / m_RVOSettings.courtZ);
            // dummy
            // labelOBs.Add(0);
            bSensor.AppendObservation(labelOBs.ToArray());
        }
    }

    void OBWorldSmall(VectorSensor sensor)
    {
        float maxLabelSpeed = m_RVOSettings.maxLabelSpeed;
        Vector3 scaleSpeed = new Vector3(maxLabelSpeed + m_RVOSettings.playerSpeedX, 0, maxLabelSpeed + m_RVOSettings.playerSppedZ);

        Vector3 agentPos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(transform.position);
        // 2, world pos
        sensor.AddObservation(agentPos.x / m_RVOSettings.courtX);
        sensor.AddObservation(agentPos.z / m_RVOSettings.courtZ);
        // 3, forward
        sensor.AddObservation(m_label.m_Panel.forward);
        // 2 speed
        sensor.AddObservation(m_label.velocity.x / scaleSpeed.x);
        sensor.AddObservation(m_label.velocity.z / scaleSpeed.z);

        // 2. endpoint
        Vector3 relativeToAgent = m_label.PlayerLabel.transform.localPosition - agentPos;
        sensor.AddObservation(relativeToAgent.x / m_RVOSettings.courtX);
        sensor.AddObservation(relativeToAgent.z / m_RVOSettings.courtZ);
        // 3, forward
        sensor.AddObservation(m_label.PlayerLabel.transform.forward);
        // 2, speed
        Vector3 relativeVel = m_label.PlayerLabel.velocity - m_label.velocity;
        sensor.AddObservation(relativeVel.x / scaleSpeed.x);
        sensor.AddObservation(relativeVel.z / scaleSpeed.z);

        var others = new List<Transform>();
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            if (!other.gameObject.activeSelf) continue;
            others.Add(other.Find("player"));
            others.Add(other.Find("label"));
        }

        Vector3 posInViewport = m_label.cam.WorldToViewportPoint(transform.position);
        var closet10 = others.OrderBy(o =>
        {
            Vector3 otherPosInViewport = m_label.cam.WorldToViewportPoint(o.position);
            return Vector2.Distance(new Vector2(otherPosInViewport.x, otherPosInViewport.y), new Vector2(posInViewport.x, posInViewport.y));
        }).Take(15);

        // attentions to others
        foreach (Transform other in closet10)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            if (!other.gameObject.activeSelf) continue;

            List<float> playerOBs = new List<float>();
            if(other.name == "player")
            {
                playerOBs.Add(1);
                // 2_relative pos
                Vector3 playerRelativePos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(other.position) - agentPos; //m_label.cam.WorldToViewportPoint(player.position) - posInViewport;
                playerOBs.Add(playerRelativePos.x / m_RVOSettings.courtX);
                playerOBs.Add(playerRelativePos.z / m_RVOSettings.courtZ);
                // forward
                playerOBs.Add(other.forward.x);
                playerOBs.Add(other.forward.y);
                playerOBs.Add(other.forward.z);
                // 2_relative vel
                Vector3 playerRelativeVel = other.GetComponentInParent<RVOplayer>().velocity - m_label.velocity;
                playerOBs.Add(playerRelativeVel.x / scaleSpeed.x);
                playerOBs.Add(playerRelativeVel.z / scaleSpeed.z);
                // endpoint
                Label labelAgent = other.parent.GetComponentInChildren<Label>();
                Vector3 labelRelativePos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(labelAgent.transform.position) - agentPos;
                playerOBs.Add(labelRelativePos.x / m_RVOSettings.courtX);
                playerOBs.Add(labelRelativePos.z / m_RVOSettings.courtZ);
            }
            else
            {
                playerOBs.Add(0);
                Label labelAgent = other.GetComponentInChildren<Label>();
                Vector3 labelRelativePos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(labelAgent.transform.position) - agentPos;
                // 2 relative pos
                playerOBs.Add(labelRelativePos.x / m_RVOSettings.courtX);
                playerOBs.Add(labelRelativePos.z / m_RVOSettings.courtZ);
                // 3 forward
                playerOBs.Add(labelAgent.m_Panel.forward.x);
                playerOBs.Add(labelAgent.m_Panel.forward.y);
                playerOBs.Add(labelAgent.m_Panel.forward.z);
                // relative vel
                Vector3 labelRelativeVel = labelAgent.velocity - m_label.velocity;
                playerOBs.Add(labelRelativeVel.x / scaleSpeed.x);
                playerOBs.Add(labelRelativeVel.z / scaleSpeed.z);
                // endpoint
                Vector3 playerRelativePos = m_label.PlayerLabel.transform.parent.transform.InverseTransformPoint(labelAgent.PlayerLabel.player.position) - agentPos;
                playerOBs.Add(playerRelativePos.x / m_RVOSettings.courtX);
                playerOBs.Add(playerRelativePos.z / m_RVOSettings.courtZ);
            }
            bSensor.AppendObservation(playerOBs.ToArray());
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (m_RVOSettings.obW) OBWorldSmall(sensor);
        else OBViewport(sensor);
    }

    /*-----------------------Action-----------------------*/
    void addForceMove(ActionBuffers actionBuffers)
    {
        float moveSmooth = m_RVOSettings.moveSmooth;
        float moveUnit = m_RVOSettings.moveUnit;
        float maxLabelSpeed = m_RVOSettings.maxLabelSpeed;
        float xzDistThres = m_RVOSettings.xzDistThres;

        float moveZ = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        
        if (Mathf.Abs(moveZ) > moveSmooth)
        {
            //Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", >moveZ is " + moveZ);
            AddReward(rwd.rew_z);
            m_label.m_Rbody.AddForce(new Vector3(0, 0, moveZ * moveUnit), ForceMode.VelocityChange);
        }
        else
        {
            // Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", <moveZ is " + moveZ);
            AddReward(-rwd.rew_z);
        }

        float moveX = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        if (Mathf.Abs(moveX) > moveSmooth)
        {
            //Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", >moveX is " + moveX);
            AddReward(rwd.rew_x);
            m_label.m_Rbody.AddForce(new Vector3(moveUnit * moveX, 0, 0f), ForceMode.VelocityChange);
        }
        else
        {
            // Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", <moveX is " + moveX);
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
        if(m_RVOSettings.CurrentTech != Tech.Ours) return;
        addForceMove(actionBuffers);
    }

    /*-----------------------Reward-----------------------*/
    public void SyncReset()
    {
        //SetReward(1.0f);
        Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", c_reward is " + GetCumulativeReward());
        //Debug.Log(transform.parent.parent.parent.name + "/" + transform.parent.name + ", c_reward is " + GetCumulativeReward() + ", occ:" + accOcc + ", int:" + accIntersect + ", updateCount:" + updateCount);
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