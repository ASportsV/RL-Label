using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ARLabelAgent : Agent
{

    ARLabelSettings m_ARLabelSettings;
    Camera cam;
    RectTransform rTransform;
    BufferSensorComponent bsensor;

    public Player player;
    Rigidbody m_PlayerRB;

    float minY = 1.2f;
    float maxYspeed = 2f;

    Rigidbody m_Rbody;  //cached on initialization

    // Start is called before the first frame update
    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
        cam = FindObjectOfType<Camera>();
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    public override void Initialize()
    {
        m_Rbody = GetComponent<Rigidbody>();
        bsensor = GetComponent<BufferSensorComponent>();
        rTransform = GetComponentInChildren<RectTransform>();
    }

    float maxDistToAgent;
    float minDistToAgent;
    float maxDistToPlayer;
    float minDistToPlayer = 1.41421f * 0.25f;
    float normalizeDistToAgent;
    float normalizeDistToPlayer;
    private void Start()
    {
        Vector3 extent = this.GetExtentInWorld();
        maxDistToAgent = Mathf.Max(extent.x, extent.y);
        minDistToAgent = 0f;
        maxDistToPlayer = 1.2247f * 0.5f + maxDistToAgent * 0.5f;

        normalizeDistToAgent = maxDistToAgent - minDistToAgent;
        normalizeDistToPlayer = maxDistToPlayer - minDistToPlayer;
        m_PlayerRB = player.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {

        //// reset the player
        //EnvObj playerScript = player.GetComponent<EnvObj>();
        //playerScript.Reset();
        player.Reset();

        var playerPos = player.transform.localPosition;
        var playerVel = player.GetComponent<Rigidbody>().velocity;
        this.transform.localPosition = new Vector3(playerPos.x, minY, playerPos.z);
        m_Rbody.velocity = new Vector3(playerVel.x, 0, playerVel.z);
        //m_Rbody.velocity = playerVel;
    }

    /** ------------------ Observation ---------------------**/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.localPosition;
        Vector3 selfVel = m_Rbody.velocity;

        sensor.AddObservation(selfPos.x / m_ARLabelSettings.courtX);
        sensor.AddObservation((selfPos.y - minY) / yDistThres);
        sensor.AddObservation(selfPos.z / m_ARLabelSettings.courtZ);

        sensor.AddObservation(selfVel.x / m_ARLabelSettings.playerSpeed);
        sensor.AddObservation(selfVel.y / maxYspeed);
        sensor.AddObservation(selfVel.z / m_ARLabelSettings.playerSpeed);
        //sensor.AddObservation(transform.forward);
        sensor.AddObservation(transform.localScale.x);

        GameObject[] others = this.transform.parent.GetComponentsInChildren<Transform>()
            .Where(x => x.CompareTag("agent") && !GameObject.ReferenceEquals(x.gameObject, gameObject))
            // distance filter
            //.Where(x => Vector3.Distance(x.transform.localPosition, gameObject.transform.localPosition) < 5.0f)
            // should filter based on viewport space
            .Select(x => x.gameObject)
            .ToArray();

        foreach (GameObject other in others)
        {
            List<float> obs = new List<float>();
            Vector3 relativePos = other.transform.localPosition - selfPos;
            obs.Add(relativePos.x / m_ARLabelSettings.courtX);
            obs.Add(relativePos.y / yDistThres);
            obs.Add(relativePos.z / m_ARLabelSettings.courtZ);

            Vector3 relativeVel = other.GetComponent<Rigidbody>().velocity - selfVel;
            sensor.AddObservation(relativeVel.x / m_ARLabelSettings.playerSpeed);
            sensor.AddObservation(relativeVel.y / maxYspeed);
            sensor.AddObservation(relativeVel.z / m_ARLabelSettings.playerSpeed);

            sensor.AddObservation(other.transform.forward);
            obs.Add(other.transform.localScale.x);

            bsensor.AppendObservation(obs.ToArray());
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        this.OBIn3DWorldSpace(sensor);
    }

    /*-----------------------Action-----------------------*/
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float y = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f) * maxYspeed;

        int scaleSize = actionBuffers.DiscreteActions[0];
        float newScale = Mathf.Clamp(scaleSize == 1
                ? this.transform.localScale.x + 0.05f
                : scaleSize == 2
                ? this.transform.localScale.x - 0.05f
                : this.transform.localScale.x, 0.1f, 1f);

        Vector3 playerVel = player.GetComponent<Rigidbody>().velocity;
        m_Rbody.velocity = new Vector3(playerVel.x, y, playerVel.z);

        // should clamp velocity in y
        float nextY = transform.localPosition.y + m_Rbody.velocity.y * Time.fixedDeltaTime;

        if (nextY <= minY)
        {
            m_Rbody.velocity = new Vector3(m_Rbody.velocity.x, 0f, m_Rbody.velocity.z);
            transform.localPosition = new Vector3(transform.localPosition.x, minY, transform.localPosition.z);
        }

        transform.LookAt(cam.transform);
        this.transform.localScale = new Vector3(newScale, newScale, newScale);
    }

    /*-----------------------Reward-----------------------*/
    float rewDist(float dist, float maxDist)
    {
        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(dist / maxDist, 1.4f), 2);
    }

    float rewScale(float scale)
    {
        return 1f / (1f + Mathf.Pow((scale / (1f - scale)), -2)); // [0, 0.1]
    }

    float rewSpeed(float speed, float maxSpeed)
    {
        return Mathf.Pow(1 - Mathf.Pow(speed / maxSpeed, 1.5f), 2); // [0, 1] x [0.1, 0]
    }

    float rewOcclude()
    {
        // return [0, 0.1, 0.2]

        Vector3 origin = transform.localPosition;
        Vector3 size = this.GetExtentInWorld();
        float radius = Mathf.Min(size.x, size.y) * 0.5f;
        //Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;
        int layerMask = 1 << 15;

        // occluded by others
        Vector3 direction = transform.forward;
        RaycastHit m_Hit;
        float rewOcclude = 0;
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        {
            //Bounds hitBounds = m_Hit.collider.bounds;
            // get the hit point
            Vector3 hitPoint = m_Hit.point;
            // get the center point of the hit plane
            Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
            // cal the distance from the intersect point to the center
            float occludeDist = Vector3.Distance(intersectionPoint, m_Hit.collider.bounds.center);

            // normalize
            float normalizeDist = m_Hit.collider.CompareTag("player")
                ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
                : (occludeDist - minDistToAgent) / normalizeDistToAgent;

            rewOcclude += (normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        }

        // occlude others
        direction = -transform.forward;
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        {
            //Bounds hitBounds = m_Hit.collider.bounds;
            // get the hit point
            Vector3 hitPoint = m_Hit.point;
            // get the center point of the hit plane
            Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
            // cal the distance from the intersect point to the center
            float occludeDist = Vector3.Distance(intersectionPoint, m_Hit.collider.bounds.center);

            // normalize
            float normalizeDist = m_Hit.collider.CompareTag("player")
                ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
                : (occludeDist - minDistToAgent) / normalizeDistToAgent;

            rewOcclude += (normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        }

        return rewOcclude / 10f;
    }


    float yDistThres = 3.0f;
    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0)
        {
            return;
        }

        //var t = player.transform;
        //Vector3 goalPos = new Vector3(t.localPosition.x, minY, t.localPosition.z);
        float dist = this.transform.localPosition.y - minY; // Vector3.Distance(goalPos, this.transform.localPosition);
        float selfSize = transform.localScale.x;
        float selfSpeed = m_Rbody.velocity.magnitude;

        //IEnumerable<RaycastHit> m_Hit = Physics.BoxCastAll(origin, halfExtent, direction, rotation, maxDistance, layerMask)
        //        .Where(h => h.collider.CompareTag("agent") && !GameObject.ReferenceEquals(gameObject, h.collider.gameObject));

        //float rewOcclude = 0; // [0, 2]
        //foreach (RaycastHit hit in m_Hit)
        //{
        //    Bounds hitBounds = hit.collider.bounds;
        //    // get the hit point
        //    Vector3 hitPoint = hit.point;
        //    // get the center point of the hit plane
        //    Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
        //    // cal the distance from the intersect point to the center
        //    float occludeDist = Vector3.Distance(intersectionPoint, hit.collider.bounds.center);

        //    // normalize
        //    float normalizeDist = hit.collider.CompareTag("player")
        //        ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
        //        : (occludeDist - minDistToAgent) / normalizeDistToAgent;

        //    if (normalizeDist < 0.3f)
        //    {
        //        rewOcclude += this.rewOcclude(normalizeDist, 0.3f) + 0.1f;
        //    }
        //}


        if (dist >= yDistThres)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
        else
        {
            //float rewDist = -0.1f + this.rewDist(dist, yDistThres);
            //rewDist /= 50f;

            //float rewScale =  this.rewScale(selfSize) * 10;
            //float rewSpeed = this.rewSpeed(m_Rbody.velocity.magnitude, new Vector3(m_ARLabelSettings.playerSpeed, maxYspeed, m_ARLabelSettings.playerSpeed).magnitude * 2) * 10;
            //float rew = 0.1f;

            //SetReward(rew * rewScale * rewSpeed + rewDist - rewOcclude * rewScale * rewSpeed);

            float rewScale = this.rewScale(selfSize);
           
            float rewSpeed = this.rewSpeed(selfSpeed, new Vector3(m_ARLabelSettings.playerSpeed, maxYspeed, m_ARLabelSettings.playerSpeed).magnitude);
            
            float rewDist = -0.1f + this.rewDist(dist, yDistThres);
            rewDist /= 50f; // [0, 0.002]

            float rewOcclude = this.rewOcclude(); // [0, 0.1, 0.2]

            SetReward((0.01f - rewOcclude) * rewDist * rewScale + rewDist);
        }
    }


    public Vector3 GetExtentInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }
}
