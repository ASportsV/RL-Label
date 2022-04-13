using Unity.MLAgents;
using UnityEngine;


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

    // label parameters
    internal float labelY = 1.8f;
    internal float xzDistThres;
    internal float maxLabelSpeed;
    internal float moveUnit;
    internal float moveSmooth;

    internal bool evaluate = false;


    int finished = 0;
    int courtCount = 0;

    private void Awake()
    {
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 0f) == 1.0f;
        courtCount = gameObject.scene.GetRootGameObjects().Length - 2;
    }

    public void FinishACourt()
    {
        finished += 1;
        if(finished >= courtCount)
        {
            finished = 0;
            Academy.Instance.StatsRecorder.Add("_test_end", 1.0f);
        }
    }
}
