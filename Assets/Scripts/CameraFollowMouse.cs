using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowMouse : MonoBehaviour
{
    public float speedH = 2.0f, speedV = 2.0f;
    private float yaw = .0f, pitch = .0f;

    void Update()
    {
        yaw += speedH * Input.GetAxis("Mouse X");
        pitch -= speedV * Input.GetAxis("Mouse Y");

        transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);
    }
}
