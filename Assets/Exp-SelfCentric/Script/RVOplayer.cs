using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid = -1;
    private float timeStep = 0.04f;
    public int currentStep = 0;

    //Vector3 destination;
    //RVOSettings m_RVOSettings;
    //Rigidbody m_Rbody;
    /** Random number generator. */
    //private System.Random m_random = new System.Random();
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
        //m_RVOSettings = FindObjectOfType<RVOSettings>();
        player = transform.Find("player");
        //m_Rbody = player.GetComponent<Rigidbody>();
    }

    public Vector3 velocity => velocities[currentStep];

    public void step(int idx)
    {
        if(idx < _positions.Count())
        {
            currentStep = idx;
            transform.localPosition = _positions[idx];
            player.transform.forward = velocities[idx].normalized;
        }
    }
}