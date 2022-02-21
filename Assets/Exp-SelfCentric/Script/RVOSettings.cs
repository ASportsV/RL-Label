using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;
    public bool sync;

    public bool CrossingMode = true;
    public float parallelModeUpdateFreq = 3f;


    public int numOfPlayer;
    public int maxNumOfPlayer = 10;

    public float playerSpeed = 4.0f;

    public int courtX = 14;
    public int courtZ = 7;
}
