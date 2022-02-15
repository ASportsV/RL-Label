using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class RVOLabelAgent : Agent
{

    RVOSettings m_RVOSettings;

    public RVOplayer PlayerLabel;
    public Camera cam;
    public Transform court;
    Rigidbody m_Rbody;
    RectTransform rTransform;

    // sensor
    BufferSensorComponent bSensor;
    RayPerceptionSensorComponent3D raycastSensor;

    float minY = 1f;
    float yDistThres = 3.0f;
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
        m_Rbody = GetComponent<Rigidbody>();
        rTransform = GetComponentInChildren<RectTransform>();
        MaxStep = m_RVOSettings.MaxSteps;
        bSensor = GetComponent<BufferSensorComponent>();
    }

    public override void OnEpisodeBegin()
    {
        PlayerLabel.resetDestination();
        this.transform.localPosition = new Vector3(0f, minY, 0f);
        m_Rbody.velocity = Vector3.zero;
    }

    Vector3 velocity => PlayerLabel.velocity;
 

    /** ------------------ Observation ---------------------**/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfVel = velocity;

        Vector3 localPosition = selfPos - court.position;
        
        // 2 + 3 + 3
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
            if (GameObject.ReferenceEquals(other.gameObject, transform.parent)) continue;
            foreach(Transform child in other)
            {
                List<float> obs = new List<float>();
                // 2 + 2 + 2
                if(child.CompareTag("player"))
                {
                    obs.Add(1); obs.Add(0);
                } 
                else
                {
                    obs.Add(0); obs.Add(1);
                }

                Vector3 relativePos = child.position - selfPos;

                obs.Add(relativePos.x / m_RVOSettings.courtX);
                //obs.Add(relativePos.y / yDistThres);
                obs.Add(relativePos.z / m_RVOSettings.courtZ);

                Vector3 vel = child.CompareTag("player")
                        ? child.parent.GetComponent<RVOplayer>().velocity
                        : child.GetComponent<RVOLabelAgent>().velocity;

                Vector3 relativeVel = vel - selfVel;
                obs.Add(relativeVel.x / (2 * m_RVOSettings.playerSpeed));
                //obs.Add(relativeVel.y / maxYspeed);
                obs.Add(relativeVel.z / (2 * m_RVOSettings.playerSpeed));

                bSensor.AppendObservation(obs.ToArray());
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        this.OBIn3DWorldSpace(sensor);
    }

    /*-----------------------Action-----------------------*/
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //var continuousActions = actionBuffers.ContinuousActions;
        //var forward = Mathf.Clamp(continuousActions[0], -1f, 1f);
        //var right = Mathf.Clamp(continuousActions[1], -1f, 1f);
        //var up = Mathf.Clamp(continuousActions[2], -1f, 1f);

        //var dirToGo = new Vector3(right, up, forward);
        var moveZ = actionBuffers.DiscreteActions[0] == 1
            ? +0.02f
            : actionBuffers.DiscreteActions[0] == 2
            ? -0.02f
            : 0;

        float newZ = Mathf.Clamp(transform.localPosition.z + moveZ, -3f, 0f);

        var moveX = actionBuffers.DiscreteActions[1] == 1
            ? +0.02f
            : actionBuffers.DiscreteActions[1] == 2
            ? -0.02f
            : 0;
        float newX = Mathf.Clamp(transform.localPosition.x + moveX, -3f, 3f);


        var moveY = actionBuffers.DiscreteActions[2] == 1
            ? +0.02f
            : actionBuffers.DiscreteActions[1] == 2
            ? -0.02f
            : 0;
        float newY = Mathf.Clamp(transform.localPosition.y + moveY, minY, minY + yDistThres);

        transform.localPosition = new Vector3(newX, newY, newZ);
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

    float rewOcclude()
    {
        // return [0, 0.1]
        float rewOcclude = 0;
        Vector3 origin = transform.position;
        float radius = 0.3f;
        float maxDistance = Mathf.Infinity;
        int layerMask = 1 << LayerMask.NameToLayer("label");

        // occluded by others
        Vector3 direction = transform.forward;
        RaycastHit m_Hit;
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, layerMask))
        {
            rewOcclude += 1f;
        }

        return rewOcclude / 10f;
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0)
        {
            return;
        }

        if(PlayerLabel.reached())
        {
            SetReward(1.0f);
            EndEpisode();
            return;
        }

        // being occluded
        // return [0, 0.1]
        Vector3 origin = transform.position;
        float radius = 0.3f;
        float maxDistance = Mathf.Infinity;
        Vector3 direction = transform.forward;

        float rew = 0f;
        // occluded by labels
        RaycastHit m_Hit;
        int labelLayerMask = 1 << LayerMask.NameToLayer("label");

        PlayerLabel.player.gameObject.layer = LayerMask.NameToLayer("Default");
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, labelLayerMask))
        {
            // [0, 1]
            Vector3 hitVel = m_Hit.collider.GetComponent<RVOLabelAgent>().velocity;
            Vector3 relativeSpeed = hitVel - velocity;
            float sqrtMat = Mathf.Min(relativeSpeed.sqrMagnitude, 2 * m_RVOSettings.playerSpeed);
            float normalizedSqrtMat = sqrtMat / (4 * m_RVOSettings.playerSpeed * m_RVOSettings.playerSpeed);
            float transferedSqrtMat = this.negativeShape(normalizedSqrtMat);
            rew = -0.1f * transferedSqrtMat;
        }
        
        // occluding players
        int playerLayerMask = 1 << LayerMask.NameToLayer("player") | labelLayerMask;
        if (Physics.SphereCast(origin, radius, -direction, out m_Hit, maxDistance, playerLayerMask))
        {
            // [0, 1]
            Vector3 hitVel = m_Hit.collider.transform.parent.GetComponent<RVOplayer>().velocity;
            Vector3 relativeSpeed = hitVel - velocity;
            float sqrtMat = Mathf.Min(relativeSpeed.sqrMagnitude, 2 * m_RVOSettings.playerSpeed);
            float normalizedSqrtMat = sqrtMat / (4 * m_RVOSettings.playerSpeed * m_RVOSettings.playerSpeed);
            float transferedSqrtMat = this.negativeShape(normalizedSqrtMat);

            rew += -0.1f * transferedSqrtMat;
        }
        
        // no occlusion
        if(rew == 0)
        {
            float dist = Vector2.Distance(
                transform.position,
                new Vector3(PlayerLabel.transform.position.x, minY, PlayerLabel.transform.position.z)
            );

            // [0, 0.01]
            //float rewDist = this.negativeShape(dist, 4.24f); // 3 * sqrt2
            float rewDist = this.negativeShape(dist, 5.2f);
            rewDist /= 100;
            rew += rewDist;
        }
        AddReward(rew);

        PlayerLabel.player.gameObject.layer = LayerMask.NameToLayer("player");

        transform.LookAt(cam.transform);
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

        //var continuousActionsOut = actionsOut.ContinuousActions;
        //continuousActionsOut[0] = -Input.GetAxis("Horizontal");
    }
}
