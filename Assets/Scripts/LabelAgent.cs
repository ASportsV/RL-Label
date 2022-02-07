using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UI;
using TMPro;

public class LabelAgent : Agent
{
    public Transform overlay;
    public GameObject debugBBox;
    public GameObject buttonPrefab;
    private GameObject debugButton;
    bool debug = false;
    bool isCasting = false;


    public Transform sceneCamera;
    public RectTransform rTransform;
    public GameObject player;
    public Vector3 maxAcc;
    public Vector3 maxSpeed;

    Rigidbody rBody;
    BufferSensorComponent bsensor;

    // Rewards
    Vector3 controlSignal;
    float lastDist;
    float minY = 1.2f;

    public int initStep = 0;
    public int currentStep = 0;
    public int totalStep = 0;
    private float time = 0.0f;
    private float timeStep = 0.04f;

    public override void Initialize()
    {
        rBody = this.GetComponent<Rigidbody>();
        bsensor = this.GetComponent<BufferSensorComponent>();
        rTransform = this.GetComponentInChildren<RectTransform>();
    }


    // tmp, hardcode
    float maxDistToAgent = 1.2f;
    float minDistToAgent = 0f;
    float maxDistToPlayer;
    float minDistToPlayer = 1.41421f * 0.25f;
    float normalizeDistToAgent;
    float normalizeDistToPlayer;
    private void Start()
    {
        maxDistToPlayer = 1.2247f * 0.5f + maxDistToAgent * 0.5f;
        normalizeDistToAgent = maxDistToAgent - minDistToAgent;
        normalizeDistToPlayer = maxDistToPlayer - minDistToPlayer;

        if (debug)
        {
            debugBBox = new GameObject();
            debugBBox.name = "Bbox_" + this.name;
            debugBBox.transform.SetParent(overlay.transform);

            Image image = debugBBox.AddComponent<Image>();
            image.color = new Color(0.0F, 1.0F, 0.0F, 0.2f);

            debugButton = Instantiate(buttonPrefab) as GameObject;
            debugButton.name = "button_" + this.name;
            debugButton.transform.SetParent(overlay.transform);
            TextMeshProUGUI text = debugButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = this.name.Split('_')[1].Split(' ')[0];
            Button button = debugButton.GetComponent<Button>();
            button.onClick.AddListener(this.triggerCasting);
            RectTransform rtCanvas = overlay.GetComponent<RectTransform>();
            RectTransform rtButton = debugButton.GetComponent<RectTransform>();
            float index = debugButton.transform.GetSiblingIndex();
            rtButton.localPosition = new Vector3(rtButton.sizeDelta.x * (0.0f + index) * 0.5f - rtCanvas.sizeDelta.x * 0.5f, -rtButton.sizeDelta.y + rtCanvas.sizeDelta.y * 0.5f, 0f);
        }
    }

    internal void Awake()
    {
        Academy.Instance.AgentPreStep += UpdateReward;
    }

    public override void OnEpisodeBegin()
    {
        // reset the position of the label
        currentStep = initStep;
        // reset the player
        EnvObj playerScript = player.GetComponent<EnvObj>();
        playerScript.Reset();

        var playerPos = player.transform.localPosition;
        var playerVel = player.GetComponent<Rigidbody>().velocity;
        this.transform.localPosition = new Vector3(playerPos.x, minY + 1.5f * Random.value, playerPos.z);
        // rBody.velocity = new Vector3(playerVel.x * Random.value, 0, playerVel.z * Random.value);
        rBody.velocity = playerVel;
    }

    /*-----------------Overservation-----------------------*/
    void OBIn3DWorldSpace(VectorSensor sensor)
    {

    }

