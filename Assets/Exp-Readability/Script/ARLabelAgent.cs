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
    Transform cam;
    RectTransform rTransform;
    BufferSensorComponent bsensor;
    RayPerceptionSensorComponent3D forwardRaycast;
    RayPerceptionSensorComponent3D backwardRaycast;

    public Player player;
    Rigidbody m_PlayerRB;

    float minY = 1.4f;
    float yDistThres = 3.0f;
    float maxYspeed = 3f;

    Rigidbody m_Rbody;  //cached on initialization

    // Start is called before the first frame update
    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<ARLabelSettings>();
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    public override void Initialize()
    {
        m_Rbody = GetComponent<Rigidbody>();
        bsensor = GetComponent<BufferSensorComponent>();
        rTransform = GetComponentInChildren<RectTransform>();
        MaxStep = m_ARLabelSettings.MaxSteps * m_ARLabelSettings.numOfDestinations;
        forwardRaycast = GetComponent<RayPerceptionSensorComponent3D>();
        backwardRaycast = GetComponentInChildren<RayPerceptionSensorComponent3D>();
    }

    float maxDistToAgent;
    float minDistToAgent;
    float maxDistToPlayer;
    float minDistToPlayer = 0; // 1.41421f * 0.25f;
    float normalizeDistToAgent;
    float normalizeDistToPlayer;
    private void Start()
    {
        cam = this.transform.parent.parent.Find("Camera");

        Vector3 extent = this.GetExtentInWorld();
        maxDistToAgent = Mathf.Min(extent.x, extent.y);
        minDistToAgent = 0f;
        maxDistToPlayer = 1.2247f * 0.5f + maxDistToAgent * 0.5f;

        normalizeDistToAgent = maxDistToAgent - minDistToAgent;
        normalizeDistToPlayer = maxDistToPlayer - minDistToPlayer;
        m_PlayerRB = player.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        player.Reset();
        var playerPos = player.transform.localPosition;
        var playerVel = player.GetComponent<Rigidbody>().velocity;
        this.transform.localPosition = new Vector3(playerPos.x, minY, playerPos.z);
        m_Rbody.velocity = new Vector3(playerVel.x, 0, playerVel.z);
    }

    /** ------------------ Observation ---------------------**/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.localPosition;
        Vector3 selfVel = m_Rbody.velocity;

        sensor.AddObservation(selfPos.x / m_ARLabelSettings.courtX);
        sensor.AddObservation((selfPos.y - minY) / yDistThres);
        sensor.AddObservation(selfPos.z / m_ARLabelSettings.courtZ);

        // sensor.AddObservation(selfVel.x / m_ARLabelSettings.playerSpeed);
        sensor.AddObservation(selfVel.y / maxYspeed);
        // sensor.AddObservation(selfVel.z / m_ARLabelSettings.playerSpeed);
        sensor.AddObservation(transform.forward);
        sensor.AddObservation(transform.localScale.x);

        GameObject[] others = this.transform.parent.GetComponentsInChildren<Transform>()
            .Where(x => x.CompareTag("player"))
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
            //obs.Add(relativePos.y / yDistThres);
            obs.Add(relativePos.z / m_ARLabelSettings.courtZ);

            Vector3 relativeVel = other.GetComponent<Rigidbody>().velocity - selfVel;
            obs.Add(relativeVel.x / m_ARLabelSettings.playerSpeed);
            //obs.Add(relativeVel.y / maxYspeed);
            obs.Add(relativeVel.z / m_ARLabelSettings.playerSpeed);

            //obs.Add(other.transform.forward.x);
            //obs.Add(other.transform.forward.y);
            //obs.Add(other.transform.forward.z);
            //obs.Add(other.transform.localScale.x);

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
        int scaleSize = actionBuffers.DiscreteActions[0];
        float newScale = Mathf.Clamp(scaleSize == 1
                ? this.transform.localScale.x + 0.05f
                : scaleSize == 2
                ? this.transform.localScale.x - 0.05f
                : this.transform.localScale.x, 0.1f, 1f);

        this.transform.localScale = new Vector3(newScale, newScale, newScale);
        // update radius
        Vector3 extent = this.GetExtentInWorld();
        // forwardRaycast.SphereCastRadius = Mathf.Min(extent.x, extent.z) * 0.5f;
        backwardRaycast.SphereCastRadius = Mathf.Min(extent.x, extent.z) * 0.5f;


        float accChange = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f) * maxYspeed;
        Vector3 playerVel = player.GetComponent<Rigidbody>().velocity;
        m_Rbody.velocity = playerVel;
        float nextYVel = m_Rbody.velocity.y + accChange;
        // should clamp velocity in y
        float nextY = transform.localPosition.y + nextYVel * Time.fixedDeltaTime;
        if (nextY <= minY)
        {
            m_Rbody.velocity = new Vector3(playerVel.x, 0f, playerVel.z);
            transform.localPosition = new Vector3(transform.localPosition.x, minY, transform.localPosition.z);
        }
        else if (nextY >= (minY + yDistThres))
        {
            m_Rbody.velocity = new Vector3(playerVel.x, 0f, playerVel.z);
            transform.localPosition = new Vector3(transform.localPosition.x, minY + yDistThres, transform.localPosition.z);
        }
        else
        {
            m_Rbody.AddForce(new Vector3(0f, accChange, 0f), ForceMode.VelocityChange);
        }

        transform.LookAt(cam.transform);
    }

    /*-----------------------Reward-----------------------*/
    float postiveShape(float x, float maxX = 1.0f)
    {
        x = x / maxX;
        return  1f / (1f + Mathf.Pow((x / (1f - x)), -2));
    }

    float negativeShape(float x, float maxX = 1.0f)
    {
        x = x / maxX;
        return Mathf.Pow(1 - Mathf.Pow(x, 1.5f), 2);
    }

    float rewOcclude(bool fastReject = false)
    {
        // return [0, 0.1, 0.2]
        float rewOcclude = 0;
        Vector3 origin = transform.position;
        Vector3 size = this.GetExtentInWorld();
        float radius = Mathf.Min(size.x, size.y) * 0.5f;
        if(radius < 0.1f) // smaller than the bounding box of the agent
        {
            return rewOcclude;
        }

        //Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;
        int layerMask = 1 << LayerMask.NameToLayer("player");

        // occluded by others
        Vector3 direction = transform.forward;
        RaycastHit m_Hit;

        //if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        //{
        //    ////Bounds hitBounds = m_Hit.collider.bounds;
        //    //// get the hit point
        //    //Vector3 hitPoint = m_Hit.point;
        //    //// get the center point of the hit plane
        //    //Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
        //    //// cal the distance from the intersect point to the center
        //    //float occludeDist = Vector3.Distance(intersectionPoint, m_Hit.collider.bounds.center);

        //    //// normalize
        //    //float normalizeDist = m_Hit.collider.CompareTag("player")
        //    //    ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
        //    //    : (occludeDist - minDistToAgent) / normalizeDistToAgent;

        //    //Debug.DrawRay(origin, hitPoint, new Color(0.8f, 0.5f, 0f));
        //    //Debug.DrawRay(origin, direction * 20, new Color(0f, 0.5f, 0.5f));
        //    rewOcclude += 1f; // (normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        //}

        //if(fastReject && rewOcclude != 0)
        //{
        //    return rewOcclude;
        //}

        // occlude others
        
        direction = -transform.forward;
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        {
            ////Bounds hitBounds = m_Hit.collider.bounds;
            //// get the hit point
            //Vector3 hitPoint = m_Hit2.point;
            //// get the center point of the hit plane
            //Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
            //// cal the distance from the intersect point to the center
            //float occludeDist = Vector3.Distance(intersectionPoint, m_Hit2.collider.bounds.center);

            //// normalize
            //float normalizeDist = m_Hit2.collider.CompareTag("player")
            //    ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
            //    : (occludeDist - minDistToAgent) / normalizeDistToAgent;

            //Debug.DrawRay(origin, hitPoint, new Color(0.8f, 0.5f, 0f));
            //Debug.DrawRay(origin, direction * 20, new Color(0f, 0.5f, 0.5f));

            //print("normalized dist " + normalizeDist + " , moveDist " + occludeDist + ", maxDistToPlayer " + maxDistToPlayer);


            rewOcclude += 1f; //(normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        }

        return rewOcclude / 10f;
    }


    void RewardNoOcclusion ()
    {

        float rewOcclude = this.rewOcclude(true); // [0, 0.1, 0.2]
        if(rewOcclude != 0)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
        else
        {
            float dist = Mathf.Max(this.transform.localPosition.y - minY, 0); // Vector3.Distance(goalPos, this.transform.localPosition);
            float selfSize = transform.localScale.x;
            
            float rewScale = this.postiveShape(selfSize);
            //float rewDist = -0.1f + 0.1f * this.rewDist(dist, yDistThres);
            //rewDist /= 50f; // [0, 0.002]
            float rewDist = this.negativeShape(dist, yDistThres);

            SetReward(0.01f * rewScale * rewDist);
        }
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0)
        {
            return;
        }

        RewardNoOcclusion();

        //float dist = Mathf.Max(this.transform.localPosition.y - minY, 0); // Vector3.Distance(goalPos, this.transform.localPosition);
        //float selfSize = transform.localScale.x;
        //float selfSpeed = m_Rbody.velocity.magnitude;

        //float rewScale = this.rewScale(selfSize);

        //float rewSpeed = this.rewSpeed(selfSpeed, new Vector3(m_ARLabelSettings.playerSpeed, maxYspeed, m_ARLabelSettings.playerSpeed).magnitude);

        //float rewDist = -0.1f + 0.1f * this.rewDist(dist, yDistThres);
        //rewDist /= 50f; // [0, 0.002]

        //float rewOcclude = this.rewOcclude(); // [0, 0.1, 0.2]

        //SetReward((0.01f - rewOcclude) * rewSpeed * rewScale + rewDist);
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

        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = -Input.GetAxis("Horizontal");
    }


    private void OnDrawGizmos()
    {

        //Vector3 origin = transform.position;
        //Vector3 size = this.GetExtentInWorld();
        //float radius = Mathf.Min(size.x, size.y) * 0.5f;
        ////Quaternion rotation = Quaternion.LookRotation(direction);
        //float maxDistance = Mathf.Infinity;
        //int layerMask = 1 << LayerMask.NameToLayer("player");

        //// occluded by others
        //Vector3 direction = transform.forward;
        //RaycastHit m_Hit;
        //float rewOcclude = 0;
        //if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        //{
        //    //Bounds hitBounds = m_Hit.collider.bounds;
        //    // get the hit point
        //    Vector3 hitPoint = m_Hit.point;
        //    // get the center point of the hit plane
        //    Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
        //    // cal the distance from the intersect point to the center
        //    float occludeDist = Vector3.Distance(intersectionPoint, m_Hit.collider.bounds.center);

        //    // normalize
        //    float normalizeDist = m_Hit.collider.CompareTag("player")
        //        ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
        //        : (occludeDist - minDistToAgent) / normalizeDistToAgent;

        //    Debug.DrawRay(origin, hitPoint, new Color(0.8f, 0.5f, 0f));
        //    Debug.DrawRay(origin, direction * 20, new Color(0f, 0.5f, 0.5f));
        //    rewOcclude += (normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        //}

        //// occlude others
        //direction = -transform.forward;
        //if (Physics.SphereCast(origin, radius, direction, out m_Hit2, maxDistance, layerMask))
        //{
        //    //Bounds hitBounds = m_Hit.collider.bounds;
        //    // get the hit point
        //    Vector3 hitPoint = m_Hit2.point;
        //    // get the center point of the hit plane
        //    Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
        //    // cal the distance from the intersect point to the center
        //    float occludeDist = Vector3.Distance(intersectionPoint, m_Hit2.collider.bounds.center);

        //    // normalize
        //    float normalizeDist = m_Hit2.collider.CompareTag("player")
        //        ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
        //        : (occludeDist - minDistToAgent) / normalizeDistToAgent;

        //    Debug.DrawRay(origin, hitPoint - origin, new Color(0.8f, 0.5f, 0f));
        //    Debug.DrawLine(intersectionPoint, m_Hit2.collider.bounds.center, new Color(0f, 0.5f, 0.5f));
        //    Debug.DrawRay(origin, direction * 20, new Color(0f, 0.5f, 0.5f));
        //    Gizmos.color = new Color(0.8f, 0.5f, 0f);
        //    Gizmos.DrawSphere(origin, radius);
        //    Gizmos.DrawSphere(intersectionPoint, radius);

        //    print("normalized dist " + normalizeDist + " , moveDist " + occludeDist + ", maxDistToPlayer " + maxDistToPlayer);


        //    rewOcclude += (normalizeDist < 0.5 ? 1f : 0f); // overlap > 50%
        //}
    }

    private void OnDrawGizmosSelected()
    {
        //Vector3 origin = transform.position;
        //Vector3 size = this.GetExtentInWorld();
        //float radius = Mathf.Min(size.x, size.y) * 0.5f;

        // occluded by others
        //Vector3 direction = transform.forward;

        //var startPositionWorld = rayOutput.StartPositionWorld;
        //var endPositionWorld = rayOutput.EndPositionWorld;
        //var rayDirection = endPositionWorld - startPositionWorld;
        //rayDirection *= rayOutput.HitFraction;

        //// hit fraction ^2 will shift "far" hits closer to the hit color
        //var lerpT = rayOutput.HitFraction * rayOutput.HitFraction;
        //var color = Color.Lerp(rayHitColor, rayMissColor, lerpT);
        //color.a *= alpha;

        //Gizmos.color = new Color(0f, 1f, 0f);
        //Gizmos.DrawRay(origin, direction * 20f);

        //direction = -transform.forward;
        //Gizmos.DrawRay(origin, direction * 20f);

    }

}
