using System;
using System.Collections;
using System.Collections.Generic;
using RVO;
using Vector2 = RVO.Vector2;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid = -1;

    Vector3 destination;
    RVOSettings m_RVOSettings;
    Rigidbody m_Rbody;
    /** Random number generator. */
    private System.Random m_random = new System.Random();
    public Transform player;

    private void Awake()
    {
        m_RVOSettings = FindObjectOfType<RVOSettings>();
        player = transform.Find("player");
    }

    // Start is called before the first frame update
    void Start()
    {
        destination = new Vector3(-transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
    }

    public Vector3 velocity
    {
        get {
            var vel2d = Simulator.Instance.getAgentVelocity(sid);
            return new Vector3(vel2d.x(), 0, vel2d.y());
        }
    }

    public bool reached()
    {
        return Vector3.Distance(transform.localPosition, destination) < 0.1f;
    }

    public void resetDestination()
    {
        float rx = UnityEngine.Random.value * 1f - 0.5f;
        float rz = UnityEngine.Random.value * 1f - 0.5f;
        destination = new Vector3(-destination.x + rx, destination.y, -destination.z + rz);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (Vector3.Distance(transform.localPosition, destination) < 0.1f)
        {
            destination = new Vector3(-transform.localPosition.x, transform.localPosition.y, -transform.localPosition.z);
        }

        if (sid >= 0)
        {
            Vector2 pos = Simulator.Instance.getAgentPosition(sid);
            Vector2 vel = Simulator.Instance.getAgentPrefVelocity(sid);
            transform.localPosition = new Vector3(pos.x(), transform.localPosition.y, pos.y());

            if (Math.Abs(vel.x()) > 0.01f && Math.Abs(vel.y()) > 0.01f)
                transform.forward = new Vector3(vel.x(), 0, vel.y()).normalized;
        }

        // update prefVel
        Vector2 goalVector = new Vector2(destination.x, destination.z) - Simulator.Instance.getAgentPosition(sid);// GameMainManager.Instance.mousePosition - Simulator.Instance.getAgentPosition(sid);
        if (RVOMath.absSq(goalVector) > m_RVOSettings.playerSpeed)
        {
            goalVector = RVOMath.normalize(goalVector) * m_RVOSettings.playerSpeed;
        }

        Simulator.Instance.setAgentPrefVelocity(sid, goalVector);

        /* Perturb a little to avoid deadlocks due to perfect symmetry. */
        float angle = (float)m_random.NextDouble() * 2.0f * (float)Math.PI;
        float dist = (float)m_random.NextDouble() * 0.001f;

        Simulator.Instance.setAgentPrefVelocity(sid, Simulator.Instance.getAgentPrefVelocity(sid) +
                                                     dist *
                                                     new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)));
    }
}