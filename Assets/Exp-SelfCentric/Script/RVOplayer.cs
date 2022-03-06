using System;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid = -1;
    public int currentStep = 0;

    public Transform player;
    
    public Vector3[] positions;
    public Vector3[] velocities;

    private void Awake()
    {
        player = transform.Find("player_parent/player");
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