    void OBIn3DCamSpace(VectorSensor sensor)
    {
        Camera cam = sceneCamera.GetComponent<Camera>();
        Vector3 selfPosInCam = sceneCamera.transform.InverseTransformPoint(this.transform.position);
        Vector3 selfVelInCam = sceneCamera.transform.InverseTransformVector(rBody.velocity);
        Vector3 selfExtentInCam = this.GetExtentInWorld();
        Vector3 selfRotation = this.transform.forward;

        var t = player.transform;
        Vector3 goalPosInCam = sceneCamera.transform.InverseTransformPoint(t.position);
        Vector3 goalVelInCam = sceneCamera.transform.InverseTransformVector(player.GetComponent<Rigidbody>().velocity);
        var distPos = goalPosInCam - selfPosInCam;
        var distVel = goalVelInCam - selfVelInCam;
        // sensor.AddObservation(distVel);
        sensor.AddObservation(selfPosInCam.x / 28f); // normalize
        sensor.AddObservation(selfPosInCam.y / 11f);
        sensor.AddObservation(selfPosInCam.z / 10f);

        sensor.AddObservation(selfVelInCam);
        sensor.AddObservation(selfExtentInCam);
        sensor.AddObservation(selfRotation);
        sensor.AddObservation(distPos.y);

        GameObject[] others = this.transform.parent.GetComponentsInChildren<Transform>()
                .Where(x => (x.CompareTag("agent")) && !GameObject.ReferenceEquals(x.gameObject, gameObject))
                // distance filter
                //.Where(x => Vector3.Distance(x.transform.localPosition, gameObject.transform.localPosition) < 5.0f)
                // should filter based on viewport space
                .Select(x => x.gameObject)
                .ToArray();

        foreach(GameObject other in others)
        {
            Vector3 posInCam = sceneCamera.transform.InverseTransformPoint(other.transform.position);
            Vector3 velInCam = sceneCamera.transform.InverseTransformVector(other.GetComponent<Rigidbody>().velocity);
            Vector3 extentInCam = other.CompareTag("agent")
                ? other.GetComponent<LabelAgent>().GetExtentInWorld()
                : other.GetComponent<EnvObj>().GetExtentInWorld();
            var relativePos = posInCam - selfPosInCam;
            var relativeVel = velInCam - selfVelInCam;
            Vector3 rotation = other.transform.forward;

            List<float> obs = new List<float>();
            obs.Add(relativePos.x / 28f);
            obs.Add(relativePos.y / 11f);
            obs.Add(relativePos.z / 10f);
            foreach (var vec in new[] { relativeVel, extentInCam, rotation })
            {
                obs.Add(vec.x);
                obs.Add(vec.y);
                obs.Add(vec.z);
            }
            bsensor.AppendObservation(obs.ToArray());
        }
    }

    void OBIn2DViewPort(VectorSensor sensor)
    {
        Camera cam = sceneCamera.GetComponent<Camera>();
        Vector3 selfPosInCam = cam.WorldToViewportPoint(this.transform.position);
        Vector3 selfVelInCam = cam.WorldToViewportPoint(rBody.velocity);
        Vector3 selfExtentInCam = this.GetExtentInViewport();

        var t = player.transform;
        Vector3 goalPosInCam = cam.WorldToViewportPoint(t.position);
        Vector3 goalVelInCam = cam.WorldToViewportPoint(player.GetComponent<Rigidbody>().velocity);
        var distPos = goalPosInCam - selfPosInCam;
        var distVel = goalVelInCam - selfVelInCam;
        sensor.AddObservation(distPos);
        sensor.AddObservation(distVel);
        sensor.AddObservation(selfExtentInCam.x);
        sensor.AddObservation(selfExtentInCam.y);

        GameObject[] others = this.transform.parent.GetComponentsInChildren<Transform>()
                .Where(x => (x.CompareTag("agent") || x.CompareTag("player")) && !GameObject.ReferenceEquals(x.gameObject, gameObject) && !GameObject.ReferenceEquals(x.gameObject, player))
                .Select(x => x.gameObject)
                .ToArray();

        foreach (GameObject other in others)
        {
            Vector3 posInCam = cam.WorldToViewportPoint(other.transform.position);
            Vector3 velInCam = cam.WorldToViewportPoint(other.GetComponent<Rigidbody>().velocity);

            Vector3 extentInCam = other.CompareTag("agent")
                    ? other.GetComponent<LabelAgent>().GetExtentInViewport()
                    : other.GetComponent<EnvObj>().GetExtentInViewport();

            var relativePos = posInCam - selfPosInCam;
            var relativeVel = velInCam - selfVelInCam;
            //sensor.AddObservation(relativePos);
            //sensor.AddObservation(relativeVel);
            //sensor.AddObservation(extentInCam);

            List<float> obs = new List<float>();
            foreach (var vec in new[] { relativePos, relativeVel })
            {
                obs.Add(vec.x);
                obs.Add(vec.y);
                obs.Add(vec.z);
            }
            obs.Add(extentInCam.x);
            obs.Add(extentInCam.y);
            bsensor.AppendObservation(obs.ToArray());
        }
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        this.OBIn3DCamSpace(sensor);
    }

    /*-----------------------Action-----------------------*/
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        time += Time.fixedDeltaTime;
        if (time >= timeStep)
        {
            time -= timeStep;
            currentStep += 1;
        }
        if (currentStep >= totalStep)
        {
            EndEpisode(); return;
        }

