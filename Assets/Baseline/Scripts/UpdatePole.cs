using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatePole : MonoBehaviour
{

    public GameObject label, player;

    void Update()
    {
        Vector3 start = player.GetComponent<Renderer>().bounds.center,
            end = label.GetComponent<Renderer>().bounds.center;
        AlignCylinder(gameObject, start, end);
    }

    public void AlignCylinder(GameObject cylinder, Vector3 start, Vector3 end)
    {
        Transform parentBackup = cylinder.transform.parent;
        cylinder.transform.parent = null;

        var offset = end - start;
        var scale = new Vector3(0.01F, offset.magnitude / 2.0f, 0.01F);
        var position = start + (offset / 2.0f);
        cylinder.transform.localPosition = position;
        cylinder.transform.localRotation = Quaternion.identity;
        cylinder.transform.up = offset;
        cylinder.transform.localScale = scale;
        cylinder.transform.parent = parentBackup;
    }
}
