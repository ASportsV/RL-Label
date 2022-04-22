using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BaselineForce : MonoBehaviour
{
    private List<LabelNode> labelNodes = new List<LabelNode>();
    private Collider viewplaneCollider;

    public double ATTRACTION_CONSTANT = 100;     
    public double REPULSION_CONSTANT = 1000;
    public float MOVEMENT_SPEED = .5f;
    private const int DEFAULT_SPRING_LENGTH = 100;
    public bool debug = false;
    private bool init = false;

    void Update()
    {
        if (!init)
        {
            return;
        }
        UpdateViewPlane();
        UpdateForces();
        foreach (var l in labelNodes)
        {
            l.UpdateSphere(debug);
            l.MoveTowardsNewPos(MOVEMENT_SPEED);
        }
    }

    public static double GetBearingAngle(Vector2 start, Vector2 end)
    {
        Vector2 half = new Vector2(start.x + ((end.x - start.x) / 2), start.y + ((end.y - start.y) / 2));

        double diffX = (double)(half.x - start.x);
        double diffY = (double)(half.y - start.y);

        if (diffX == 0) diffX = 0.001;
        if (diffY == 0) diffY = 0.001;

        double angle;
        if (Math.Abs(diffX) > Math.Abs(diffY))
        {
            angle = Math.Tanh(diffY / diffX) * (180.0 / Math.PI);
            if (((diffX < 0) && (diffY > 0)) || ((diffX < 0) && (diffY < 0))) angle += 180;
        }
        else
        {
            angle = Math.Tanh(diffX / diffY) * (180.0 / Math.PI);
            if (((diffY < 0) && (diffX > 0)) || ((diffY < 0) && (diffX < 0))) angle += 180;
            angle = (180 - (angle + 90));
        }

        return angle;
    }

    public static double CalcDistance(Vector2 a, Vector2 b)
    {
        double xDist = (a.x - b.x);
        double yDist = (a.y - b.y);
        return Math.Sqrt(Math.Pow(xDist, 2) + Math.Pow(yDist, 2));
    }

    private Vector CalcAttractionForce(LabelNode x, LabelNode y, double springLength)
    {
        double proximity = 0.00000001 + CalcDistance(x.Location, y.Location);

        // Hooke's Law: F = -kx
        double force = ATTRACTION_CONSTANT * Math.Max(proximity - springLength, 0);
        double angle = GetBearingAngle(x.Location, y.Location);

        return new Vector(force, angle);
    }

    private Vector CalcRepulsionForce(LabelNode x, LabelNode y)
    {
        double proximity = 0.00000001 + CalcDistance(x.Location, y.Location); // [1, +Inf 

        // Coulomb's Law: F = k(Qq/r^2)
        double force = -(REPULSION_CONSTANT / Math.Pow(5 * proximity, 2));
        double angle = GetBearingAngle(x.Location, y.Location);
        //Debug.Log("To " + x.sid  +" from " + y.sid + ": force " + force.ToString() + ", dist " + proximity);
        return new Vector(force, angle);
    }

    private void UpdateForces()
    {
        foreach (var label in labelNodes)
        {
            if (label.Location.x != -8f)
            {
                Vector netForce = new Vector(0f, 0f);

                foreach (var otherLabel in labelNodes)
                {
                    if (label == otherLabel)
                    {
                        netForce += CalcAttractionForce(
                            label, otherLabel.Player, DEFAULT_SPRING_LENGTH);
                    }
                    else
                    {
                        var f1 = CalcRepulsionForce(label, otherLabel);                     
                        netForce += f1;
                        var f2 = CalcRepulsionForce(label, otherLabel.Player);
                        netForce += f2;
                    }
                }

                label.totalForce = netForce;
            }
        }
    }

    public void InitFrom(List<GameObject> lG, List<GameObject> l, List<bool> isAgents)
    {
        viewplaneCollider = GetViewplaneCollider();
        for (int i = 0; i < lG.Count; i++)
            AddLabel(lG[i], l[i], isAgents[i]);
        init = true;
    }

    public void AddLabel(GameObject lG, GameObject l, bool isAgent)
    {
        GameObject player = lG.transform.Find("player").gameObject;
        RVOplayer m_player = lG.GetComponent<RVOplayer>();
        LabelNode nodeP = new LabelNode
            ("player_" + m_player.sid, player.GetComponentInChildren<Collider>(), viewplaneCollider);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = Vector3.zero;
        sphere.name = l.name;
        LabelNode nodeL = new LabelNode
            ("label_" + m_player.sid, l.GetComponentInChildren<Collider>(), viewplaneCollider, nodeP, sphere, isAgent);
        nodeL.label = l;
        labelNodes.Add(nodeL);
    }

    private Collider GetViewplaneCollider()
    {
        if (viewplaneCollider != null)
        {
            return viewplaneCollider;
        }

        GameObject parallelToCam = GameObject.CreatePrimitive(PrimitiveType.Plane);
        parallelToCam.GetComponent<MeshRenderer>().enabled = debug;
        parallelToCam.transform.position =
                Camera.main.transform.position + Camera.main.transform.forward;
        Vector3 dir = parallelToCam.GetComponent<Collider>().bounds.center -
            Camera.main.transform.position;
        parallelToCam.transform.up = dir;
        parallelToCam.name = "parallelToCam";
        return parallelToCam.GetComponent<Collider>();
    }

    private void UpdateViewPlane()
    {
        viewplaneCollider.gameObject.transform.position =
            Camera.main.transform.position + Camera.main.transform.forward;
        viewplaneCollider.gameObject.transform.up = Camera.main.transform.forward;
    }

    public void CleanUp()
    {
        foreach (var l in labelNodes)
        {
            if (l.sphere != null)
                Destroy(l.sphere);
            if (l.plane != null)
                Destroy(l.plane);
        }
        if (viewplaneCollider != null)
            Destroy(viewplaneCollider.transform.gameObject);

        labelNodes = new List<LabelNode>();
        viewplaneCollider = null;
        init = false;
    }
}