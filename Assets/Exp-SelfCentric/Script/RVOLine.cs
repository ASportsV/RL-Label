using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RVOLine : MonoBehaviour
{

    public Transform end;
    // Start is called before the first frame update
    LineRenderer line;
    int numOfPoint = 2;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 start = new Vector3(transform.position.x, transform.position.y - 0.4f, transform.position.z);
        line.SetPosition(0, start);
        line.SetPosition(1, end.position);
    }
}
