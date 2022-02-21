using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;


[System.Serializable]
public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;
    public bool sync;

    public bool CrossingMode = true;
    public float parallelModeUpdateFreq = 3f;

    public int numOfPlayer;
    public int maxNumOfPlayer = 10; // = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
    public int minNumOfPlayer = 6; // = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);

    public float playerSpeed = 1f;

    public int courtX = 14;
    public int courtZ = 7;
}
