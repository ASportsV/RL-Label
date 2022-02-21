using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;
    public bool sync;

    public bool CrossingMode = true;
    public float parallelModeUpdateFreq = 3f;

    public int numOfPlayer;
    public int maxNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
    public int minNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);

    public float playerSpeed = Academy.Instance.EnvironmentParameters.GetWithDefault("playerSpeed", 1.0f);

    public int courtX = 14;
    public int courtZ = 7;
}
