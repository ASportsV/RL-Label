using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyMove : MonoBehaviour
{
    public GameObject ball, plane, ballx, bally, ballz;
    private int counter = -1;

    // Start is called before the first frame update
    void Start()
    {
        // plane.transform.rotation = Camera.main.transform.rotation;
        // plane.transform.LookAt(Camera.main.transform.position, -Vector3.up);
        plane.transform.forward = Camera.main.transform.forward;
        /*
        plane.transform.eulerAngles = new Vector3(
            plane.transform.eulerAngles.x,
            plane.transform.eulerAngles.y + 180,
            plane.transform.eulerAngles.z);
        */
    }

    // Update is called once per frame
    void Update()
    {
        AdjustBasis();
    }

    private void LogVector(Vector3 v, string n)
    {
        Debug.LogFormat("{0}: ({1}, {2}, {3})", n, v.x, v.y, v.z);
    }

    private void AdjustBasis()
    {
        ball.transform.position = plane.GetComponent<Renderer>().bounds.center;
        ballx.transform.position = plane.transform.right;
        bally.transform.position = plane.transform.up;
        ballz.transform.position = plane.transform.forward;
    }
}
