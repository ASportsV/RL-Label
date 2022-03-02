using System.Collections;
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
        public float rew_intersets;
        public float rew_dist;
    }

    RVOSettings m_RVOSettings;
    Rewards rwd = new Rewards();

    public RVOplayer PlayerLabel;
    public Camera cam;
    public Transform court;
    Rigidbody m_Rbody;
    RectTransform rTransform;
    RVOLine m_RVOLine;
    Transform m_Panel;

    public List<HashSet<string>> occludedObjectOverTime = new List<HashSet<string>>();
    public List<HashSet<string>> intersectionsOverTime = new List<HashSet<string>>();
    public List<float> distToTargetOverTime = new List<float>();
    public List<Vector2> posOverTime = new List<Vector2>();

    // sensor
    BufferSensorComponent bSensor;

    public float minY = 1.8f;
    float yDistThres = 0.0f;
    float xzDistThres;
    float maxDist;
    //float minAngle = -170f;
    //float maxAngle = -10f;
    public float minZInCam;
    public float maxZInCam;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        Academy.Instance.AgentPreStep += UpdateReward;

        m_Rbody = GetComponent<Rigidbody>();

        rwd.rew_z = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_z", -0.00025f);
        rwd.rew_x = Academy.Instance.EnvironmentParameters.GetWithDefault("rew_x", -0.00025f);
        xzDistThres =  Academy.Instance.EnvironmentParameters.GetWithDefault("xzDistThres", 2f);
        rwd.rew_occlude = -0.1f;
        rwd.rew_intersets = -0.1f;
        rwd.rew_dist = -0.01f;
    }

    private void OnDestroy()
    {
        Academy.Instance.AgentPreStep -= UpdateReward;
    }

    public override void Initialize()
    {
        rTransform = GetComponentInChildren<RectTransform>();
        bSensor = GetComponent<BufferSensorComponent>();
        m_RVOLine = GetComponent<RVOLine>();
        maxDist = Mathf.Sqrt(yDistThres * yDistThres + xzDistThres * xzDistThres);
        m_Panel = transform.Find("panel");
    }

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(0f, minY, 0f);
        occludedObjectOverTime.Clear();
        intersectionsOverTime.Clear();
        distToTargetOverTime.Clear();
        posOverTime.Clear();
    }

    Vector3 velocity => PlayerLabel.velocity;

    //public float normalizedAngle {
    //    get { 
    //        var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
    //        if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y< 0) angle = -angle;
    //        return (angle - minAngle) / (maxAngle - minAngle);
    //    }
    //}

    public float normalizedDist => Vector3.Distance(
            transform.position, 
            new Vector3(PlayerLabel.transform.position.x, minY + PlayerLabel.transform.position.y, PlayerLabel.transform.position.z)
    ) / maxDist;


    /** ------------------ Observation ---------------------**/
    void OBPureRel(VectorSensor sensor)
    {
        // 6 = 3_camforward + 3_end point

        // 3, screen x y
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        //sensor.AddObservation(posInViewport.x);
        //sensor.AddObservation(posInViewport.y);
        //sensor.AddObservation((posInViewport.z - minZInCam) / (maxZInCam - minZInCam));

        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 3. endpoint
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        sensor.AddObservation((relativeTPosInviewport.z) / (maxZInCam - minZInCam));

        //// 1, z forward
        //sensor.AddObservation(transform.forward);
        //// theta
        //sensor.AddObservation(normalizedAngle);

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
            Vector3 playerRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            playerOBs.Add(playerRelativePos.x);
            playerOBs.Add(playerRelativePos.y);
            playerOBs.Add(playerRelativePos.z / (maxZInCam - minZInCam));

            // 3_cam forward for occlusion
            playerOBs.Add(player.forward.x);
            playerOBs.Add(player.forward.y);
            playerOBs.Add(player.forward.z);
            // 2_relative vel
            Vector3 playerRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            playerOBs.Add(playerRelativeVel.x / (m_RVOSettings.playerSpeedX));
            playerOBs.Add(playerRelativeVel.z / (m_RVOSettings.playerSppedZ));

            RVOLabelAgent labelAgent = other.GetComponentInChildren<RVOLabelAgent>();
            List<float> labelOBs = new List<float>();
            // 1_type
            labelOBs.Add(0);
            // 3_relative pos
            Vector3 labelRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            labelOBs.Add(labelRelativePos.x);
            labelOBs.Add(labelRelativePos.y);
            labelOBs.Add(labelRelativePos.z / (maxZInCam - minZInCam));
            // 3_cam forward for occlusion
            labelOBs.Add(labelAgent.m_Panel.forward.x);
            labelOBs.Add(labelAgent.m_Panel.forward.y);
            labelOBs.Add(labelAgent.m_Panel.forward.z);
            // 2_relative vel
            Vector3 labelRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            labelOBs.Add(labelRelativeVel.x / (m_RVOSettings.playerSpeedX));
            labelOBs.Add(labelRelativeVel.z / (m_RVOSettings.playerSppedZ));

            // another endpoints
            playerOBs.Add(labelRelativePos.x);
            playerOBs.Add(labelRelativePos.y);
            bSensor.AppendObservation(playerOBs.ToArray());

            labelOBs.Add(playerRelativePos.x);
            labelOBs.Add(playerRelativePos.y);
            bSensor.AppendObservation(labelOBs.ToArray());

        }
    }

    void OBRichzplus(VectorSensor sensor, bool z = false)
    {
        // 9 = 3_pos + 3_camforward + 3_end point

        // 3, screen x y
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
        if (z) sensor.AddObservation((posInViewport.z - minZInCam) / (maxZInCam - minZInCam));
        
        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 3. endpoint
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        if (z) sensor.AddObservation((relativeTPosInviewport.z) / (maxZInCam - minZInCam));

        //// 1, z forward
        //sensor.AddObservation(transform.forward);
        //// theta
        //sensor.AddObservation(normalizedAngle);

        // attentions to others
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;

            // 11 = 1_type + 3_pos + 3_camforward + 2_vel + 2_endpoint
            Transform player = other.Find("player");

            List<float> playerOBs = new List<float>();
            // 1_type
            playerOBs.Add(1);
            // 3_relative pos
            Vector3 playerRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            playerOBs.Add(playerRelativePos.x);
            playerOBs.Add(playerRelativePos.y);
            if (z) playerOBs.Add(playerRelativePos.z / (maxZInCam - minZInCam));
            
            // 3_cam forward for occlusion
            playerOBs.Add(player.forward.x);
            playerOBs.Add(player.forward.y);
            playerOBs.Add(player.forward.z);
            // 2_relative vel
            Vector3 playerRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            playerOBs.Add(playerRelativeVel.x / (m_RVOSettings.playerSpeedX));
            playerOBs.Add(playerRelativeVel.z / (m_RVOSettings.playerSppedZ));

            RVOLabelAgent labelAgent = other.GetComponentInChildren<RVOLabelAgent>();
            List<float> labelOBs = new List<float>();
            // 1_type
            labelOBs.Add(0);
            // 3_relative pos
            Vector3 labelRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            labelOBs.Add(labelRelativePos.x);
            labelOBs.Add(labelRelativePos.y);
            if (z) labelOBs.Add(labelRelativePos.z / (maxZInCam - minZInCam));
            // 3_cam forward for occlusion
            labelOBs.Add(labelAgent.m_Panel.forward.x);
            labelOBs.Add(labelAgent.m_Panel.forward.y);
            labelOBs.Add(labelAgent.m_Panel.forward.z);
            // 2_relative vel
            Vector3 labelRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            labelOBs.Add(labelRelativeVel.x / (m_RVOSettings.playerSpeedX));
            labelOBs.Add(labelRelativeVel.z / (m_RVOSettings.playerSppedZ));
            
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
        //this.OBRichzplus(sensor, true);
        OBPureRel(sensor);
    }

    /*-----------------------Action-----------------------*/
    void distMove(ActionBuffers actionBuffers)
    {
        float moveUnit = 0.06f;
        var moveZ = actionBuffers.DiscreteActions[0] == 1
            ? moveUnit
            : actionBuffers.DiscreteActions[0] == 2
            ? -moveUnit
            : 0;
        if (moveZ != 0)
        {
            AddReward(rwd.rew_z);
            Vector3 newPos = transform.position + new Vector3(0, 0, 1.0f) * moveZ;
            float distToTarget = Mathf.Abs(newPos.z - PlayerLabel.transform.position.z);
            if (distToTarget < xzDistThres)
            {
                transform.position = newPos;
            }
        }
        else
        {
            AddReward(-rwd.rew_z);
        }

        var moveX = actionBuffers.DiscreteActions[1] == 1
            ? -moveUnit
            : actionBuffers.DiscreteActions[1] == 2
            ? +moveUnit
            : 0;
        if (moveX != 0)
        {
            AddReward(rwd.rew_x);
            Vector3 newPos = transform.position + new Vector3(1.0f, 0, 0) * moveX;
            float distToTarget = Mathf.Abs(newPos.x - PlayerLabel.transform.position.x);
            if (distToTarget < xzDistThres)
            {
                transform.position = newPos; //target - new Vector3(1.0f, 0, 0) * (xzDistThres - 0.001f);
            }
        }
        else
        {
            AddReward(-rwd.rew_x);
        }
    }

    void addForceMove(ActionBuffers actionBuffers)
    {
        float moveUnit = 3f;
        float moveZ = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f) * moveUnit;

        // var moveZ = actionBuffers.DiscreteActions[0] == 1
        //    ? moveUnit
        //    : actionBuffers.DiscreteActions[0] == 2
        //    ? -moveUnit
        //    : 0;
        if (Mathf.Abs(moveZ) > 0.001f)
        {
            AddReward(rwd.rew_z);
            m_Rbody.AddForce(new Vector3(0, 0, 1.0f) * moveZ * 1, ForceMode.VelocityChange);

        }
        else
        {
            AddReward(-rwd.rew_z);
        }

        float moveX = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f) * moveUnit;
        // var moveX = actionBuffers.DiscreteActions[1] == 1
        //     ? -moveUnit
        //     : actionBuffers.DiscreteActions[1] == 2
        //     ? +moveUnit
        //     : 0;
        if (Mathf.Abs(moveX) > 0.001f)
        {
            AddReward(rwd.rew_x);
            m_Rbody.AddForce(new Vector3(1, 0, 0f) * moveX * 1, ForceMode.VelocityChange);
        }
        else
        {
            AddReward(-rwd.rew_x);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        addForceMove(actionBuffers);
    }

    private void FixedUpdate()
    {
        float distToTarget = transform.position.z - PlayerLabel.transform.position.z;
        if(Mathf.Abs(distToTarget) > xzDistThres)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, PlayerLabel.transform.position.z + (distToTarget > 0 ? xzDistThres: -xzDistThres));
        }

        distToTarget = transform.position.x - PlayerLabel.transform.position.x;
        if (Mathf.Abs(distToTarget) > xzDistThres)
        {
            transform.position = new Vector3(PlayerLabel.transform.position.x + (distToTarget > 0 ? xzDistThres : -xzDistThres), transform.position.y, transform.position.z);
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

    public void SyncReset()
    {
        SetReward(1.0f);
        Debug.Log(this.name + " c_reward is " + GetCumulativeReward());
        EndEpisode();
    }

    private void CollectOccluding()
    {
        BoxCollider collider = m_Panel.GetComponent<BoxCollider>();
        Vector3 size = collider.size * 0.5f;
        Vector3[] points = new Vector3[] {
            new Vector3(-size.x, size.y, 0),
            new Vector3(0, size.y, 0),
            new Vector3(size.x, size.y, 0),
            //
            new Vector3(-size.x, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(size.x, 0, 0),
            // 
            new Vector3(-size.x, -size.y, 0),
            new Vector3(0, -size.y, 0),
            new Vector3(size.x, -size.y, 0)
        };

        int labelLayerMask = 1 << LayerMask.NameToLayer("label");
        int playerLayerMask = 1 << LayerMask.NameToLayer("player");

        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (var p in points)
        {
            Vector3 origin = m_Panel.TransformPoint(p);
            Vector3 direction = origin - cam.transform.position;
            Debug.DrawRay(origin, direction, new Color(1, 0, 0));
            // raycast, count hit
            RaycastHit hit;
            if(Physics.Raycast(origin, direction, out hit, Mathf.Infinity, labelLayerMask | playerLayerMask))
            {
                if(!GameObject.ReferenceEquals(hit.collider.transform.parent.gameObject, gameObject))
                    hits.Add(hit);
            }
        }

        var ids = new HashSet<string>();


        hits.ForEach(hit => {

            string id;
            if(hit.collider.CompareTag("player"))
            {
                id = "p_" + hit.collider.GetComponentInParent<RVOplayer>().sid;
            }
            else
            {
                id = "l_" + hit.collider.GetComponentInParent<RVOLabelAgent>().PlayerLabel.sid;
            }
            ids.Add(id);                
        });
        occludedObjectOverTime.Add(ids);
    }

    private int numOfIntersection()
    {
        var intersectedLines = transform.parent.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject) && l.isIntersected(m_RVOLine, cam));

        var intersections = new HashSet<string>();
        var selfSid = PlayerLabel.sid;
        foreach(var sid in intersectedLines.Select(i => i.GetComponent<RVOLabelAgent>().PlayerLabel.sid))
        {
            intersections.Add((selfSid > sid) ? (selfSid + "_" + sid) : (sid + "_" + selfSid));
        }
        intersectionsOverTime.Add(intersections);

        return intersectedLines.Count();
    }

    private void CollectDistToTarget()
    {
        distToTargetOverTime.Add(Vector2.Distance(
             new Vector2(transform.position.x, transform.position.z),
             new Vector2(PlayerLabel.transform.position.x, PlayerLabel.transform.position.z)
        ));
    }

    private void CollectPos()
    {
        posOverTime.Add(new Vector2(transform.position.x, transform.position.z));
    }

    RaycastHit forHit;
    RaycastHit backHit;
    int rewOcclusions()
    {
        Vector3 origin = m_Panel.position;
        Vector3 extent = GetSizeInWorld() * 0.5f;
        Vector3 direction = m_Panel.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;

        int count = 0;
        // occluded by labels
        int labelLayerMask = 1 << LayerMask.NameToLayer("label");

        if (Physics.BoxCast(origin, extent, direction, out forHit, rotation, maxDistance, labelLayerMask))
        {
            count += 1;
        }

        // occluding players
        int playerLayerMask = 1 << LayerMask.NameToLayer("player"); //| labelLayerMask;
        if (Physics.BoxCast(origin, extent, -direction, out backHit, rotation, maxDistance, playerLayerMask))
        {
            count += 1;
        }        
        return count;
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0 || !gameObject.activeSelf)
        {
            return;
        }

        // being occluded
        float rew = 0f;
        rew += rwd.rew_occlude * rewOcclusions();

        int numOfIntersections = numOfIntersection();
        rew += rwd.rew_intersets * numOfIntersections;

        //float dist = Mathf.Clamp(Vector3.Distance(
        //        transform.position,
        //        new Vector3(PlayerLabel.transform.position.x, minY + PlayerLabel.transform.position.y, PlayerLabel.transform.position.z)
        //    ), 0, maxDist);
        //float rewDist = rwd.rew_dist * (dist / maxDist);
        // if no occlusion and intersection, double penatly to move fast        
        //rew += rewDist * (rew == 0 ? 2 : 1);
        AddReward(rew);

        m_Panel.LookAt(cam.transform);
        CollectOccluding();
        CollectDistToTarget();
        CollectPos();

    }

    public Vector3 GetSizeInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //var discreteActionsOut = actionsOut.DiscreteActions;
        //discreteActionsOut[0] = 0;
        ////forward
        //if (Input.GetKey(KeyCode.W))
        //{
        //    discreteActionsOut[0] = 1;
        //}
        //if (Input.GetKey(KeyCode.S))
        //{
        //    discreteActionsOut[0] = 2;
        //}


        //discreteActionsOut[1] = 0;
        //if (Input.GetKey(KeyCode.A))
        //{
        //    discreteActionsOut[1] = 1;
        //}
        //if (Input.GetKey(KeyCode.D))
        //{
        //    discreteActionsOut[1] = 2;
        //}

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
