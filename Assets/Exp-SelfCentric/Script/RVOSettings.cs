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

enum Tech
{
    Ours,
    Opti,
    No,
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
    List<Tech> techOrders = new List<Tech>();
    int _currentTaskIdx = 0;
    internal int currentTaskIdx { get { return _currentTaskIdx; } }
    internal Task CurrentTask => tasks[currentTaskIdx];
    internal Tech CurrentTech => techOrders[currentTaskIdx];

    private void Awake()
    {
        maxNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("maxPlayerNum", 10);
        minNumOfPlayer = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("minPlayerNum", 6);

        // decide which order the user use
        var orders = getOrderByUserId(0);
        for(int i = 0; i < 12; ++i) // 3 * 12 trials
        {
            techOrders.AddRange(orders);
        }
    }

    Tech[] getOrderByUserId(int userId)
    {
        Tech[] order1 = new[] { Tech.No, Tech.Opti, Tech.Ours };
        Tech[] order2 = new[] { Tech.Opti, Tech.Ours, Tech.No };
        Tech[] order3 = new[] { Tech.Ours, Tech.No, Tech.Opti };

        return order3;
    }

    public void NextTask()
    {
        this._currentTaskIdx += 1;
        if (this._currentTaskIdx > tasks.Count) this._currentTaskIdx = -1;
    }

}
