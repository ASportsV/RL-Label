using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


[System.Serializable]
public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;
    public bool sync;

    public int numOfPlayer;
    public float playerSpeedX = 1f;
    public float playerSppedZ = 1f;

    internal int maxNumOfPlayer;
    internal int minNumOfPlayer;

    public int courtX = 14;
    public int courtZ = 7;

    internal float minZInCam;
    internal float maxZInCam;

    internal float labelY = 1.8f;

    internal bool evaluate = true;

    private void Awake()
    {
        maxNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
        minNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);
    }
}
