using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Line : MonoBehaviour
{

    public Transform start;
    public Transform end;

    LineRenderer lineRenderer;
    int lengthOfLineRenderer = 2;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        lineRenderer.SetPosition(0, start.position - new Vector3(0, 0.4f, 0)); // offset
        lineRenderer.SetPosition(1, end.position);
    }
}