        // Actions, size = 3
        // controlSignal = Vector3.zero;
        // controlSignal.x = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        // controlSignal.y = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);
        // controlSignal.z = Mathf.Clamp(actionBuffers.ContinuousActions[2], -1f, 1f);
        float y = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f) * 2f;

        int scaleSize = actionBuffers.DiscreteActions[0];
        float newScale = Mathf.Clamp(scaleSize == 1
                ? this.transform.localScale.x + 0.05f
                : scaleSize == 2
                ? this.transform.localScale.x - 0.05f
                : this.transform.localScale.x, 0.1f, 1f);


        // actions
        // Vector3 acc = Vector3.zero;
        // acc.x = maxAcc.x * controlSignal.x;
        // acc.y = maxAcc.y * controlSignal.y;
        // acc.z = maxAcc.z * controlSignal.z;
        // rBody.AddForce(acc, ForceMode.Acceleration);
        Vector3 playerVel = player.GetComponent<Rigidbody>().velocity;
        rBody.velocity = new Vector3(playerVel.x, y, playerVel.z);

        // should clamp velocity in y
        float nextY = transform.localPosition.y + rBody.velocity.y * Time.fixedDeltaTime;

        if (nextY <= minY)
        {
            rBody.velocity = new Vector3(rBody.velocity.x, 0f, rBody.velocity.z);
            transform.localPosition = new Vector3(transform.localPosition.x, minY, transform.localPosition.z);
        }

        this.transform.LookAt(sceneCamera);
        this.transform.localScale = new Vector3(newScale, newScale, newScale);
    }

    /*-----------------------Reward-----------------------*/
    float rewDist(float dist, float maxDist)
    {
        //return the value on a declining sigmoid shaped curve that decays from 1 to 0
        //This reward will approach 1 if it matches perfectly and approach zero as it deviates
        return Mathf.Pow(1 - Mathf.Pow(dist / maxDist, 1.4f), 2) / 10.0f;
    }

    float rewVel(Vector3 vel, Vector3 goalVel)
    {

        // reward acc
        //float diffV = Mathf.Abs(controlSignal.x) + Mathf.Abs(controlSignal.z); // 0 ~ 2

        // reward diff vel
        //float rewDVec = diffV / -20f; // [-0.1, 0] 
        //float dx = Mathf.Clamp(Mathf.Abs(goalVel.x - vel.x), 0f, maxSpeed.x);
        //float dz = Mathf.Clamp(Mathf.Abs(goalVel.z - vel.z), 0f, maxSpeed.z);

        //float d = (dx / maxSpeed.x) + (dz / maxSpeed.z); // [0, 2]
        //d /= 2f; // [0, 0.1]
        //return Mathf.Pow(1 - Mathf.Pow(d, 1.4f), 2) / 10.0f;

        // expect the label to be static
        float dx = Mathf.Abs(vel.x / maxSpeed.x);
        float dz = Mathf.Abs(vel.z / maxSpeed.z);
        float d = (dx + dz) / 2.0f;
        return -0.05f + Mathf.Pow(1 - Mathf.Pow(d, 1.4f), 2) / 20.0f; // [0, -0.05]
    }

    float rewScale(float scale)
    {
        return 0.1f / (1f + Mathf.Pow((scale / (1f - scale)), -2)); // [0, 0.1]
    }

    float yDistThres = 3.0f;
    void UpdateReward(int academyStepCount)
    {
        var t = player.transform;
        var playerBody = player.GetComponent<Rigidbody>();
        Vector3 goalPos = new Vector3(t.localPosition.x, minY, t.localPosition.z);
        float dist = Vector3.Distance(goalPos, this.transform.localPosition);

        if (academyStepCount == 0)
        {
            lastDist = dist;
            return;
        }

        Vector3 origin = sceneCamera.transform.position;
        Vector3 halfExtent = this.GetExtentInWorld() * 0.5f;
        Vector3 direction = -gameObject.transform.forward;
        Quaternion rotation = Quaternion.LookRotation(direction);
        float maxDistance = Mathf.Infinity;
        int layerMask = 1 << 15;
        IEnumerable<RaycastHit> m_Hit = Physics.BoxCastAll(origin, halfExtent, direction, rotation, maxDistance, layerMask)
                .Where(h => h.collider.CompareTag("agent") && !GameObject.ReferenceEquals(gameObject, h.collider.gameObject)); // && !GameObject.ReferenceEquals(player, h.collider.gameObject));

        bool tooClose = m_Hit.Any(hit =>
        {
            Bounds hitBounds = hit.collider.bounds;
            // get the hit point
            Vector3 hitPoint = hit.point;
            // get the center point of the hit plane
            Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
            // cal the distance from the intersect point to the center
            float occludeDist = Vector3.Distance(intersectionPoint, hit.collider.bounds.center);

            // normalize
            float normalizeDist = hit.collider.CompareTag("player")
                ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
                : (occludeDist - minDistToAgent) / normalizeDistToAgent;

            return normalizeDist < 0.3f;
        });

        if (dist >= yDistThres || tooClose)
        {
            SetReward(-1.0f);
            EndEpisode();
        }
        else
        {
            float rewDist = -0.1f + this.rewDist(dist, yDistThres);
            rewDist /= 50f;

            float rewScale = -0.1f + this.rewScale(this.transform.localScale.x);
            rewScale /= 50f;

            SetReward(0.01f + rewDist + rewScale);
        }

 
        // if (dist < distThres)
        // {
        //     // reward velocity
        //     float rewDVec = 0; // this.rewVel(rBody.velocity, playerBody.velocity);

        //     // reward dist
        //     float rewDist = 0; // this.rewDist(dist, distThres);

        //     // single direction penatly
        //     // dist = 0.25 --->[-0.1, 0.1]
        //     // dist = 0 ---> [0, 0.2]
        //     //if (dist > lastDist) rewDist += (lastDist - dist) / (distThres * 10f);
        //     //lastDist = dist;

        
        //     float rewOcclusion = 0f;
        //     foreach (RaycastHit hit in m_Hit)
        //     {
        //         Bounds hitBounds = hit.collider.bounds;
        //         // get the hit point
        //         Vector3 hitPoint = hit.point;
        //         // get the center point of the hit plane
        //         Vector3 intersectionPoint = origin + Vector3.Project(hitPoint - origin, direction);
        //         // cal the distance from the intersect point to the center
        //         float occludeDist = Vector3.Distance(intersectionPoint, hit.collider.bounds.center);

        //         // normalize
        //         float normalizeDist = hit.collider.CompareTag("player")
        //             ? (occludeDist - minDistToPlayer) / normalizeDistToPlayer
        //             : (occludeDist - minDistToAgent) / normalizeDistToAgent;
        //         normalizeDist -= 1.0f; // [-1, 0]
        //         // calculate rewards
        //         rewOcclusion += normalizeDist;
        //     }
        //     rewOcclusion /= 10f; // [-0.1, 0] * 18

        //     //AddReward(rewDist + rewOcclusion);
        //     AddReward(rewDist + rewDVec + rewOcclusion);
        // }
        // else
        // {
        //     SetReward(-1.0f);
        //     EndEpisode();
        // }
    }

    /*-----------------Debug-----------------------*/
    void triggerCasting()
    {
        isCasting = !isCasting;
        Image image = debugButton.GetComponent<Image>();
        if (image)
        {
            image.color = isCasting ? new Color(153f / 255f, 1f, 206f / 255f) : new Color(1f, 1f, 1f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug) return;
        RectTransform rt = gameObject.GetComponentInChildren<RectTransform>();
        Vector3[] v = new Vector3[4];
        rt.GetWorldCorners(v);

        Camera cam = sceneCamera.GetComponent<Camera>();
        IEnumerable<Vector3> corners = v.Select(x => cam.WorldToViewportPoint(x));

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


        if (isCasting)
        {
            Vector3 origin = sceneCamera.transform.position;
            Vector3 halfExtent = rt.rect.size * 0.5f;
            Vector3 direction = -gameObject.transform.forward;
            Quaternion rotation = Quaternion.LookRotation(direction);
            float maxDistance = 10000;
            DrawBoxCastBox(origin, halfExtent, direction, rotation, maxDistance, new Color(1.0f, 0f, 0f));
            int layerMask = 1 << 15;

            IEnumerable<RaycastHit> m_Hit = Physics.BoxCastAll(origin, halfExtent, direction, rotation, maxDistance, layerMask).Where(h => !GameObject.ReferenceEquals(gameObject, h.collider.gameObject));
            if (m_Hit.Count() > 0)
            {
                foreach (RaycastHit hit in m_Hit)
                {
                    Debug.Log("Hit : " + hit.collider.name + ", at point: " + hit.point.ToString());
                    Gizmos.color = new Color(0.0f, 0.0f, 0.0f, 0.5f);
                    Gizmos.DrawSphere(hit.point, 0.1f);
                }
            }

        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private float boundsSize(Bounds a)
    {
        float xLeft = a.min.x;
        float xRight = a.max.x;
        float yTop = a.max.y;
        float yBottom = a.min.y;

        return (xLeft - xRight) * (yBottom - yTop);
    }

    private float intersectArea(Bounds a, Bounds b)
    {

        float xLeft = Mathf.Max(a.min.x, b.min.x);
        float yTop = Mathf.Min(a.max.y, b.max.y);
        float xRight = Mathf.Min(a.max.x, b.max.x);
        float yBottom = Mathf.Max(a.min.y, b.min.y);

        if (xRight < xLeft || yBottom < yTop) return 0f;

        return (xRight - xLeft) * (yBottom - yTop);

        //    if x_right < x_left or y_bottom<y_top:
        //        return 0.0

        //    # The intersection of two axis-aligned bounding boxes is always an
        //# axis-aligned bounding box
        //        intersection_area = (x_right - x_left) * (y_bottom - y_top)

        //    # compute the area of both AABBs
        //        bb1_area = (bb1['x2'] - bb1['x1']) * (bb1['y2'] - bb1['y1'])
        //    bb2_area = (bb2['x2'] - bb2['x1']) * (bb2['y2'] - bb2['y1'])

        //    # compute the intersection over union by taking the intersection
        //# area and dividing it by the sum of prediction + ground-truth
        //# areas - the interesection area
        //        iou = intersection_area / float(bb1_area + bb2_area - intersection_area)
        //    assert iou >= 0.0
        //    assert iou <= 1.0
        //    return iou

    }

    public Bounds GetBoundsInViewport()
    {

        Camera cam = sceneCamera.GetComponent<Camera>();
        Vector3 extent = this.GetExtentInViewport();
        extent.z = 0;
        Vector3 selfPosInCam = cam.WorldToViewportPoint(this.transform.position);
        selfPosInCam.z = 0;

        return new Bounds(selfPosInCam, extent);
    }

    public Vector3 GetExtentInWorld()
    {
        float scale = this.transform.localScale.x;
        return new Vector3(rTransform.rect.size.x * scale, rTransform.rect.size.y * scale, 0.0001f);
    }

    public Vector3 GetExtentInViewport()
    {
        RectTransform rt = gameObject.GetComponentInChildren<RectTransform>();
        Vector3[] v = new Vector3[4];
        rt.GetWorldCorners(v);

        Camera cam = sceneCamera.GetComponent<Camera>();
        IEnumerable<Vector3> corners = v.Select(x => cam.WorldToViewportPoint(x));

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

    public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Vector3 direction, Quaternion orientation, float distance, Color color)
    {
        direction.Normalize();
        Box bottomBox = new Box(origin, halfExtents, orientation);
        Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

        Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
        Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
        Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
        Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
        Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
        Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
        Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
        Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

        DrawBox(bottomBox, color);
        DrawBox(topBox, color);
    }

    public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color)
    {
        DrawBox(new Box(origin, halfExtents, orientation), color);
    }
    public static void DrawBox(Box box, Color color)
    {
        Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color);
        Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color);
        Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color);
        Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color);

        Debug.DrawLine(box.backTopLeft, box.backTopRight, color);
        Debug.DrawLine(box.backTopRight, box.backBottomRight, color);
        Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color);
        Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color);

        Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color);
        Debug.DrawLine(box.frontTopRight, box.backTopRight, color);
        Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color);
        Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color);
    }
}



public struct Box
{
    public Vector3 localFrontTopLeft { get; private set; }
    public Vector3 localFrontTopRight { get; private set; }
    public Vector3 localFrontBottomLeft { get; private set; }
    public Vector3 localFrontBottomRight { get; private set; }
    public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
    public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
    public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
    public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

    public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
    public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
    public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
    public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
    public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
    public Vector3 backTopRight { get { return localBackTopRight + origin; } }
    public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
    public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

    public Vector3 origin { get; private set; }

    public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
    {
        Rotate(orientation);
    }
    public Box(Vector3 origin, Vector3 halfExtents)
    {
        this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
        this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
        this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
        this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

        this.origin = origin;
    }


    public void Rotate(Quaternion orientation)
    {
        localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
        localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
        localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
        localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
    }

    //This should work for all cast types
    static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
    {
        return origin + (direction.normalized * hitInfoDistance);
    }

    static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        Vector3 direction = point - pivot;
        return pivot + rotation * direction;
    }
}

