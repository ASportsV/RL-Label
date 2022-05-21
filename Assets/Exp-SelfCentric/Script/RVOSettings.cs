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
    internal bool small;
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

    public GameObject playerGroup;

    // UI
    //internal bool sceneFinished = false;
    //internal bool sceneStarted = false;
    //internal bool shouldRate = true;

    //internal List<Task> _tasks;
    //internal List<Task> tasks {
    //    get { return _tasks; }
    //    set {
    //        _tasks = value;
    //        // new sheet
    //        //srd.AddNewSheet(sheetName, techOrders, _tasks);
    //    }
    //}

    //List<Tech> techOrders = new List<Tech>();
    //public Texture[] videoTextures = new Texture[6];
    //int _currentTaskIdx = 0;
    //internal int currentTaskIdx { get { return _currentTaskIdx; } }
    //public Task CurrentTask => tasks[currentTaskIdx];
    internal Tech[] techOrder = new[] { Tech.Ours, Tech.Opti, Tech.No };
    internal Tech CurrentTech => techOrder[0];

    string _sceneName;
    internal string sceneName {
        get { return _sceneName; }
        set {
            _sceneName = value;
            //sheetName = string.Format("{0}_{1}_{2}_{3}_{4}_userId{5}", DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, sceneName, userId);
        }
    }

    private void Awake()
    {
        obW = Academy.Instance.EnvironmentParameters.GetWithDefault("ob_w", 0f) == 1.0f;
        small = Academy.Instance.EnvironmentParameters.GetWithDefault("small", 1f) == 1f;
        labelY = small ? 1.9f : 2.8f;
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 1f) == 1.0f;
        evaluate_metrics = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_metrics", 1f) == 1.0f;
        courtCount = gameObject.scene.GetRootGameObjects().Count(go => go.activeSelf) - 2;
        //srd = new SheetReader();

        //for (int i = 0; i < 6; ++i)
        //{
        //    techOrders.AddRange(techOrder);
        //}
    }

    private void Start()
    {
        playerGroup.SetActive(true);
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

    //public void NextTask(bool round = false)
    //{
    //    this._currentTaskIdx += 1;
    //    // in the setting view
    //    if(round)  {
    //        this._currentTaskIdx = this._currentTaskIdx % 18;
    //        return;
    //    }
        
    //    if (this._currentTaskIdx >= tasks.Count) this._currentTaskIdx = -1;
    //}

}
