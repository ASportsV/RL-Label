using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using System.Linq;

[Serializable]
public class TaskItem {
    public int id;
    public int[] point;
    public string color;
    public int occ;
    public bool isAgent;
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

    internal bool obW;
    // label parameters
    internal float labelY = 2.5f;
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

    internal List<Task> _tasks;
    internal List<Task> tasks {
        get { return _tasks; }
        set {
            _tasks = value;
            // new sheet
            srd.AddNewSheet(sheetName, techOrders, _tasks);
        }
    }

    List<Tech> techOrders = new List<Tech>();
    int _currentTaskIdx = 0;
    internal int currentTaskIdx { get { return _currentTaskIdx; } }
    public Task CurrentTask => tasks[currentTaskIdx];
    internal Tech CurrentTech => techOrders[currentTaskIdx];
    internal float ansTime = 0;

    //sheet
    SheetReader srd;
    public int userId = 0;

    string _sceneName;
    internal string sceneName {
        get { return _sceneName; }
        set {
            _sceneName = value;
            sheetName = string.Format("{0}_{1}_{2}_{3}_{4}_userId{5}", DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, sceneName, userId);
        }
    }
    string sheetName;
    bool _setUserId = false;
    internal bool setUserId {
        get { return _setUserId; }
        set { 
            if (value != true) return; 
            _setUserId = value; 
            sheetName = string.Format("{0}_{1}_{2}_{3}_{4}_userId{5}", DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, sceneName, userId);
        }
    }

    private void Awake()
    {
        obW = Academy.Instance.EnvironmentParameters.GetWithDefault("ob_w", 1f) == 1.0f;
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 1f) == 1.0f;
        evaluate_metrics = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_metrics", 0f) == 1.0f;
        courtCount = gameObject.scene.GetRootGameObjects().Count(go => go.activeSelf) - 2;
        srd = new SheetReader();
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

    public void getOrderByUserId()
    {
        Tech[][] orders = new[] {
            new[] { Tech.No, Tech.Opti, Tech.Ours },
            new[] { Tech.Opti, Tech.Ours, Tech.No },
            new[] { Tech.Ours, Tech.No, Tech.Opti }
        };
        Tech[] order = orders[userId % 3];
        for(int i = 0; i < 6; ++i)
        {
            techOrders.AddRange(order);
        }
        // for(int i = 0; i < 2; ++i)
        // {
        //     foreach(Tech tech in order)
        //     {
        //         for(int j = 0; j < 3; ++j) 
        //             techOrders.Add(tech);
        //     }
        // }
    }

    public void saveToSheet()
    {
        srd.SetAns(sheetName, currentTaskIdx, ansTime);
    }

    public void NextTask(bool round = false)
    {
        this._currentTaskIdx += 1;
        // in the setting view
        if(round)  {
            this._currentTaskIdx = this._currentTaskIdx % 18;
            return;
        }
        
        if (this._currentTaskIdx >= tasks.Count) this._currentTaskIdx = -1;
    }

}
