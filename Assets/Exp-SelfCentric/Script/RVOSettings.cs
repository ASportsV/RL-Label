using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


[System.Serializable]
public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;
    public bool sync;

    public float parallelModeUpdateFreq = 3f;
    public int numOfPlayer;
    public float playerSpeed = 1f;

    internal bool CrossingMode = true;
    internal int maxNumOfPlayer;
    internal int minNumOfPlayer;

    public int courtX = 14;
    public int courtZ = 7;

    private void Awake()
    {
        CrossingMode = Academy.Instance.EnvironmentParameters.GetWithDefault("crossing", 1.0f) != 0f;
        maxNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
        minNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);
    }
}
