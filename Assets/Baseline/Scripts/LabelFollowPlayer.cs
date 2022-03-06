using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelFollowPlayer : MonoBehaviour
{
    public GameObject player;
    public bool followX = true, planeBased = false;
    private Vector3 lastPosition = new Vector3();
    private float resetY = 2f;

    void Start()
    {
        if (planeBased)
        {
            return;
        }

        ResetPosition();
        if (!followX)
        {
            lastPosition = player.transform.position;
        }
    }

    void Update()
    {
        if (planeBased)
        {
            return;
        }

        Vector3 playerPos = player.transform.position;
        if (followX)
        {
            transform.position = new Vector3(playerPos.x,
                transform.position.y, playerPos.z);
        } else
        {
            transform.position += (player.transform.position - lastPosition);
            lastPosition = player.transform.position;
        }
    }

    public void ResetPosition()
    {
        lastPosition = player.transform.position;
        Vector3 playerPos = player.transform.position;
        transform.position = new Vector3(playerPos.x,
            playerPos.y + resetY, playerPos.z);
    }

    public void ChangeHeight(float offset)
    {
        Vector3 playerPos = player.transform.position;
        transform.position = new Vector3(playerPos.x,
            transform.position.y + offset, playerPos.z);
    }
}
