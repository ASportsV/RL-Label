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
        FixedTime
    }

    public int numOfPlayers;
    public PlayerMovingModeType playerMovingMode = PlayerMovingModeType.FixedSpeed;
    public float playerSpeed;
    public float playerMovingTime;
    public float waitAfterReached;

    public bool syncPlayerMoving = true;


    public float courtX;
    public float courtZ;

    public int numOfDestinations;
}
