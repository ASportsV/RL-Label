using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Baseline : MonoBehaviour
{
    public GameObject labelPrefab;
    public List<GameObject> labelGroups, labels;
    public Material lower, upper, center, planeDirs;
    public Material[] middleMats;
    private List<GameObject[]> labelsCorners, labelsMiddles;
    private Vector3 sphereScale = new Vector3(.3f, .3f, .3f),
        planeScale = new Vector3(.8f, .8f, .8f);
    private bool spheresInit = false, toInit = false;
    public bool hideSpheres, hidePlanes, hidePlaneSpheres;
    private float yThreshold = 3f, step = 1.5f, bigStep = 2f,
        lineMax = 3f, lineThresh = 2f,
        positiveStep, negativeStep, movementSpeed = .25f;
    private int countMoves = 1;
    public List<(float, float)> planeXY;

    private struct PlanePlayer
    {
        public GameObject player, plane,
            sphereUp, sphereForward, sphereRight, sphereCt;
    }
    private List<PlanePlayer> planes;

    public enum UpdateAlgo
    {
        OneDim,
        ThreeDim,
        PlaneBased
    }
    public UpdateAlgo algo;

    private void Start()
    {
        if(toInit) {
            StartHelper();
        }
    }

    void Update()
    {
        if(!spheresInit)
        {
            return;
        }
        UpdateSpheres();
        LabelRealignmentHelper();

        foreach (var l in labels)
        {
            l.transform.LookAt(Camera.main.transform.position);
        }

        switch (algo)
        {
            case UpdateAlgo.OneDim:
                LabelsAlgorithmOneDim();
                break;
            case UpdateAlgo.ThreeDim:
                LabelsAlgorithmThreeDimPlane(false);
                break;
            case UpdateAlgo.PlaneBased:
                UpdatePlanes();
                UpdateLabsFromXY();
                LabelsAlgorithmThreeDimPlane(true);
                break;
        }
    }

    public void InitFrom(List<GameObject> lG, List<GameObject> l)
    {
        CleanEverything();

        labelGroups = lG;
        labels = l;

        StartHelper();

        InitCornerSpheres();
        InitMiddleSpheres();
        if (algo == UpdateAlgo.PlaneBased)
        {
            InitPlanes();
        }

        spheresInit = true;

        ResetPositions();
    }

    public void CleanEverything()
    {
        labelGroups = null;
        labels = null;
        labelsCorners = null;
        labelsMiddles = null;
        spheresInit = false;
        toInit = false;
        planeXY = null;
    }

    public void Init(List<GameObject> players)
    {
        int counter = 1;
        foreach (var player in players)
        {
            GameObject labelObj = Instantiate(labelPrefab);
            labelObj.transform.name = string.Format("label{0}", counter);
            labelObj.transform.parent = transform;
            labelObj.transform.localPosition = Vector3.zero;
            labelObj.transform.localRotation = Quaternion.identity;
            labelObj.transform.localScale = Vector3.one;
            labelObj.GetComponent<LabelFollowPlayer>().player = player;
            labelObj.GetComponentInChildren<UpdatePole>().player = player;
            labelObj.GetComponentInChildren<TextMeshPro>().text =
                string.Format("{0}_label", counter);

            foreach (Transform child in labelObj.transform)
            {
                string old_name = child.name;
                child.name = string.Format("{0}{1}", child.name, counter);
                if (child.CompareTag("bg"))
                {
                    labels.Add(child.gameObject);
                }
            }
            labelGroups.Add(labelObj);
            counter++;
        }

        StartHelper();
        ResetPositions();
    }

    private void StartHelper()
    {
        positiveStep = step;
        negativeStep = -1 * step;
        int counter = 0;
        foreach (var l in labelGroups)
        {
            l.GetComponentInChildren<LabelIdHandler>().lId = counter++;
            if (algo == UpdateAlgo.PlaneBased)
            {
                l.GetComponentInChildren<LabelFollowPlayer>().planeBased = true;
            }
            else
            {
                l.GetComponentInChildren<LabelFollowPlayer>().followX = (algo == UpdateAlgo.OneDim);
            }
        }
        spheresInit = false;
        labelsCorners = new List<GameObject[]>();
        labelsMiddles = new List<GameObject[]>();

        if (algo == UpdateAlgo.PlaneBased)
        {
            planeXY = new List<(float, float)>();
            for (int i = 0; i < labelGroups.Count; i++)
            {
                planeXY.Add((0f, 1f));
            }
        }
    }

    public void ResetPositions()
    {
        if(algo == UpdateAlgo.PlaneBased)
        {
            for (int i = 0; i < planeXY.Count; i++)
            {
                planeXY[i] = (0f, 1f);
            }
            UpdatePlanes();
        }
        else
        {
            foreach (var l in labelGroups)
            {
                l.GetComponentInChildren<LabelFollowPlayer>().ResetPosition();
            }
        }
        UpdateSpheres();
    }

    private void LabelsAlgorithmThreeDimPlane(bool isPlane)
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if(labelGroups[i].activeInHierarchy)
            {
                AdjustLabelThreeDimPlane(i, isPlane);
            }
        }
    }

    private void LabelsAlgorithmOneDim()
    {
        for (int i = 0; i < labels.Count; i++)
        {
            if(labelGroups[i].activeInHierarchy)
            {
                AdjustLabelOneDim(i);
            }
        }
    }

    private (int, int) CheckHitFromCorners(GameObject[] corners, string myName,
        bool draw, bool lowerCounts)
    {
        int counter = 0;
        foreach (var c in corners)
        {
            var checkHit = CheckHit(c.GetComponent<Renderer>(), c.transform, myName, draw);
            if ((lowerCounts || (counter > 1)) && checkHit.Item1)
            {
                return (counter, checkHit.Item2);
            }
            counter++;
        }
        return (-1, -1);
    }

    private bool[] CheckHitFromMiddles(GameObject[] middles, string myName, bool draw)
    {
        bool[] hits = new bool[4];
        int counter = 0;
        foreach (var m in middles)
        {
            hits[counter++] = CheckHit(m.GetComponent<Renderer>(),
                m.transform, myName, draw).Item1;
        }
        return hits;
    }

    private (bool, int) CheckHit(Renderer r, Transform t, string myName, bool draw = false)
    {
        if (r.isVisible)
        {
            RaycastHit hit;
            Vector3 direction = t.position - Camera.main.transform.position;
            if (Physics.Raycast(Camera.main.transform.position, direction, out hit))
            {
                if (hit.transform.gameObject.name != myName &&
                    !hit.transform.CompareTag("ground"))
                {
                    if (!hit.collider.CompareTag("label") &&
                        !hit.collider.CompareTag("player"))
                    {
                        // Debug.LogFormat("name: {0}, tag: {1}", hit.collider.name, hit.collider.tag);
                    }

                    int lId2 = hit.collider.CompareTag("label") ?
                        hit.collider.GetComponent<LabelIdHandler>().lId : -1;
                    if (draw)
                    {
                        // Debug.DrawRay(Camera.main.transform.position, direction, Color.yellow);
                        // if (lId2 != -1)
                            // Debug.LogFormat("{0} --- {1} collided with --> (label) name: {2}",
                                // myName, t.name, labels[lId2].name);
                        // else
                            // Debug.LogFormat("{0} --- {1} collided with --> (player) name: {2}",
                                // myName, t.name, hit.collider.transform.parent.parent.name);
                    }
                    return (true, lId2);
                }
            }
        }
        return (false, -1);
    }

    private (float, float) ComputeUpdatesFromHits(int cornerHit, bool[] middleHits)
    {
        float yUpdate = 0f, xUpdate = 0f;
        switch (cornerHit)
        {
            case 0:
                if (middleHits[0] == middleHits[3])
                {
                    yUpdate = negativeStep;
                    xUpdate = positiveStep;
                }
                else if (middleHits[0])
                {
                    yUpdate = positiveStep;
                    xUpdate = 0f;
                }
                else if (middleHits[3])
                {
                    yUpdate = 0f;
                    xUpdate = negativeStep;
                }
                break;
            case 1:
                if (middleHits[0] == middleHits[1])
                {
                    yUpdate = positiveStep;
                    xUpdate = positiveStep;
                }
                else if (middleHits[0])
                {
                    yUpdate = positiveStep;
                    xUpdate = 0f;
                }
                else if (middleHits[1])
                {
                    yUpdate = 0f;
                    xUpdate = positiveStep;
                }
                break;
            case 2:
                if (middleHits[1] == middleHits[2])
                {
                    yUpdate = negativeStep;
                    xUpdate = positiveStep;
                }
                else if (middleHits[1])
                {
                    yUpdate = 0f;
                    xUpdate = positiveStep;
                }
                else if (middleHits[2])
                {
                    yUpdate = negativeStep;
                    xUpdate = 0f;
                }
                break;
            case 3:
                if (middleHits[2] == middleHits[3])
                {
                    yUpdate = negativeStep;
                    xUpdate = negativeStep;
                }
                else if (middleHits[2])
                {
                    yUpdate = negativeStep;
                    xUpdate = 0f;
                }
                else if (middleHits[3])
                {
                    yUpdate = 0f;
                    xUpdate = negativeStep;
                }
                break;
            default:
                Debug.Log("Error. Hit found but no switch case entered.");
                break;
        }
        return (xUpdate, yUpdate);
    }

    private void AdjustLabelThreeDimPlane(int lId, bool isPlane)
    {
        float yUpdate = 0, xUpdate = 0;
        bool draw = lId == 9 ? true : false;
        var cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name,
            draw, CheckLowerCounts(lId, lineMax));
        int counter = 5;
        while(cornerHit.Item1 != -1 && counter > 0)
        {
            Vector3 oldPosition = labels[lId].transform.position;
            if (cornerHit.Item1 == 4 || labels[lId].transform.position.y < yThreshold)
            {
                yUpdate = bigStep;
                xUpdate = 0;
            }
            else if ((yUpdate == bigStep && cornerHit.Item1 != 4) || (yUpdate == 0 && xUpdate == 0))
            {
                bool[] middleHits = CheckHitFromMiddles(labelsMiddles[lId], labels[lId].name, draw);
                var updateTuple = ComputeUpdatesFromHits(cornerHit.Item1, middleHits);
                xUpdate = updateTuple.Item1;
                yUpdate = updateTuple.Item2;
            }
            yUpdate = oldPosition.y < 1f ? bigStep : yUpdate;
            float xUpdateNeg = xUpdate * (-1f),
                    yUpdateNeg = yUpdate * (-1f);
            int lId2 = cornerHit.Item2;
            if (isPlane)
            {
                var xyUpdate = planeXY[lId];
                planeXY[lId] = (xyUpdate.Item1 + xUpdate,
                    xyUpdate.Item2 + yUpdate);
                // Debug.LogFormat("1] moving {0}? - ({1},{2})", lId, planeXY[lId].Item1, planeXY[lId].Item2);

                for (int i = 0; i < countMoves; i++)
                {
                    MovementWithPlane(labels[lId], planes[lId], planeXY[lId].Item1, planeXY[lId].Item2);
                }

                if(lId2 != -1)
                {
                    var xyUpdate2 = planeXY[lId2];
                    planeXY[lId2] = (xyUpdate2.Item1 + xUpdateNeg,
                        xyUpdate2.Item2 + yUpdateNeg);
                    // Debug.LogFormat("2] moving {0}? - ({1},{2})", lId2, planeXY[lId2].Item1, planeXY[lId2].Item2);

                    for (int i = 0; i < countMoves; i++)
                    {
                        MovementWithPlane(labels[lId2], planes[lId2], planeXY[lId2].Item1, planeXY[lId2].Item2);
                    }
                    
                }
            } else
            {
                MovementWithUpdates(labels[lId], xUpdate, yUpdate);

                if(lId2 != -1)
                {
                    MovementWithUpdates(labels[lId2], xUpdateNeg, yUpdateNeg);
                }
            }
            UpdateSpheres();
            cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name,
                draw, CheckLowerCounts(lId, lineMax));
            counter--;
        }
    }

    private void MovementHelper(GameObject obj, Vector3 targetPos)
    {
        var lIdH = obj.GetComponentInChildren<LabelIdHandler>();
        if (lIdH.newSphere != null)
            Destroy(lIdH.newSphere);
        var newSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newSphere.transform.parent = obj.transform;
        newSphere.transform.name = string.Format("newPosFor{0}", obj.name);
        newSphere.transform.position = targetPos;
        newSphere.GetComponent<Renderer>().material = planeDirs;
        newSphere.transform.localScale = Vector3.zero;// new Vector3(.5f, .5f, .5f);
        Destroy(newSphere.GetComponent<Collider>());
        lIdH.newSphere = newSphere;

        Vector3 oldPos = obj.transform.position;
        float step = movementSpeed * Time.deltaTime;
        // Format("Calling movement helper {6} - oldPos: {0},{1},{2}; newPos: {3},{4},{5}",
            // oldPos.x, oldPos.y, oldPos.z, targetPos.x, targetPos.y, targetPos.z, obj.name);
        obj.transform.position = Vector3.MoveTowards(oldPos, targetPos, step);
    }

    private void MovementWithPlane(GameObject obj, PlanePlayer pp,
        float xUpdate, float yUpdate, bool debug = true)
    {
        if(yUpdate < 1f) {
            yUpdate = 1f;
        }
        Vector3 oldPos = pp.sphereCt.transform.position;
        Vector3 targetPos = oldPos +
            (xUpdate * pp.plane.transform.right) +
            (yUpdate * pp.plane.transform.forward);

        // if(debug && (obj.name == "3_label" || obj.name == "9_label"))
            // Debug.LogFormat("{0} is moving with updates ({1}, {2})",
                // obj.name, xUpdate, yUpdate);

        MovementHelper(obj, targetPos);
    }

    private void MovementWithUpdates(GameObject obj, float xUpdate, float yUpdate)
    {
        Vector3 oldPos = obj.transform.position;
        Vector3 targetPos = new Vector3(
                oldPos.x + xUpdate,
                oldPos.y + yUpdate,
                oldPos.z);
        MovementHelper(obj, targetPos);
    }

    private bool CheckLowerCounts(int lId, float threshold, bool debug = true)
    {
        float lL = labelGroups[lId].GetComponentInChildren<RVOLine>().GetLineLength();
        if(debug && (lL > lineMax))
        {
            // Format("{0} lower counts: {1} - length: {2}", lId, (lL < lineMax), lL);
        }
        return (lL < threshold);
    }

    private void AdjustLabelOneDim(int lId)
    {
        float yUpdate = 8;
        bool draw = lId == 0 ? true : false;
        var cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name,
            draw, CheckLowerCounts(lId, lineMax));
        int counter = 20;
        while (cornerHit.Item1 != -1 && counter > 0)
        {
            if (yUpdate == 8 || (yUpdate == bigStep && cornerHit.Item1 != 4))
            {
                yUpdate = cornerHit.Item1 < 2 ? positiveStep : negativeStep;
            }
            if (cornerHit.Item1 == 4 || labels[lId].transform.position.y < yThreshold)
            {
                yUpdate = bigStep;
            }
            Vector3 oldPosition = labels[lId].transform.position;
            yUpdate = oldPosition.y < 1f ? bigStep : yUpdate;
            float yUpdateNeg = yUpdate * (-1f);

            MovementWithUpdates(labels[lId], 0f, yUpdate);

            if(cornerHit.Item2 != -1)
            {
                MovementWithUpdates(labels[cornerHit.Item2], 0f, yUpdateNeg);
            }

            UpdateSpheres();
            cornerHit = CheckHitFromCorners(labelsCorners[lId], labels[lId].name,
                draw, CheckLowerCounts(lId, lineMax));
            counter--;
        }
    }

    private Bounds GetBounds(GameObject o)
    {
        return o.GetComponentInChildren<BoxCollider>().bounds;
    }

    private Vector3[] Corners(Bounds bounds)
    {
        Vector3 min = bounds.min, max = bounds.max;
        float z = (min.z + max.z) / 2;
        Vector3[] corners = new Vector3[] {
            new Vector3(min.x, min.y, z),
            new Vector3(max.x, min.y, z),
            new Vector3(min.x, max.y, z),
            new Vector3(max.x, max.y, z),
            bounds.center
        };
        return corners;
    }

    private Vector3[] Middles(Bounds bounds)
    {
        Vector3 min = bounds.min, max = bounds.max;
        float z = (min.z + max.z) / 2,
            mid_x = (min.x + max.x) / 2,
            mid_y = (min.y + max.y) / 2;
        Vector3[] middles = new Vector3[]
        {
            new Vector3(mid_x, min.y, z),
            new Vector3(min.x, mid_y, z),
            new Vector3(mid_x, max.y, z),
            new Vector3(max.x, mid_y, z)
        };
        return middles;
    }

    private GameObject CreateSphereDir(string name, Transform parent)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = hidePlaneSpheres ?
            Vector3.zero : sphereScale;
        sphere.GetComponent<Renderer>().material = planeDirs;
        sphere.transform.parent = parent;
        string sphereName = name;
        sphere.name = sphereName;

        return sphere;
    }

    private void CreatePlane(GameObject lG, GameObject l)
    {
        GameObject player = lG.transform.Find("player_parent/player").gameObject;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Destroy(plane.GetComponent<MeshCollider>());
        plane.transform.parent = l.transform.parent;
        plane.transform.localScale = hidePlanes ? Vector3.zero :
            planeScale;

        int i = planes.Count;
        plane.name = string.Format("plane_{0}", i);
        PlanePlayer pp = new PlanePlayer();
        pp.player = player;
        pp.plane = plane;
        pp.sphereCt = CreateSphereDir(
            string.Format("plane_center_sphere_{0}", i),
            pp.plane.transform);
        pp.sphereForward = CreateSphereDir(
            string.Format("plane_forward_sphere_{0}", i),
            pp.plane.transform);
        pp.sphereRight = CreateSphereDir(
            string.Format("plane_right_sphere_{0}", i),
            pp.plane.transform);
        pp.sphereUp = CreateSphereDir(
            string.Format("plane_up_sphere_{0}", i),
            pp.plane.transform);
        planes.Add(pp);
    }

    private void InitPlanes()
    {
        planes = new List<PlanePlayer>();
        for (int i = 0; i < labelGroups.Count; i++)
        {
            GameObject l = labels[i], lG = labelGroups[i];
            CreatePlane(lG, l);
        }
    }

    private void UpdatePlanes()
    {
        foreach (var pp in planes)
        {
            pp.plane.transform.position =
                pp.player.GetComponent<Renderer>().bounds.center;
            Vector3 dir = pp.plane.GetComponent<Renderer>().bounds.center -
                Camera.main.transform.position;
            pp.plane.transform.up = -dir;

            pp.sphereCt.transform.position =
                pp.plane.GetComponent<Renderer>().bounds.center;
            pp.sphereForward.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.forward;
            pp.sphereRight.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.right;
            pp.sphereUp.transform.position =
                pp.sphereCt.transform.position +
                pp.plane.transform.up;
        }
    }

    private void UpdateLabsFromXY()
    {
        for (int lId = 0; lId < labelGroups.Count; lId++)
        {
            if (labelGroups[lId].activeInHierarchy)
            {
                MovementWithPlane(labels[lId], planes[lId], planeXY[lId].Item1,
                    planeXY[lId].Item2, false);
            }
        }
    }

    private GameObject GetPlayerFromLId(int lId)
    {
        return labelGroups[lId].transform.Find("player_parent/player").gameObject;
    }

    private bool IsLineEmpty(int lId, bool draw = false)
    {
        BoxCollider labelBoxCollider = labels[lId].GetComponentInChildren<BoxCollider>();
        Vector3 labelPos = labelBoxCollider.bounds.center,
        playerPos = GetPlayerFromLId(lId).GetComponent<Renderer>().bounds.center;

        RaycastHit hit;
        Vector3 direction = playerPos - labelPos;
        if (Physics.Raycast(labelPos, direction, out hit))
        {
            if (draw)
            {
                // Debug.DrawRay(labelPos, direction, Color.yellow);
                // Debug.LogFormat("{0} collided with --> name: {1}, tag: {2}",
                // labelBoxCollider.gameObject.name, hit.collider.name, hit.collider.tag);
            }

            if (hit.collider.name != GetPlayerFromLId(lId).name)
            {
                return false;
            }
        }
        return true;
    }

    private void LabelRealignment(int lId)
    {
        if (!CheckLowerCounts(lId, lineThresh) && IsLineEmpty(lId)
                || (algo == UpdateAlgo.PlaneBased && (planeXY[lId].Item1 < 0f || planeXY[lId].Item2 < 1f)))
        {
            if (algo == UpdateAlgo.PlaneBased)
            {
                planeXY[lId] = (0f, 1f);
                MovementWithPlane(labels[lId], planes[lId], planeXY[lId].Item1, planeXY[lId].Item2, false);
            }
            else
                MovementHelper(labels[lId],
                GetPlayerFromLId(lId).GetComponent<Renderer>().bounds.center);
        }
    }

    private void LabelRealignmentHelper()
    {
        for (int i = 0; i < labelGroups.Count; i++)
        {
            if(labelGroups[i].activeInHierarchy)
            {
                LabelRealignment(i);
            }
        }
    }

    public void AddLabel(GameObject lG, GameObject l)
    {
        labelGroups.Add(lG);
        labels.Add(l);
        lG.GetComponentInChildren<LabelIdHandler>().lId = labelGroups.Count - 1;

        AddCornerSphere(l);
        AddMiddleSphere(l);
        if (algo == UpdateAlgo.PlaneBased)
        {
            CreatePlane(lG, l);
            planeXY.Add((0f, 1f));
        }
    }

    private void UpdateCornerSpheres()
    {
        int i = 0;
        foreach (var l in labels)
        {
            Vector3[] lCorners = Corners(GetBounds(l));
            int j = 0;
            GameObject[] cornerSpheres = labelsCorners[i++];
            foreach (var c in lCorners)
            {
                cornerSpheres[j++].transform.position = c;
            }
        }
    }

    private void InitCornerSpheres()
    {
        foreach (var l in labels)
        {
            AddCornerSphere(l);
        }

        spheresInit = true;
    }

    private void AddCornerSphere(GameObject l)
    {
        Vector3[] lCorners = Corners(GetBounds(l));
        GameObject[] cornerSpheres = new GameObject[5];

        int j = 0;
        foreach (var c in lCorners)
        {
            GameObject sphere;
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<SphereCollider>());
            sphere.transform.localScale = hideSpheres ?
                Vector3.zero : sphereScale;
            sphere.GetComponent<Renderer>().material =
                j == 4 ? center : (j < 2 ? lower : upper);
            cornerSpheres[j++] = sphere;
            sphere.transform.parent = l.transform.parent;
            string sphereName = j == 4 ?
                "center-sphere" : string.Format("corner-sphere-{0}", j);
            sphere.name = sphereName;
            sphere.transform.position = c;
        }

        labelsCorners.Add(cornerSpheres);
    }

    private void UpdateMiddleSpheres()
    {
        int i = 0;
        foreach (var l in labels)
        {
            Vector3[] lMiddles = Middles(GetBounds(l));
            int j = 0;
            GameObject[] middleSpheres = labelsMiddles[i++];
            foreach (var c in lMiddles)
            {
                middleSpheres[j++].transform.position = c;
            }
        }
    }

    private void InitMiddleSpheres()
    {
        foreach (var l in labels)
        {
            AddMiddleSphere(l);
        }
    }

    private void AddMiddleSphere(GameObject l)
    {
        Vector3[] lMiddles = Middles(GetBounds(l));
        int j = 0;
        GameObject[] middleSpheres = new GameObject[4];
        foreach (var c in lMiddles)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<SphereCollider>());
            sphere.transform.localScale = hideSpheres ?
                Vector3.zero : sphereScale;
            sphere.GetComponent<Renderer>().material =
                middleMats[j];
            middleSpheres[j++] = sphere;
            sphere.transform.parent = l.transform.parent;
            sphere.name = string.Format("middle-sphere-{0}", j);
            sphere.transform.position = c;
        }

        labelsMiddles.Add(middleSpheres);
    }

    private void UpdateSpheres()
    {
        UpdateCornerSpheres();
        UpdateMiddleSpheres();
    }
}
