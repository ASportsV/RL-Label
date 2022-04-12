using Unity.MLAgents;
using UnityEngine;


[System.Serializable]
public class RVOSettings : MonoBehaviour
{
    public int MaxSteps;

    public int numOfPlayer;
    public float playerSpeedX = 1f;
    public float playerSppedZ = 1f;

    public int courtX = 14;
    public int courtZ = 7;

    internal float minZInCam;
    internal float maxZInCam;

    internal float labelY = 1.8f;

    internal bool evaluate = false;

    private void Awake()
    {
        evaluate = Academy.Instance.EnvironmentParameters.GetWithDefault("_test_mode", 0f) == 1.0f;
    }
}
