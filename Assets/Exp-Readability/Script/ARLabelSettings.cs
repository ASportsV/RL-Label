using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARLabelSettings : MonoBehaviour
{
    // 0 fixed speed
    // 1 fixed time
    public enum PlayerMovingModeType
    {
        FixedSpeed,
        RandomSpeed,
    }

    public int numOfPlayers;
    public int numOfAgents;

    public PlayerMovingModeType playerMovingMode = PlayerMovingModeType.FixedSpeed;
    public int MaxSteps;
    public float playerSpeed => Mathf.Ceil(2 * Mathf.Sqrt(courtX * courtX + courtZ * courtZ) / ((float)(MaxSteps - 100) * Time.fixedDeltaTime));

    //public float playerMovingTime;
    //public float waitAfterReached;
    public bool syncPlayerMoving = true;


    public float courtX;
    public float courtZ;

    public int numOfDestinations;
}
