using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid = -1;

    //Vector3 destination;
    RVOSettings m_RVOSettings;
    Rigidbody m_Rbody;
    /** Random number generator. */
    private System.Random m_random = new System.Random();
    public Transform player;
    
    Vector3[] _positions;
    public Vector3[] positions
    {
        set
        {
            _positions = value;

            velocities = new Vector3[_positions.Length];
            for (int i = 0; i < _positions.Length - 1; ++i)
            {
                Vector3 cur = _positions[i];
                Vector3 next = _positions[i + 1];
                Vector3 vel = (next - cur) / timeStep;
                velocities[i] = vel;
            }
            velocities[_positions.Length - 1] = Vector3.zero;
        }

    }
    public Vector3[] velocities;


    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        player = transform.Find("player");
        m_Rbody = player.GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        resetDestination();
    }

    public Vector3 velocity => velocities[currentStep];

    public bool reached()
    {
        return false;
        //return Vector3.Distance(transform.localPosition, destination) < 0.1f;
    }

    public void resetDestination()
    {
        //if(m_RVOSettings.CrossingMode)
        //{
        //    float rx = UnityEngine.Random.value * 1f - 0.5f;
        //    float rz = UnityEngine.Random.value * 1f - 0.5f;
        //    //destination = new Vector3(-destination.x + rx, destination.y, -destination.z + rz);

        //    destination = new Vector3(
        //        -transform.localPosition.x + rx, 
        //        transform.localPosition.y, 
        //        -transform.localPosition.z + rz);
        //}
        //else
        //{
        //    float rx = m_RVOSettings.courtX -1f- UnityEngine.Random.value;
        //    float rz = transform.localPosition.z; //+ UnityEngine.Random.value * 1f - 0.5f;
        //    destination = new Vector3(
        //        rx,
        //        transform.localPosition.y,
        //        rz
        //    );
        //}
    }


    private float time = 0.0f;
    private float timeStep = 0.04f;
    public int currentStep = 0;
    // Update is called once per frame
    private void FixedUpdate()
    {

        time += Time.fixedDeltaTime;

        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }

        if (currentStep < _positions.Length)
        {
            transform.localPosition = _positions[currentStep];
            player.transform.forward = velocities[currentStep].normalized;
        }

        //Vector2 pos = Simulator.Instance.getAgentPosition(sid);
        //Vector2 vel = Simulator.Instance.getAgentPrefVelocity(sid);

        //transform.localPosition = new Vector3(pos.x(), transform.localPosition.y, pos.y());
        ////m_Rbody.velocity = new Vector3(vel.x(), 0, vel.y());

        //if(!m_RVOSettings.CrossingMode)
        //{
        //    transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
        //}
        //else
        //{
        //    if (!reached())
        //        transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
        //}

        //// update prefVel
        //Vector2 goalVector = new Vector2(destination.x, destination.z) - Simulator.Instance.getAgentPosition(sid);

        //Simulator.Instance.setAgentPrefVelocity(sid, goalVector);
        //if(!m_RVOSettings.CrossingMode && accTime >= m_RVOSettings.parallelModeUpdateFreq)
        //{
        //    int rank = (1+Array.IndexOf(transform.parent.GetComponentsInChildren<RVOplayer>().OrderByDescending(p => p.transform.localPosition.x).ToArray(), this)) - (int) (m_RVOSettings.numOfPlayer * 0.5);
        //    Simulator.Instance.setAgentMaxSpeed(sid, m_RVOSettings.playerSpeed + m_RVOSettings.playerSpeed *  rank / m_RVOSettings.numOfPlayer);

        //    accTime -= m_RVOSettings.parallelModeUpdateFreq;
        //}

        ///* Perturb a little to avoid deadlocks due to perfect symmetry. */
        //float angle = (float)m_random.NextDouble() * 2.0f * (float)Math.PI;
        //float dist = (float)m_random.NextDouble() * 0.001f;

        //Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
        //                                             dist *
        //                                             new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));

        //accTime += Time.fixedDeltaTime;
    }
}