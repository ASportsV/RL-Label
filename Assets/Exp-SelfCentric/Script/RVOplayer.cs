using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid = -1;
    public int currentStep = 0;

    //Vector3 destination;
    //RVOSettings m_RVOSettings;
    //Rigidbody m_Rbody;
    /** Random number generator. */
    //private System.Random m_random = new System.Random();
    public Transform player;
    
    public Vector3[] positions;
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
        if(idx < positions.Count())
        {
            currentStep = idx;
            transform.localPosition = positions[idx];
            player.transform.forward = velocities[idx].normalized;
        }
    }
}