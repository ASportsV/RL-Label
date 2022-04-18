using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using System.Linq;

[Serializable]
public class TaskItem {
    public int id;
    public int point;
    public string color;
    public int occ;
}

[Serializable]
public class Task
{
    public int track_id;
    public string type;
    public string Q;
    public string A;
    public List<TaskItem> setting;
}

[Serializable]
public class TaskList
{
    public List<Task> tasks;
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
    public float playerSpeedX = 1f;
    public float playerSppedZ = 1f;

    public int courtX = 14;
    public int courtZ = 7;

    internal float minZInCam;
    internal float maxZInCam;

    internal bool obW = false;
    // label parameters
    internal float labelY = 1.8f;
    internal float xzDistThres;
    internal float maxLabelSpeed;
    internal float moveUnit;
    internal float moveSmooth;
    internal Queue<int> testingTrack;

    internal bool evaluate = false;
    internal bool evaluate_metrics = false;
    int finished = 0;
    int courtCount = 8;

    // UI
    internal bool sceneFinished = false;
    internal bool sceneStarted = false;

    internal List<Task> tasks;
    List<Tech> techOrders = new List<Tech>();
    int _currentTaskIdx = 0;
    internal int currentTaskIdx { get { return _currentTaskIdx; } }
    internal Task CurrentTask => tasks[currentTaskIdx];
    internal Tech CurrentTech => techOrders[currentTaskIdx];
    internal float ansTime = 0;

    private void Awake()
    {
        obW = Academy.Instance.EnvironmentParameters.GetWithDefault("ob_w", 0f) == 1.0f;
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 0f) == 1.0f;
        evaluate_metrics = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_metrics", 0f) == 1.0f;
        courtCount = gameObject.scene.GetRootGameObjects().Count(go => go.activeSelf) - 2;

        // decide which order the user use
        var orders = getOrderByUserId(0);
        for(int i = 0; i < 12; ++i) // 3 * 12 trials
        {
            techOrders.AddRange(orders);
        }
    }

    public void FinishACourt()
    {
        finished += 1;
        if(finished >= courtCount)
        {
            finished = 0;
            Academy.Instance.StatsRecorder.Add("_test/_test_end", 1.0f);
        }
    }

    Tech[] getOrderByUserId(int userId)
    {
        Tech[] order1 = new[] { Tech.No, Tech.Opti, Tech.Ours };
        Tech[] order2 = new[] { Tech.Opti, Tech.Ours, Tech.No };
        Tech[] order3 = new[] { Tech.Ours, Tech.No, Tech.Opti };

        return order2;
    }

    public void NextTask()
    {
        this._currentTaskIdx += 1;
        if (this._currentTaskIdx > tasks.Count) this._currentTaskIdx = -1;
    }

}
