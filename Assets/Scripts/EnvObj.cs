using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RendererArrayExtension
{
    public static Bounds ComputeBounds(this Renderer[] renderers)
    {
        Bounds bounds = new Bounds();
        for (int ir = 0; ir < renderers.Length; ir++)
        {
            Renderer renderer = renderers[ir];
            if (ir == 0)
                bounds = renderer.bounds;
            else
                bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
}

public class EnvObj : MonoBehaviour
{
    public Transform overlay;
    public GameObject debugBBox;
    public bool debug = false;

    public Transform sceneCamera;
    Renderer rend;

    public Vector3[] positions;
    public Vector3[] velocities;
    public bool isBall;
    private int initStep = 0;

    public int currentStep = 0;
    public int totalStep = 0;
    private float time = 0.0f;
    private float timeStep = 0.04f;

    private void Start()
    {
        rend = this.GetComponent<Renderer>();
        if(debug)
        {
            debugBBox = new GameObject();
            debugBBox.name = "Bbox_" + this.name;
            debugBBox.transform.SetParent(overlay.transform);
            Image image = debugBBox.AddComponent<Image>();
            image.color = new Color(1.0F, 0.0F, 0.0F, 0.2f);
        }
    }


    private void FixedUpdate()
    {
        //print("EnvObj fixedUpdate " + this.transform.localPosition);
        time += Time.fixedDeltaTime;
        
        if(time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }
        
        if (currentStep < totalStep)
        {
            this.UpdateVelocity(currentStep);
        }
        else
        {
            Reset();
        }


        // 2d bbox debug
        if (debug && sceneCamera != null)
        {

            Bounds bounds = gameObject.GetComponent<Collider>().bounds;
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            Vector3[] worldCorners = new[] {
                new Vector3( c.x + e.x, c.y + e.y, c.z + e.z ),
                new Vector3( c.x + e.x, c.y + e.y, c.z - e.z ),
                new Vector3( c.x + e.x, c.y - e.y, c.z + e.z ),
                new Vector3( c.x + e.x, c.y - e.y, c.z - e.z ),
                new Vector3( c.x - e.x, c.y + e.y, c.z + e.z ),
                new Vector3( c.x - e.x, c.y + e.y, c.z - e.z ),
                new Vector3( c.x - e.x, c.y - e.y, c.z + e.z ),
                new Vector3( c.x - e.x, c.y - e.y, c.z - e.z ),
            };
            Camera cam = sceneCamera.GetComponent<Camera>();
            IEnumerable<Vector3> corners = worldCorners.Select(corner => cam.WorldToViewportPoint(corner));

            float maxX = corners.Max(corner => corner.x);
            float minX = corners.Min(corner => corner.x);
            float maxY = corners.Max(corner => corner.y);
            float minY = corners.Min(corner => corner.y);
            float maxZ = corners.Max(corner => corner.z);
            float minZ = corners.Min(corner => corner.z);
            float cx = (minX + maxX) / 2.0f;
            float cy = (minY + maxY) / 2.0f;
            float w = (maxX - minX);
            float h = (maxY - minY);

            RectTransform canvasRT = overlay.GetComponent<RectTransform>();
            RectTransform bboxRT = debugBBox.GetComponent<RectTransform>();

            bboxRT.localPosition = new Vector3(cx * canvasRT.sizeDelta.x - canvasRT.sizeDelta.x * 0.5f, cy * canvasRT.sizeDelta.y - canvasRT.sizeDelta.y * 0.5f, 0f);
            bboxRT.sizeDelta = new Vector2(w * canvasRT.sizeDelta.x, h * canvasRT.sizeDelta.y);
        }
    }

    public void SetInitStep(int step)
    {
        this.initStep = step;
    }

    public void Reset()
    {
        currentStep = initStep;
        this.UpdatePosition(currentStep);
        this.UpdateVelocity(currentStep);
    }

    public void UpdatePosition(int currentPos)
    {
        this.transform.localPosition = positions[currentPos];
    }

    public void UpdateVelocity(int currentStep)
    {
        try
        {
            this.GetComponent<Rigidbody>().velocity = velocities[currentStep];

        } 
        catch(Exception e)
        {
            print("UpdateVelocity " + this.name);

        }
    }

    //public Bounds GetNextExtentInViewport(int step)
    //{
    //    Vector3 goalNextVel = velocities[step];
    //    Vector3 goalNextPos = this.transform.localPosition + goalNextVel * Time.fixedDeltaTime;

    //    Vector3 extent = this.GetExtentInViewport(goalNextPos);
    //    return new Bounds(goalNextPos, extent);
    //}

    public Bounds GetBoundsInViewport()
    {
        Camera cam = sceneCamera.GetComponent<Camera>();
        Vector3 extent = this.GetExtentInViewport();
        //extent.z = 0;
        Vector3 selfPosInCam = cam.WorldToViewportPoint(this.transform.position);
        //selfPosInCam.z = 0;

        return new Bounds(selfPosInCam, extent);
    }

    public Vector3 GetExtentInWorld()
    {
        return rend.bounds.size;
    }

    public Vector3 GetExtentInViewport(Vector3? position = null)
    {
        Renderer[] r = gameObject.GetComponentsInChildren<Renderer>();
        Bounds bounds = r.ComputeBounds();
        Vector3 c;
        if (position == null)
            c = bounds.center;
        else
            c = position.Value;
        //Vector3 c = (position.Value != null) ? position : bounds.center;
        Vector3 e = bounds.extents;

        Vector3[] worldCorners = new[] {
            new Vector3( c.x + e.x, c.y + e.y, c.z + e.z ),
            new Vector3( c.x + e.x, c.y + e.y, c.z - e.z ),
            new Vector3( c.x + e.x, c.y - e.y, c.z + e.z ),
            new Vector3( c.x + e.x, c.y - e.y, c.z - e.z ),
            new Vector3( c.x - e.x, c.y + e.y, c.z + e.z ),
            new Vector3( c.x - e.x, c.y + e.y, c.z - e.z ),
            new Vector3( c.x - e.x, c.y - e.y, c.z + e.z ),
            new Vector3( c.x - e.x, c.y - e.y, c.z - e.z ),
        };

        Camera cam = sceneCamera.GetComponent<Camera>();
        IEnumerable<Vector3> corners = worldCorners.Select(corner => cam.WorldToViewportPoint(corner));

        float maxX = corners.Max(corner => corner.x);
        float minX = corners.Min(corner => corner.x);
        float maxY = corners.Max(corner => corner.y);
        float minY = corners.Min(corner => corner.y);
        float maxZ = corners.Max(corner => corner.z);
        float minZ = corners.Min(corner => corner.z);

        float width = maxX - minX;
        float height = maxY - minY;
        float depth = maxZ - minZ;

        return new Vector3(width, height, depth);
    }
}
