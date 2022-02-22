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
        public float rew_turn = -0.00025f;
        public float rew_y = -1f;
        public float rew_z = -0.00025f;
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
    float yDistThres = 0.0f;
    float xzDistThres = 3.0f;
    float maxDist;
    float minAngle = -170f;
    float maxAngle = -10f;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    public override void Initialize()
    {
        //m_Rbody = GetComponent<Rigidbody>();
        rTransform = GetComponentInChildren<RectTransform>();
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
    void OB1_Dist(VectorSensor sensor)
    {
        float distToGocal = Vector3.Distance(transform.position, new Vector3(PlayerLabel.transform.position.x, minY+PlayerLabel.transform.position.y, PlayerLabel.transform.position.z));
        sensor.AddObservation(distToGocal);
    }

    void OB1_Angle(VectorSensor sensor)
    {
        var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
        if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y < 0) angle = -angle;
        sensor.AddObservation((angle - minAngle) / (maxAngle - minAngle));
    }

    // 2
    void OB2_WorldPos(VectorSensor sensor)
    {
        Vector3 localPosition = transform.position - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);
    }

    void OB2_ScreenPos(VectorSensor sensor, Vector3 pos)
    {
        Vector3 posInViewport = cam.WorldToViewportPoint(pos);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
    }

    // 3
    void OB3_ScreenPos(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfPosInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(selfPosInViewport);
    }

    void OBForGoals(VectorSensor sensor, bool z = false)
    {
        // 1 + 3 + 3 + 3 + 3 + 1
        // 1, dist
        OB1_Dist(sensor);

        // 2, screen x y
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
        if (z) sensor.AddObservation(posInViewport.z);
        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 2,
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        if (z) sensor.AddObservation(relativeTPosInviewport.z);

        // 1, z forward
        sensor.AddObservation(transform.forward);


        // theta
        OB1_Angle(sensor);

        // attentions to others
        int i = 0;
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;
            
            foreach (Transform child in other)
            {
                List<float> obs = new List<float>();
                // 2 + 1 + 3 + 2 + 10
                Vector3 relativePos = cam.WorldToViewportPoint(child.position) - posInViewport;
                obs.Add(relativePos.x);
                obs.Add(relativePos.y);
                if (z) obs.Add(relativePos.z);

                // type
                obs.Add(child.CompareTag("player") ? 1f : 0f);
                // forward, for occlusion
                Vector3 otherForward = child.CompareTag("player")
                        ? child.forward
                        : child.Find("panel").forward;
                obs.Add(otherForward.x);
                obs.Add(otherForward.y);
                obs.Add(otherForward.z);

                // relative vel
                Vector3 relativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
                obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
                obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));
                // one hot
                float[] one_hot = new float[m_RVOSettings.maxNumOfPlayer];
                one_hot[i] = 1.0f;
                obs.AddRange(one_hot);
                bSensor.AppendObservation(obs.ToArray());
            }
            ++i;
        }
    }

    void OBForGoals3D(VectorSensor sensor, bool z = false)
    {
        // 16 = 1 + 2 + 2 + 3 + 2 + 2 + 3 + 1
        // 1, dist
        OB1_Dist(sensor);

        // 2, 3D x y
        Vector3 localPosition = transform.position - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);
        // 2, screen xy
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);

        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 2,
        Vector3 distToGoal = PlayerLabel.player.position - transform.position;
        sensor.AddObservation(distToGoal.x / m_RVOSettings.courtX);
        sensor.AddObservation(distToGoal.z / m_RVOSettings.courtZ);
        // 2, screen xy
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);

        // 3, z forward
        sensor.AddObservation(transform.forward);

        // 1, theta
        OB1_Angle(sensor);

        // attentions to others
        int i = 0;
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;

            foreach (Transform child in other)
            {
                List<float> obs = new List<float>();
                // 20 = 2 + 2 + 1 + 3 + 2 + 10
                Vector3 relativePos = child.position - transform.position;
                obs.Add(relativePos.x / m_RVOSettings.courtX);
                obs.Add(relativePos.z / m_RVOSettings.courtZ);

                Vector3 relativeScreenPos = cam.WorldToViewportPoint(child.position) - posInViewport;
                obs.Add(relativeScreenPos.x);
                obs.Add(relativeScreenPos.y);

                // type
                obs.Add(child.CompareTag("player") ? 1f : 0f);
                // forward, for occlusion
                Vector3 otherForward = child.CompareTag("player")
                        ? child.forward
                        : child.Find("panel").forward;
                obs.Add(otherForward.x);
                obs.Add(otherForward.y);
                obs.Add(otherForward.z);

                // relative vel
                Vector3 relativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
                obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
                obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));
                // one hot
                float[] one_hot = new float[m_RVOSettings.maxNumOfPlayer];
                one_hot[i] = 1.0f;
                obs.AddRange(one_hot);
                bSensor.AppendObservation(obs.ToArray());
            }
            ++i;
        }
    }

    // old
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

        OB1_Dist(sensor);
        OB1_Angle(sensor);

        // 2 + 3 + 3
        Vector3 localPosition = selfPos - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);

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

    void OBIn3DWorldSpace3(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfPosInViewport = cam.WorldToViewportPoint(selfPos);
        Vector3 selfVel = velocity;
        Vector3 goalPosInViewport = cam.WorldToViewportPoint(PlayerLabel.transform.position);

        OB1_Dist(sensor);
        OB1_Angle(sensor);

        // pos
        Vector3 localPosition = selfPos - court.position;
        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);

        // forward
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


    void OBIn2DViewportSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfPosInViewport = cam.WorldToViewportPoint(selfPos);
        Vector3 selfVel = velocity;
        Vector3 goalPosInViewport = cam.WorldToViewportPoint(PlayerLabel.transform.position);

        OB1_Dist(sensor);
        OB1_Angle(sensor);

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
        this.OBForGoals(sensor, true);
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
            AddReward(rwd.rew_z);
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

        // rotation
        var rotateY = actionBuffers.DiscreteActions[1] == 1
            ? +2f
            : actionBuffers.DiscreteActions[1] == 2
            ? -2f
            : 0;
        if(rotateY != 0)
        {
            AddReward(rwd.rew_turn);
            var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
            if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y < 0) angle = -angle;
            rotateY = Mathf.Clamp(angle + rotateY, minAngle, maxAngle) - angle;
            transform.RotateAround(PlayerLabel.player.position, PlayerLabel.player.up, rotateY);
        }

        // ///
        // var moveY = actionBuffers.DiscreteActions[2] == 1
        //     ? +0.02f
        //     : actionBuffers.DiscreteActions[2] == 2
        //     ? -0.02f
        //     : 0;
        // if(moveY != 0)
        // {
        //     AddReward(rwd.rew_y);
        //     float newY = Mathf.Clamp(transform.localPosition.y + moveY, minY, minY + yDistThres);
        //     transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
        // }
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

    RaycastHit forHit;
    RaycastHit backHit;
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
        Vector3 extent = new Vector3(0.3f, 0.3f, 0.000001f);
        Vector3 direction = m_Panel.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;

        float rew = 0f;
        // occluded by labels
        int labelLayerMask = 1 << LayerMask.NameToLayer("label");
        
        //if (Physics.SphereCast(origin, radius, direction,  maxDistance, labelLayerMask))
        if(Physics.BoxCast(origin, extent, direction, out forHit, rotation, maxDistance, labelLayerMask))
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
        int playerLayerMask = 1 << LayerMask.NameToLayer("player"); // | labelLayerMask;
        //if (Physics.SphereCast(origin, radius, -direction, out backHit, maxDistance, playerLayerMask))
        if (Physics.BoxCast(origin, extent, -direction, out backHit, rotation, maxDistance, playerLayerMask))
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
        m_Panel.LookAt(cam.transform);
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

        // discreteActionsOut[2] = 0;
        // if (Input.GetKey(KeyCode.Q))
        // {
        //     discreteActionsOut[2] = 1;
        // }
        // if (Input.GetKey(KeyCode.E))
        // {
        //     discreteActionsOut[2] = 2;
        // }

        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = -Input.GetAxis("Horizontal");
    }

    private void OnDrawGizmos()
    {
        if (m_Panel == null) return;

        Vector3 origin = m_Panel.position;
        Vector3 direction = m_Panel.forward;
        

        Gizmos.color = new Color(0f, 1f, 0.5f);
        Gizmos.DrawRay(origin, direction);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(origin, -direction);

        if(!Object.Equals(forHit, default(RaycastHit)))
        {
            Vector3 intersectionPoint = origin + Vector3.Project(forHit.point - origin, direction);
            Gizmos.color = new Color(0f, 1f, 0.5f);
            Gizmos.DrawLine(origin, intersectionPoint);
            Gizmos.DrawWireSphere(intersectionPoint, 0.3f);
        }

        if (!Object.Equals(backHit, default(RaycastHit)))
        {
            Vector3 intersectionPoint = origin + Vector3.Project(backHit.point - origin, -direction);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(origin, backHit.point);
            Gizmos.DrawWireSphere(intersectionPoint, 0.3f);
        }
    }
}
