using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

struct Task
{
    public int sceneIdx;
    public string task;
 
    public Task(int sIdx, string q)
    {
        task = q;
        sceneIdx = sIdx;
    }
}

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

    internal bool evaluate = false;

    internal bool sceneFinished = false;
    internal bool sceneStarted = false;

    internal Queue<int> testingScenes;
    internal List<Task> tasks;
    internal int currentTaskIdx = 0;


    private void Awake()
    {
        maxNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
        minNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);
    }
}
