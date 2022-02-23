using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(LineRenderer))]
public class RVOLine : MonoBehaviour
{
    public Transform overlay;
    public Transform end;
    // Start is called before the first frame update
    LineRenderer line;
    float yOffset = 0.0f;

    private void Start()
    {
        line = GetComponent<LineRenderer>();
        overlay = transform.parent.parent.parent.Find("Canvas");
        debugPoint = new GameObject();
        debugPoint.name = "debug_line_" + this.name;
        debugPoint.transform.SetParent(overlay, false);

        Image image = debugPoint.AddComponent<Image>();
        image.color = new Color(0.0F, 1.0F, 0.0F, 0.2f);


    }

    // Update is called once per frame
    void Update()
    {
        Vector3 start = new Vector3(transform.position.x, transform.position.y - yOffset, transform.position.z);
        line.SetPosition(0, start);
        line.SetPosition(1, end.position);
    }

    Vector3[] points()
    {
        Vector3 start = new Vector3(transform.position.x, transform.position.y - yOffset, transform.position.z);
        return new Vector3[] { start, end.position };
    }


    Vector2 debugIntersection;
    public bool isIntersected(RVOLine line, Camera cam)
    {
        Vector3[] self = points();
        Vector3[] other = line.points();
        Vector2 intersection;

        Vector2[] selfInCam = self.Select(v => cam.WorldToViewportPoint(v)).Select(v => new Vector2(v.x, v.y)).ToArray();
        Vector2[] otherInCam = other.Select(v => cam.WorldToViewportPoint(v)).Select(v => new Vector2(v.x, v.y)).ToArray();
        
        bool intersected = LineSegmentsIntersection(selfInCam[0], selfInCam[1], otherInCam[0], otherInCam[1], out intersection);
        if(intersected)
        {
            debugIntersection = intersection;
        }
        return intersected;
    }

    public static bool LineSegmentsIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

        if (d == 0.0f)
        {
            return false;
        }

        var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
        var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

        if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
        {
            return false;
        }

        intersection.x = p1.x + u * (p2.x - p1.x);
        intersection.y = p1.y + u * (p2.y - p1.y);

        return true;
    }

    GameObject debugPoint;
    private void OnDrawGizmos()
    {
        if (overlay == null) return;

        RectTransform canvasRT = overlay.GetComponent<RectTransform>();
        RectTransform bboxRT = debugPoint.GetComponent<RectTransform>();

        bboxRT.localPosition = new Vector3(
            debugIntersection.x * canvasRT.sizeDelta.x - canvasRT.sizeDelta.x * 0.5f,
            debugIntersection.y * canvasRT.sizeDelta.y - canvasRT.sizeDelta.y * 0.5f, 0f);
        bboxRT.sizeDelta = new Vector2(0.005f * canvasRT.sizeDelta.x, 0.01f * canvasRT.sizeDelta.y);

        //bboxRT.localPosition = new Vector3(
        //    debugIntersection.x,
        //    debugIntersection.y, 0f);
        //bboxRT.sizeDelta = new Vector2(0.01f * canvasRT.sizeDelta.x, 0.01f * canvasRT.sizeDelta.y);
    }
}
