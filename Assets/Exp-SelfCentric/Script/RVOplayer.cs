using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RVOplayer : MonoBehaviour
{

    [HideInInspector] public int sid { get; private set; }
    [HideInInspector] public string root;
    public int currentStep = 0;

    public Transform label;
    Rigidbody label_Rbody;
    public Transform m_Panel;
    RectTransform rTransform;
    RVOLine m_RVOLine;

    public Vector3[] positions;
    public Vector3[] velocities;

    public void Init(int sId)
    {
        this.sid = sId;
        label_Rbody = label.GetComponent<Rigidbody>();
        m_Panel = label.Find("panel");
        rTransform = label.GetComponentInChildren<RectTransform>();
        m_RVOLine = label.GetComponent<RVOLine>();

        var text = transform.Find("BackCanvas/Text")
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text = transform.GetSiblingIndex().ToString(); //sid.ToString();
        
        text = transform.Find("TopCanvas/Text")
            .GetComponent<TMPro.TextMeshProUGUI>();
        text.text = transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/FrontCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/LeftCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
        //text = playerObj.transform.Find(string.Format("{0}/RightCanvas/Text", root))
        //    .GetComponent<TMPro.TextMeshProUGUI>();
        //text.text = playerObj.transform.GetSiblingIndex().ToString();
    }

    public Vector3 velocity => velocities[currentStep];

    public float distX => label.transform.position.x - transform.position.x;
    public float distZ => label.transform.position.z - transform.position.z;

    public void clampLabelPos(float xzDistThres)
    {
        if (Mathf.Abs(distX) > xzDistThres)
        {
            label.transform.position = new Vector3(transform.position.x + (distX > 0 ? xzDistThres : -xzDistThres), label.transform.position.y, label.transform.position.z);
            label_Rbody.velocity = new Vector3(0f, 0f, label_Rbody.velocity.z);
        }

        if (Mathf.Abs(distZ) > xzDistThres)
        {
            label.transform.position = new Vector3(label.transform.position.x, label.transform.position.y, transform.position.z + (distZ > 0 ? xzDistThres : -xzDistThres));
            label_Rbody.velocity = new Vector3(label_Rbody.velocity.x, 0f, 0f);
        }
    }

    public bool occluding()
    {
        Vector3 origin = m_Panel.position;
        Vector3 extent = GetSizeInWorld() * 0.5f;
        Vector3 direction = m_Panel.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;

        // occluding players
        RaycastHit backHit;
        int playerLayerMask = 1 << LayerMask.NameToLayer("player") | 1 << LayerMask.NameToLayer("label");
        return Physics.BoxCast(origin, extent, -direction, out backHit, rotation, maxDistance, playerLayerMask);
    }

    public int numOfIntersection(Camera cam)
    {
        var intersectedLines = transform.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject) && l.isIntersected(m_RVOLine, cam));

        return intersectedLines.Count();
    }

    public void addForeToLabel(Vector3 force)
    {
        label_Rbody.AddForce(force, ForceMode.VelocityChange);
    }

    public void step(int idx)
    {
        if(idx < positions.Count())
        {
            currentStep = idx;
            transform.localPosition = positions[idx];
            transform.forward = velocities[idx].normalized;
            //m_RBody.velocity = velocities[idx];
            //player.GetComponent<Rigidbody>().velocity = velocities[idx];
        }
    }

    public Vector3 GetSizeInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }
}