using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RVOLabelAgent : Agent
{


    [System.Serializable]
    public class RewardInfo
    {                                           
        public float rew_turn = 0f;
        public float rew_y = -1f;
        public float rew_z = 0f;
        public float rew_occlude = -0.1f;
        public float rew_intersets = -0.1f;
        public float rew_dist = -0.01f;
    }

    RVOSettings m_RVOSettings;
    RewardInfo rwd = new RewardInfo();

    public RVOplayer PlayerLabel;
    public Camera cam;
    public Transform court;
    //Rigidbody m_Rbody;
    RectTransform rTransform;
    RVOLine m_RVOLine;
    Transform m_Panel;

    // sensor
    BufferSensorComponent bSensor;
    RayPerceptionSensorComponent3D raycastSensor;

    float minY = 1f;
    float yDistThres = 3.0f;
    float xzDistThres = 3.0f;
    float maxDist;
    float maxYspeed = 3f;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    public override void Initialize()
    {
        //m_Rbody = GetComponent<Rigidbody>();
        rTransform = GetComponentInChildren<RectTransform>();
        //MaxStep = m_RVOSettings.MaxSteps;
        bSensor = GetComponent<BufferSensorComponent>();
        m_RVOLine = GetComponent<RVOLine>();
        maxDist = Mathf.Sqrt(yDistThres * yDistThres + xzDistThres * xzDistThres);
        m_Panel = transform.Find("panel");
    }

    public override void OnEpisodeBegin()
    {
        if (!m_RVOSettings.sync) PlayerLabel.resetDestination();
        transform.localPosition = new Vector3(0f, minY, 0f);
        transform.forward = PlayerLabel.transform.forward;
    }

    Vector3 velocity => PlayerLabel.velocity;
 

    /** ------------------ Observation ---------------------**/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfVel = velocity;

        // 2 + 3 + 3
        Vector3 localPosition = selfPos - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);
        
        Vector3 distToGoal = selfPos - PlayerLabel.transform.position;
        float distY = localPosition.y - minY;
        sensor.AddObservation(distToGoal.x / m_RVOSettings.courtX);
        sensor.AddObservation(distY / yDistThres);
        sensor.AddObservation(distToGoal.z / m_RVOSettings.courtZ);
        sensor.AddObservation(transform.forward);

        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            List<float> obs = new List<float>();

            foreach(Transform child in other)
            {
                // 2 + 2
                Vector3 relativePos = child.position - selfPos;
                obs.Add(relativePos.x / m_RVOSettings.courtX);
                obs.Add(relativePos.z / m_RVOSettings.courtZ);
            }
            
            Vector3 relativeVel = other.GetComponent<RVOplayer>().velocity - selfVel;
            obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
            obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));

            bSensor.AppendObservation(obs.ToArray());
        }
    }

    // dist + theta
    void OBIn3DWorldSpace2(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfVel = velocity;

        // 2 + 3 + 3
        Vector3 localPosition = selfPos - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);

        float distToGocal = Vector3.Distance(selfPos, new Vector3(PlayerLabel.transform.position.x, minY, PlayerLabel.transform.position.z));
        sensor.AddObservation(distToGocal);

        var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
        if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y < 0) angle = -angle;
        sensor.AddObservation((angle - minAngle) / (maxAngle - minAngle));

        Vector3 selfPosInViewport = cam.WorldToViewportPoint(selfPos);
        Vector3 goalPosInViewport = cam.WorldToViewportPoint(PlayerLabel.transform.position);
        // 4 ?
        sensor.AddObservation(selfPosInViewport.x);
        sensor.AddObservation(selfPosInViewport.y);
        sensor.AddObservation(goalPosInViewport.x);
        sensor.AddObservation(goalPosInViewport.y);

        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            List<float> obs = new List<float>();

            foreach (Transform child in other)
            {
                // 2 + 2
                Vector3 relativePos = cam.WorldToViewportPoint(child.position) - selfPosInViewport;
                obs.Add(relativePos.x);
                obs.Add(relativePos.y);
            }

            Vector3 relativeVel = other.GetComponent<RVOplayer>().velocity - selfVel;
            obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
            obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));

            bSensor.AppendObservation(obs.ToArray());
        }
    }

    float minAngle = -170f;
    float maxAngle = -10f;
    void OBIn2DViewportSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfPosInViewport = cam.WorldToViewportPoint(selfPos);
        Vector3 selfVel = velocity;
        Vector3 goalPosInViewport = cam.WorldToViewportPoint(PlayerLabel.transform.position);

        float distToGocal = Vector3.Distance(selfPos, new Vector3(PlayerLabel.transform.position.x, minY, PlayerLabel.transform.position.z));
        sensor.AddObservation(distToGocal);

        var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
        if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y < 0) angle = -angle;
        sensor.AddObservation((angle - minAngle) / (maxAngle - minAngle));
        sensor.AddObservation(selfPosInViewport);
        sensor.AddObservation(goalPosInViewport);

        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            List<float> obs = new List<float>();

            foreach (Transform child in other)
            {
                // 2 + 2
                Vector3 relativePos = cam.WorldToViewportPoint(child.position) - selfPosInViewport;
                obs.Add(relativePos.x);
                obs.Add(relativePos.y);
            }

            Vector3 relativeVel = other.GetComponent<RVOplayer>().velocity - selfVel;
            obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
            obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));

            bSensor.AppendObservation(obs.ToArray());
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        this.OBIn2DViewportSpace(sensor);
    }

    /*-----------------------Action-----------------------*/
 
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        var moveZ = actionBuffers.DiscreteActions[0] == 1
            ? +0.02f
            : actionBuffers.DiscreteActions[0] == 2
            ? -0.02f
            : 0;
        if(moveZ != 0)
        {
            // AddReward(-0.001f);
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * (PlayerLabel.transform.position - transform.position);
            bool isForward = localDir.z > 0;

            transform.position += transform.forward * moveZ;
            Vector3 target = new Vector3(PlayerLabel.transform.position.x, transform.position.y, PlayerLabel.transform.position.z);
            float distToTarget = Vector3.Distance(transform.position, target);
            if(distToTarget >= xzDistThres)
            {
                transform.position = target - transform.forward * (xzDistThres - 0.001f);
            }
            if(!isForward)
            {
                transform.position = target;
            }
        }


        ///
        var moveY = actionBuffers.DiscreteActions[2] == 1
            ? +0.02f
            : actionBuffers.DiscreteActions[2] == 2
            ? -0.02f
            : 0;
        if(moveY != 0)
        {
            AddReward(rwd.rew_y);
            float newY = Mathf.Clamp(transform.localPosition.y + moveY, minY, minY + yDistThres);
            transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        }

        // rotation
        var rotateY = actionBuffers.DiscreteActions[1] == 1
            ? +2f
            : actionBuffers.DiscreteActions[1] == 2
            ? -2f
            : 0;
        if(rotateY != 0)
        {
            // AddReward(-0.001f);
            var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
            if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y < 0) angle = -angle;
            rotateY = Mathf.Clamp(angle + rotateY, minAngle, maxAngle) - angle;
            transform.RotateAround(PlayerLabel.player.position, PlayerLabel.player.up, rotateY);
        }

    }

    /*-----------------------Reward-----------------------*/
    float postiveShape(float x, float maxX = 1.0f)
    {
        x = x / maxX;
        return 1f / (1f + Mathf.Pow((x / (1f - x)), -2));
    }

    float negativeShape(float x, float maxX = 1.0f)
    {
        x = x / maxX;
        return Mathf.Pow(1 - Mathf.Pow(x, 1.5f), 2);
    }

    public void SyncReset(bool maxstep = false)
    {
        SetReward(1.0f);
        Debug.Log(this.name + " c_reward is " + GetCumulativeReward());
        if (maxstep) EpisodeInterrupted();
        else EndEpisode();
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0)
        {
            return;
        }

        if(!m_RVOSettings.sync && PlayerLabel.reached())
        {
            SetReward(1.0f);
            EndEpisode();
            return;
        }

        // being occluded
        // return [0, 0.1]
        Vector3 origin = m_Panel.position;
        float radius = 0.3f;
        float maxDistance = Mathf.Infinity;
        Vector3 direction = m_Panel.forward;

        float rew = 0f;
        // occluded by labels
        RaycastHit m_Hit;

        int labelLayerMask = 1 << LayerMask.NameToLayer("label");
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, labelLayerMask))
        {
            // [0, 1]
            // Vector3 hitVel = m_Hit.collider.GetComponent<RVOLabelAgent>().velocity;
            // Vector3 relativeSpeed = hitVel - velocity;
            // float sqrtMat = Mathf.Min(relativeSpeed.sqrMagnitude, 2 * m_RVOSettings.playerSpeed);
            // float normalizedSqrtMat = sqrtMat / (4 * m_RVOSettings.playerSpeed * m_RVOSettings.playerSpeed);
            // float transferedSqrtMat = this.negativeShape(normalizedSqrtMat);
            rew += rwd.rew_occlude * 1; //transferedSqrtMat;
        }
        
        // occluding players + labels
        //PlayerLabel.player.gameObject.layer = LayerMask.NameToLayer("Default");
        int playerLayerMask = 1 << LayerMask.NameToLayer("player") | labelLayerMask;
        if (Physics.SphereCast(origin, radius, -direction, out m_Hit, maxDistance, playerLayerMask))
        {
            // [0, 1]
            // Vector3 hitVel = m_Hit.collider.transform.parent.GetComponent<RVOplayer>().velocity;
            // Vector3 relativeSpeed = hitVel - velocity;
            // float sqrtMat = Mathf.Min(relativeSpeed.sqrMagnitude, 2 * m_RVOSettings.playerSpeed);
            // float normalizedSqrtMat = sqrtMat / (4 * m_RVOSettings.playerSpeed * m_RVOSettings.playerSpeed);
            // float transferedSqrtMat = this.negativeShape(normalizedSqrtMat);
            rew += rwd.rew_occlude * 1; // transferedSqrtMat;
        }

        int numOfIntersections = transform.parent.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject))
            .Count(l => l.isIntersected(m_RVOLine, cam));
        rew += rwd.rew_intersets * numOfIntersections;

        float dist = Mathf.Clamp(Vector3.Distance(
                transform.position,
                new Vector3(PlayerLabel.transform.position.x, minY + PlayerLabel.transform.position.y, PlayerLabel.transform.position.z)
            ), 0, maxDist);

            // [0, 0.01]
            //float rewDist = this.negativeShape(dist, 4.24f); // 3 * sqrt2
        float rewDist = rwd.rew_dist * this.postiveShape(dist, maxDist);

        // if no occlusion and intersection, double penatly to move fast        
        rew += rewDist * (rew == 0 ? 2 : 1);
        AddReward(rew);

        //PlayerLabel.player.gameObject.layer = LayerMask.NameToLayer("player");
        transform.Find("panel").LookAt(cam.transform);
    }

    public Vector3 GetExtentInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        //forward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }


        discreteActionsOut[1] = 0;
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }

        discreteActionsOut[2] = 0;
        if (Input.GetKey(KeyCode.Q))
        {
            discreteActionsOut[2] = 1;
        }
        if (Input.GetKey(KeyCode.E))
        {
            discreteActionsOut[2] = 2;
        }

        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = -Input.GetAxis("Horizontal");
    }
}
