using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Label : MonoBehaviour
{
    public RVOplayer PlayerLabel;
    public Rigidbody m_Rbody;
    RectTransform rTransform;
    RVOLine m_RVOLine;
    public Transform m_Panel;
    public Camera cam;

    public List<HashSet<string>> occludedObjectOverTime = new List<HashSet<string>>();
    public List<HashSet<string>> intersectionsOverTime = new List<HashSet<string>>();
    public List<float> distToTargetOverTime = new List<float>();
    public List<Vector2> posOverTime = new List<Vector2>();

    protected void Awake()
    {
        m_Rbody = GetComponent<Rigidbody>();
        m_RVOLine = GetComponent<RVOLine>();
        m_Panel = transform.Find("panel");
        rTransform = GetComponentInChildren<RectTransform>();
        PlayerLabel = transform.parent.GetComponent<RVOplayer>();
        cam = transform.parent.parent.parent.Find("Camera").GetComponent<Camera>();
    }

    protected void Start()
    {
    }

    public Vector3 velocity => PlayerLabel.velocity + m_Rbody.velocity;

    public void cleanMetrics()
    {
        occludedObjectOverTime.Clear();
        intersectionsOverTime.Clear();
        distToTargetOverTime.Clear();
        posOverTime.Clear();
    }

    private void FixedUpdate()
    {
        m_Panel.LookAt(cam.transform);
        //CollectOccluding();
        //CollectDistToTarget();
        //CollectPos();
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
                id = "l_" + hit.collider.GetComponentInParent<Label>().PlayerLabel.sid;
            }
            ids.Add(id);
        });
        occludedObjectOverTime.Add(ids);
    }

    public int numOfIntersection()
    {
        var intersectedLines = transform.parent.parent.GetComponentsInChildren<RVOLine>()
            .Where(l => !GameObject.ReferenceEquals(l.gameObject, gameObject) && l.isIntersected(m_RVOLine, cam));

        var intersections = new HashSet<string>();
        var selfSid = PlayerLabel.sid;
        foreach (var sid in intersectedLines.Select(i => i.GetComponent<Label>().PlayerLabel.sid))
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

    RaycastHit forHit;
    RaycastHit backHit;
    public int rewOcclusions()
    {
        Vector3 origin = m_Panel.position;
        Vector3 extent = GetSizeInWorld() * 0.5f;
        Vector3 direction = m_Panel.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;

        int count = 0;
        // occluded by labels
        int labelLayerMask = 1 << LayerMask.NameToLayer("label");

        if (Physics.BoxCast(origin, extent, direction, out forHit, rotation, maxDistance, labelLayerMask))
        {
            count += 1;
        }

        // occluding players
        int playerLayerMask = 1 << LayerMask.NameToLayer("player") | labelLayerMask;
        if (Physics.BoxCast(origin, extent, -direction, out backHit, rotation, maxDistance, playerLayerMask))
        {
            count += 1;
        }
        return count;
    }

    public Vector3 GetSizeInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }

    private void OnDrawGizmos()
    {
        if (m_Panel == null) return;

        Vector3 origin = m_Panel.position;
        Vector3 direction = m_Panel.forward;


        Gizmos.color = new Color(0f, 1f, 0.5f);
        Gizmos.DrawRay(origin, direction);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawRay(origin, -direction);

        if (!Object.Equals(forHit, default(RaycastHit)))
        {
            Vector3 intersectionPoint = origin + Vector3.Project(forHit.point - origin, direction);
            Gizmos.color = new Color(0f, 1f, 0.5f);
            Gizmos.DrawLine(origin, intersectionPoint);
            Gizmos.DrawWireSphere(intersectionPoint, 0.3f);
        }

        if (!Object.Equals(backHit, default(RaycastHit)))
        {
            Vector3 intersectionPoint = origin + Vector3.Project(backHit.point - origin, -direction);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawLine(origin, backHit.point);
            Gizmos.DrawWireSphere(intersectionPoint, 0.3f);
        }
    }
}
