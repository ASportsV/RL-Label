using Unity.MLAgents;
using UnityEngine;
using System.Linq;


[System.Serializable]
public class RVOSettings : MonoBehaviour
{
    //public int MaxSteps;
    //public int numOfPlayer;
    public float playerSpeedX = 1f;
    public float playerSppedZ = 1f;

    public int courtX = 14;
    public int courtZ = 7;

    internal float minZInCam;
    internal float maxZInCam;

    internal bool obW = false;

    // label parameters
    internal float labelY = 2.8f;
    internal float xzDistThres;
    internal float maxLabelSpeed;
    internal float moveUnit;
    internal float moveSmooth;

    internal bool evaluate = false;
    internal bool evaluate_metrics = false;

    int finished = 0;
    int courtCount = 8;

    private void Awake()
    {
        obW = Academy.Instance.EnvironmentParameters.GetWithDefault("ob_w", 0f) == 1.0f;
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 0f) == 1.0f;
        evaluate_metrics = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_metrics", 0f) == 1.0f;
        courtCount = gameObject.scene.GetRootGameObjects().Count(go => go.activeSelf) - 2;
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
}
