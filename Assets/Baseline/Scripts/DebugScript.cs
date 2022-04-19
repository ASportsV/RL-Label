using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    Vector3 oldPos = Vector3.zero;
    Collider collider, viewplaneCollider;
    GameObject parallelToCam;
    float MAX_DISTANCE = 1000000f;

    void Start()
    {
        collider = GetComponent<Collider>();
        parallelToCam = GameObject.CreatePrimitive(PrimitiveType.Plane);
        parallelToCam.GetComponent<MeshRenderer>().enabled = false;
        parallelToCam.transform.position =
                Camera.main.transform.position + Camera.main.transform.forward;
        Vector3 dir = parallelToCam.GetComponent<Collider>().bounds.center -
            Camera.main.transform.position;
        parallelToCam.transform.up = dir;
        viewplaneCollider = parallelToCam.GetComponent<Collider>();
    }

    private Vector3 GetObjPosOnViewPlane()
    {
        Vector3 dir = Camera.main.transform.position -
            collider.bounds.center;
        Ray ray = new Ray(collider.bounds.center, dir);
        RaycastHit hit;
        Debug.DrawRay(collider.bounds.center, dir);

        if (viewplaneCollider.Raycast(ray, out hit, MAX_DISTANCE))
        {
            Vector3 planePoint = hit.transform.InverseTransformPoint(hit.point);
            return planePoint;
        }

        Debug.LogError("No intersection with camera plane!");
        return new Vector3();
    }

    void Update()
    {
        if (transform.position == oldPos)
        {
            return;
        }

        // Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        Vector3 screenPos = GetObjPosOnViewPlane();
        Debug.LogFormat("({0}, {1}, {2})", screenPos.x, screenPos.y, screenPos.z);
        oldPos = transform.position;
    }
}
