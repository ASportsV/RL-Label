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

    public RVOplayer player;
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
        cam = transform.parent.parent.parent.Find("Camera").GetComponent<Camera>();
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
        player.resetDestination();
        this.transform.localPosition = new Vector3(0f, minY, 0f);
        m_Rbody.velocity = Vector3.zero;
    }

    Vector3 velocity => player.velocity;
 

    /** ------------------ Observation ---------------------**/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {
        Vector3 selfPos = transform.position;
        Vector3 selfVel = velocity;
        Vector3 localPosition = selfPos - court.position;

        sensor.AddObservation(localPosition.x / m_RVOSettings.courtX);
        //sensor.AddObservation((selfPos.y - minY) / yDistThres);
        sensor.AddObservation(localPosition.z / m_RVOSettings.courtZ);
        
        Vector3 distToGoal = selfPos - player.transform.position;
        sensor.AddObservation(distToGoal / m_RVOSettings.courtX);
        //sensor.AddObservation(distToGoal / yDistThres);
        sensor.AddObservation(distToGoal / m_RVOSettings.courtZ);

        sensor.AddObservation(transform.forward);

        foreach (Transform other in transform.parent.parent)
        {

            foreach(Transform child in other)
            {
                List<float> obs = new List<float>();
                // 2 + 4
                if(child.CompareTag("player"))
                {
                    obs.Add(1); obs.Add(0);
                } 
                else
                {
                    obs.Add(0); obs.Add(1);
                }

                if (GameObject.ReferenceEquals(child.gameObject, gameObject)) continue;
                Vector3 relativePos = child.position - selfPos;

                obs.Add(relativePos.x / m_RVOSettings.courtX);
                //obs.Add(relativePos.y / yDistThres);
                obs.Add(relativePos.z / m_RVOSettings.courtZ);

                Vector3 vel = child.CompareTag("player")
                        ? child.parent.GetComponent<RVOplayer>().velocity
                        : child.GetComponent<RVOLabelAgent>().velocity;

                Vector3 relativeVel = vel - selfVel;
                obs.Add(relativeVel.x / m_RVOSettings.playerSpeed);
                //obs.Add(relativeVel.y / maxYspeed);
                obs.Add(relativeVel.z / m_RVOSettings.playerSpeed);

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

        transform.localPosition = new Vector3(newX, minY, newZ);
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

    private void FixedUpdate()
    {
        //transform.localPosition = new Vector3(player.transform.localPosition.x, transform.localPosition.y, player.transform.localPosition.z);
        transform.LookAt(cam.transform);
    }

    void UpdateReward(int academyStepCount)
    {
        if (academyStepCount == 0)
        {
            return;
        }

        if(player.reached())
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
        int labelLayerMask = 1 << LayerMask.NameToLayer("label");
        int playerLayerMask = 1 << LayerMask.NameToLayer("player");
        Vector3 direction = transform.forward;

        float rew = 0f;
        // occluded by labels
        RaycastHit m_Hit;
        player.gameObject.layer = LayerMask.NameToLayer("Default");
        if (Physics.SphereCast(origin, radius, direction, out m_Hit, maxDistance, labelLayerMask))
        {
            // [0, 1]
            float relativeSpeed = (velocity - m_Hit.collider.GetComponent<RVOLabelAgent>().velocity).sqrMagnitude / m_RVOSettings.playerSpeed;
            rew = -0.1f * this.negativeShape(relativeSpeed);
        }
        
        // occluding players
        if (Physics.SphereCast(origin, radius, -direction, out m_Hit, maxDistance, playerLayerMask))
        {
            // [0, 1]
            float relativeSpeed = (velocity - m_Hit.collider.transform.parent.GetComponent<RVOplayer>().velocity).sqrMagnitude / m_RVOSettings.playerSpeed;
            rew += -0.1f * this.negativeShape(relativeSpeed);
        }
        
        // no occlusion
        if(rew == 0)
        {
            float dist = Vector3.Distance(player.transform.position, transform.position);
            // [0, 0.01]
            float rewDist = this.negativeShape(dist, 4.24f);
            rewDist /= 100;
            rew += (0.01f - rewDist);
        }
        AddReward(rew);

        player.gameObject.layer = LayerMask.NameToLayer("player");
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
