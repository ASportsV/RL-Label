using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DrawBounds : MonoBehaviour
{
    public Color color_sphere = new Color(0.0f, 0.0f, 0.0f, 0.5f);
    public Color color_bounds = new Color(1.0f, 1.0f, 0.0f, 0.5f);
    public bool Hierarchical = true;
    public bool Disable = false;

    public void OnDrawGizmos()
    {
        Bounds b = gameObject.GetComponent<Collider>().bounds;
        //if (Hierarchical)
        //{
        //    Renderer[] r = gameObject.GetComponentsInChildren<Renderer>();
        //    b = r.ComputeBounds();
        //}
        //else
        //{
        //    Renderer r = gameObject.GetComponent<Renderer>();
        //    if (r != null)
        //    {
        //        b = r.bounds;
        //    }
        //}
        Gizmos.color = color_bounds;
        Gizmos.DrawCube(b.center, b.size);

        var texture = new Texture2D(Screen.width, Screen.height);
        Color[] pixels = Enumerable.Repeat(color_sphere, Screen.width * Screen.height).ToArray();
        texture.SetPixels(pixels);
        texture.Apply();

        GUI.DrawTexture(new Rect(10, 10, 60, 60), texture, ScaleMode.ScaleToFit, true, 10.0F);
        //Gizmos.DrawGUITexture(new Rect(10, 10, 20, 20), texture);

        //Gizmos.color = color_sphere;
        //Gizmos.DrawSphere(b.center, b.size.magnitude * 0.1f);

        //RectTransform rt = gameObject.GetComponentInChildren<RectTransform>();
        //if(rt)
        //{
        //    Vector2 min = rt.rect.min;
        //    Vector2 max = rt.rect.max;

        //    var A = transform.TransformPoint(min);
        //    var B = transform.TransformPoint(new Vector3(min.x, max.y));
        //    var C = transform.TransformPoint(max);
        //    var D = transform.TransformPoint(new Vector3(max.x, min.y));

        //    Vector3[] v = new Vector3[4];
        //    rt.GetWorldCorners(v);

        //    Debug.Log("World Corners");
        //    for (var i = 0; i < 4; i++)
        //    {
        //        Debug.Log("World Corner " + i + " : " + v[i]);
        //    }
        //}
    }
}