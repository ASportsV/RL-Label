using UnityEngine;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(DecisionRequester))]
public class ARTrack : Agent
{
    [System.Serializable]
    public class RewardInfo
    {                                           
        public float mult_forward = 0.001f; 
        public float mult_barrier = -0.8f; 
        public float mult_car = -0.5f; 
    }

    CarLabelSettings m_ARLabelSettings;
    BufferSensorComponent m_BufferSensor;

    public float Movespeed = 30;
    public float Turnspeed = 100;
    public RewardInfo rwd = new RewardInfo();

    private Rigidbody rb = null;   
    private Vector3 recall_position;
    private Quaternion recall_rotation;
    private Bounds bnd;

    private void Awake()
    {
        m_ARLabelSettings = FindObjectOfType<CarLabelSettings>();
    }

    public override void Initialize()
    {
        MaxStep = m_ARLabelSettings.MaxSteps;
        m_BufferSensor = GetComponent<BufferSensorComponent>();

        rb = this.GetComponent<Rigidbody>();
        rb.drag = 1;
        rb.angularDrag = 5;
        rb.interpolation = RigidbodyInterpolation.Extrapolate;

        this.GetComponent<MeshCollider>().convex = true;        
        this.GetComponent<DecisionRequester>().DecisionPeriod = 1;
        bnd = this.GetComponent<MeshRenderer>().bounds;

        recall_position = new Vector3(this.transform.position.x, this.transform.position.y, this.transform.position.z);
        recall_rotation = new Quaternion(this.transform.rotation.x, this.transform.rotation.y, this.transform.rotation.z, this.transform.rotation.w);
    }
    public override void OnEpisodeBegin()
    {
        rb.velocity = Vector3.zero;
        this.transform.position = recall_position;
        this.transform.rotation = recall_rotation;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float distanceThres = 30f;
        var others = transform.parent.GetComponentsInChildren<ARTrack>()
            .Where(c => !GameObject.ReferenceEquals(c.gameObject, gameObject) && 
                Vector3.Distance(c.transform.localPosition, transform.localPosition) < distanceThres); // the same distance of the raycast sensor
            //.OrderBy(c => Vector3.Distance(c.transform.localPosition, transform.localPosition));
            
        //System.Array.Sort(others, (a, b) => (Vector3.Distance(a.transform.local, transform.position)).CompareTo(Vector3.Distance(b.transform.position, transform.position)));
        int numBulletAdded = 0;
        foreach(var car in others)
        {
            if (numBulletAdded >= 10) break;
            Rigidbody otherRB = car.GetComponent<Rigidbody>();
            Vector3 relativePos = otherRB.transform.localPosition - transform.localPosition;
            Vector3 relativeVel = otherRB.velocity - rb.velocity;

            float[] obs = {
                relativePos.x / distanceThres,
                relativePos.z / distanceThres,
                relativeVel.x / Movespeed,
                relativeVel.z / Movespeed
            };
            m_BufferSensor.AppendObservation(obs);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        //decisionrequestor component needed
        //  space type: discrete
        //      branches size: 2 move, turn
        //          branch 0 size: 3  fwd, nomove, back
        //          branch 1 size: 3  left, noturn, right

        if (isWheelsDown() == false)
            return;

        float mag = Mathf.Abs(rb.velocity.sqrMagnitude);

        switch (actions.DiscreteActions.Array[0])   //move
        {
            case 0:
                break;
            case 1:
                rb.AddRelativeForce(Vector3.back * Movespeed * Time.fixedDeltaTime, ForceMode.VelocityChange); //back
                break;
            case 2:
                rb.AddRelativeForce(Vector3.forward * Movespeed * Time.fixedDeltaTime, ForceMode.VelocityChange); //forward
                AddReward(mag * rwd.mult_forward);
                break;
        }

        switch (actions.DiscreteActions.Array[1])   //turn
        {
            case 0:
                break;
            case 1:
                this.transform.Rotate(Vector3.up, -Turnspeed * Time.deltaTime); //left
                break;
            case 2:
                this.transform.Rotate(Vector3.up, Turnspeed * Time.deltaTime); //right
                break;
        }
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        //Purpose:  for me to simulate the brain actions (I control the car with the keyboard)
        actionsOut.DiscreteActions.Array[0] = 0;
        actionsOut.DiscreteActions.Array[1] = 0;

        float move = Input.GetAxis("Vertical");     // -1..0..1  WASD arrowkeys
        float turn = Input.GetAxis("Horizontal");

        if (move < 0)
            actionsOut.DiscreteActions.Array[0] = 1;    //back
        else if (move > 0)
            actionsOut.DiscreteActions.Array[0] = 2;    //forward

        if (turn < 0)
            actionsOut.DiscreteActions.Array[1] = 1;    //left
        else if (turn > 0)
            actionsOut.DiscreteActions.Array[1] = 2;    //right
    }
    private void OnCollisionEnter(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.gameObject.CompareTag("BarrierWhite") == true
            || collision.gameObject.CompareTag("BarrierYellow") == true)
        {
            AddReward(mag * rwd.mult_barrier);
            if (m_ARLabelSettings.doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Car") == true)
        {
            AddReward(mag * rwd.mult_car);
            if (m_ARLabelSettings.doEpisodes == true)
                EndEpisode();
        }
    }
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}
