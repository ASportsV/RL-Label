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

    float minY = 1.2f;
    float yDistThres = 0.0f;
    float xzDistThres = 3.0f;
    float maxDist;
    float minAngle = -170f;
    float maxAngle = -10f;
    public float minZInCam;
    public float maxZInCam;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    private void OnDestroy()
    {
        Academy.Instance.AgentPreStep -= UpdateReward;
    }

    public override void Initialize()
    {
        //m_Rbody = GetComponent<Rigidbody>();
        rTransform = GetComponentInChildren<RectTransform>();
        bSensor = GetComponent<BufferSensorComponent>();
        m_RVOLine = GetComponent<RVOLine>();
        maxDist = Mathf.Sqrt(yDistThres * yDistThres + xzDistThres * xzDistThres);
        m_Panel = transform.Find("panel");

        Debug.Log("Initialize " + this.name);
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("OnEpisodeBegin " + this.name);
        if (!m_RVOSettings.sync) PlayerLabel.resetDestination();
        transform.localPosition = new Vector3(0f, minY, 0f);
        transform.forward = transform.parent.transform.forward;
    }

    Vector3 velocity => PlayerLabel.velocity;
    public float normalizedAngle {
        get { 
            var angle = Vector3.Angle(PlayerLabel.transform.right, transform.forward); // find current angle
            if (Vector3.Cross(PlayerLabel.transform.right, transform.forward).y< 0) angle = -angle;
            return (angle - minAngle) / (maxAngle - minAngle);
        }
    }
    public float normalizedDist => Vector3.Distance(transform.position, new Vector3(PlayerLabel.transform.position.x, minY + PlayerLabel.transform.position.y, PlayerLabel.transform.position.z)) / maxDist;


    /** ------------------ Observation ---------------------**/
    void OBRichz(VectorSensor sensor, bool z = false)
    {
        // 1 + 3 + 3 + 3 + 3 + 1
        // 1, dist
        sensor.AddObservation(normalizedDist);

        // 2, screen x y
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
        if (z) sensor.AddObservation((posInViewport.z - minZInCam) / (maxZInCam - minZInCam));
        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 2,
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        if (z) sensor.AddObservation((relativeTPosInviewport.z - minZInCam) / (maxZInCam - minZInCam));

        // 1, z forward
        sensor.AddObservation(transform.forward);

        // theta
        sensor.AddObservation(normalizedAngle);

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
                if (z) obs.Add(relativePos.z / (maxZInCam - minZInCam));

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

    void OBRichzplus(VectorSensor sensor, bool z = false)
    {
        // 14 = 1_dist + 3_pos + 3_camforward + 3_target_endpont + 3_z forward + 1_angle
        // 1, dist
        sensor.AddObservation(normalizedDist);

        // 2, screen x y
        Vector3 posInViewport = cam.WorldToViewportPoint(transform.position);
        sensor.AddObservation(posInViewport.x);
        sensor.AddObservation(posInViewport.y);
        if (z) sensor.AddObservation((posInViewport.z - minZInCam) / (maxZInCam - minZInCam));
        // 3, cam to forward
        sensor.AddObservation(m_Panel.forward);

        // 2,
        Vector3 relativeTPosInviewport = cam.WorldToViewportPoint(PlayerLabel.player.transform.position) - posInViewport;
        sensor.AddObservation(relativeTPosInviewport.x);
        sensor.AddObservation(relativeTPosInviewport.y);
        if (z) sensor.AddObservation((relativeTPosInviewport.z - minZInCam) / (maxZInCam - minZInCam));

        // 1, z forward
        sensor.AddObservation(transform.forward);

        // theta
        sensor.AddObservation(normalizedAngle);

        // attentions to others
        int i = 0;
        foreach (Transform other in transform.parent.parent)
        {
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent.gameObject)) continue;

            // 13 = 1_type + 3_pos + 3_camforward + 2_vel + 2_endpoint + 2_dist_angle
            Transform player = other.Find("player");
            List<float> playerOBs = new List<float>();
            playerOBs.Add(1);
            Vector3 playerRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            playerOBs.Add(playerRelativePos.x);
            playerOBs.Add(playerRelativePos.y);
            if (z) playerOBs.Add(playerRelativePos.z / (maxZInCam - minZInCam));
            // forward for occlusion
            playerOBs.Add(player.forward.x);
            playerOBs.Add(player.forward.y);
            playerOBs.Add(player.forward.z);
            // relative vel
            Vector3 playerRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            playerOBs.Add(playerRelativeVel.x / (2 * m_RVOSettings.playerSpeed));
            playerOBs.Add(playerRelativeVel.z / (2 * m_RVOSettings.playerSpeed));

            RVOLabelAgent labelAgent = other.GetComponentInChildren<RVOLabelAgent>();
            List<float> labelOBs = new List<float>();
            labelOBs.Add(0);
            Vector3 labelRelativePos = cam.WorldToViewportPoint(player.position) - posInViewport;
            labelOBs.Add(labelRelativePos.x);
            labelOBs.Add(labelRelativePos.y);
            if (z) labelOBs.Add(labelRelativePos.z / (maxZInCam - minZInCam));
            // forward for occlusion
            labelOBs.Add(labelAgent.m_Panel.forward.x);
            labelOBs.Add(labelAgent.m_Panel.forward.y);
            labelOBs.Add(labelAgent.m_Panel.forward.z);
            // relative vel
            Vector3 labelRelativeVel = other.GetComponent<RVOplayer>().velocity - velocity;
            labelOBs.Add(labelRelativeVel.x / (2 * m_RVOSettings.playerSpeed));
            labelOBs.Add(labelRelativeVel.z / (2 * m_RVOSettings.playerSpeed));
            
            // another endpoints
            playerOBs.Add(labelRelativePos.x);
            playerOBs.Add(labelRelativePos.y);
            playerOBs.Add(labelAgent.normalizedAngle);
            playerOBs.Add(labelAgent.normalizedDist);
            bSensor.AppendObservation(playerOBs.ToArray());

            labelOBs.Add(playerRelativePos.x);
            labelOBs.Add(playerRelativePos.y);
            labelOBs.Add(labelAgent.normalizedAngle);
            labelOBs.Add(labelAgent.normalizedDist);
            bSensor.AppendObservation(labelOBs.ToArray());

        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        this.OBRichzplus(sensor, true);
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

    public GameObject[] occluding()
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

        //Vector3 origin = cam.transform.position;
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


        GameObject[] gs = hits.GroupBy(h => h.colliderInstanceID).Select(g => g.First().collider.gameObject).ToArray();

        return gs;
    }

    public int numOfIntersection()
    {
        return transform.parent.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject))
            .Count(l => l.isIntersected(m_RVOLine, cam));
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
        float rew = 0f;
        rew += rwd.rew_occlude * rewOcclusions();

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
        float rewDist = rwd.rew_dist * (dist / maxDist);
        // if no occlusion and intersection, double penatly to move fast        
        rew += rewDist * (rew == 0 ? 2 : 1);
        AddReward(rew);

        //PlayerLabel.player.gameObject.layer = LayerMask.NameToLayer("player");
        m_Panel.LookAt(cam.transform);
    }

    public Vector3 GetSizeInWorld()
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
