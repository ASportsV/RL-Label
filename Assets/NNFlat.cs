using Unity.MLAgents;           //for Agent class
using Unity.MLAgents.Actuators; //for OnActionReceived(ActionBuffers...
using Unity.MLAgents.Sensors;   //for CollectObservations(VectorSensor...
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MeshCollider))]
[RequireComponent(typeof(DecisionRequester))]
public class NNFlat : Agent
{
    [System.Serializable]
    public class RewardInfo
    {
        public float nomovement = -0.1f;
        public float mult_forward = 0.001f;
        public float mult_backward = -0.001f;
        public float mult_road = 0.01f;
        public float mult_gravel = 0.001f;
        public float mult_barrier = -0.1f;
        public float grass = -1.0f;         //OnCollisionEnter punish + endepisode
        public float mult_car = -0.5f;      //OnCollisionEnter punish + endepisode
        public float rightdirection = 0.0001f;
    }
    public float Movespeed = 30;
    public float Turnspeed = 100;
    public RewardInfo rwd = new RewardInfo();
    public bool doEpisodes = true;
    private Rigidbody rb = null;
    private Vector3 recall_position;
    private Quaternion recall_rotation;
    private Bounds bnd;

    public override void Initialize()
    {
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
                AddReward(rwd.nomovement);
                break;
            case 1:
                rb.AddRelativeForce(Vector3.back * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //back
                AddReward(mag * rwd.mult_backward);
                break;
            case 2:
                rb.AddRelativeForce(Vector3.forward * Movespeed * Time.deltaTime, ForceMode.VelocityChange); //forward
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
    public override void CollectObservations(VectorSensor sensor)
    {
        //Note: BehaviourParameters component - set VectorObservation size to 3, because here
        //      we are adding 3 observations manually

        //1. cast rays to both sides of car
        int layermask = 1 << 6; //6 is carperception 
        Vector3 posbottomcar = new Vector3(this.transform.position.x, this.transform.position.y + bnd.size.y + 0.5f, this.transform.position.z);
        Vector3 poswhite = new Vector3(posbottomcar.x + 1.0f, posbottomcar.y, posbottomcar.z); ;
        Vector3 posyellow = new Vector3(posbottomcar.x - 1.0f, posbottomcar.y, posbottomcar.z);
        RaycastHit[] hitWhite = Physics.SphereCastAll(posbottomcar, 0.2f, poswhite, 20f, layermask);
        RaycastHit[] hitYellow = Physics.SphereCastAll(posbottomcar, 0.2f, posyellow, 20f, layermask);

        //2. did white hit BarrierWhite, and yellow hit BarrierYellow?
        bool isWhite = false;
        bool isYellow = false;
        bool isRightDirection;
        foreach (RaycastHit hit in hitWhite)
        {
            if (hit.collider.gameObject.CompareTag("BarrierWhite") == true)
            {
                isWhite = true;
                break;
            }
        }        
        foreach (RaycastHit hit in hitYellow)
        {
            if (hit.collider.gameObject.CompareTag("BarrierYellow") == true)
            {
                isYellow = true;
                break;
            }
        }
        isRightDirection = isWhite || isYellow;
                
        //3. manually send 3 observations to the neural network
        sensor.AddObservation(isRightDirection);
        if (isRightDirection == true)
        {
            AddReward(rwd.rightdirection);
        }
        sensor.AddObservation(isWhite);
        sensor.AddObservation(isYellow);
    }
    private void OnCollisionEnter(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.gameObject.CompareTag("Grass") == true)
        {
            AddReward(rwd.grass);               //punish + end
            if (doEpisodes == true)
                EndEpisode();
        }
        else if (collision.gameObject.CompareTag("Car") == true)
        {
            AddReward(mag * rwd.mult_car);      //punish + end
            if (doEpisodes == true)
                EndEpisode();
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        float mag = collision.relativeVelocity.sqrMagnitude;

        if (collision.gameObject.CompareTag("BarrierWhite") == true
            || collision.gameObject.CompareTag("BarrierYellow") == true)
        {
            AddReward(mag * rwd.mult_barrier);  //punish
        }
        else if (collision.gameObject.CompareTag("Road") == true)
        {
            AddReward(mag * rwd.mult_road);     //reward
        }
        else if (collision.gameObject.CompareTag("Gravel") == true)
        {
            AddReward(mag * rwd.mult_gravel);   //reward
        }
    }
    private bool isWheelsDown()
    {
        //raycast down from car = ground should be closely there
        return Physics.Raycast(this.transform.position, -this.transform.up, bnd.size.y * 0.55f);
    }
}
