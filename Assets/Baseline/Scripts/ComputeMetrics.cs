using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ComputeMetrics : MonoBehaviour
{
    Transform m_Panel;
    RVOLine m_RVOLine;

    public Camera cam;
    public RVOplayer PlayerLabel;
    public List<HashSet<string>> occludedObjectOverTime = new List<HashSet<string>>();
    public List<HashSet<string>> intersectionsOverTime = new List<HashSet<string>>();

    public List<float> distToTargetOverTime = new List<float>();
    public List<Vector2> posOverTime = new List<Vector2>();

    public void Initialize()
    {
        m_RVOLine = GetComponent<RVOLine>();
        m_Panel = transform.Find("panel");
    }

    public void UpdateMetrics()
    {
        m_Panel.LookAt(cam.transform);
        numOfIntersection();
        CollectOccluding();
        CollectDistToTarget();
        CollectPos();
    }

    private void CollectOccluding()
    {
        BoxCollider collider = m_Panel.GetComponent<BoxCollider>();
        Vector3 size = collider.size * 0.5f;
        Vector3[] points = new Vector3[] {
            new Vector3(-size.x, size.y, 0),
            new Vector3(0, size.y, 0),
            new Vector3(size.x, size.y, 0),
            //
            new Vector3(-size.x, 0, 0),
            new Vector3(0, 0, 0),
            new Vector3(size.x, 0, 0),
            // 
            new Vector3(-size.x, -size.y, 0),
            new Vector3(0, -size.y, 0),
            new Vector3(size.x, -size.y, 0)
        };

        int labelLayerMask = 1 << LayerMask.NameToLayer("label");
        int playerLayerMask = 1 << LayerMask.NameToLayer("player");

        List<RaycastHit> hits = new List<RaycastHit>();
        foreach (var p in points)
        {
            Vector3 origin = m_Panel.TransformPoint(p);
            Vector3 direction = origin - cam.transform.position;
            Debug.DrawRay(origin, direction, new Color(1, 0, 0));
            // raycast, count hit
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, Mathf.Infinity, labelLayerMask | playerLayerMask))
            {
                if (!GameObject.ReferenceEquals(hit.collider.transform.parent.gameObject, gameObject))
                    hits.Add(hit);
            }
        }

        var ids = new HashSet<string>();


        hits.ForEach(hit => {

            string id;
            if (hit.collider.CompareTag("player"))
            {
                id = "p_" + hit.collider.GetComponentInParent<RVOplayer>().sid;
            }
            else
            {
                id = "l_" + hit.collider.GetComponentInParent<RVOLabelAgent>().PlayerLabel.sid;
            }
            ids.Add(id);
        });
        occludedObjectOverTime.Add(ids);
    }

    private int numOfIntersection()
    {
        var intersectedLines = transform.parent.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject) && l.isIntersected(m_RVOLine, cam));

        var intersections = new HashSet<string>();
        var selfSid = PlayerLabel.sid;
        foreach (var sid in intersectedLines.Select(i => i.GetComponent<RVOLabelAgent>().PlayerLabel.sid))
        {
            intersections.Add((selfSid > sid) ? (selfSid + "_" + sid) : (sid + "_" + selfSid));
        }
        intersectionsOverTime.Add(intersections);

        return intersectedLines.Count();
    }

    private void CollectDistToTarget()
    {
        distToTargetOverTime.Add(Vector2.Distance(
             new Vector2(transform.position.x, transform.position.z),
             new Vector2(PlayerLabel.transform.position.x, PlayerLabel.transform.position.z)
        ));
    }

    private void CollectPos()
    {
        posOverTime.Add(new Vector2(transform.position.x, transform.position.z));
    }
}
