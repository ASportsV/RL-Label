using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BaselineForce : MonoBehaviour
{
    private List<LabelNode> labelNodes = new List<LabelNode>();
    private Collider viewplaneCollider;

    public double ATTRACTION_CONSTANT = 10;     
    public double REPULSION_CONSTANT = 100;
    private const int DEFAULT_SPRING_LENGTH = 100;

    public float dumping = .001f;
    public bool debug = false;

    void Update()
    {
        UpdateForces();
        foreach (var l in labelNodes)
        {
            l.UpdateSphere(debug, dumping);
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

    public static int CalcDistance(Vector2 a, Vector2 b)
    {
        double xDist = (a.x - b.x);
        double yDist = (a.y - b.y);
        return (int)Math.Sqrt(Math.Pow(xDist, 2) + Math.Pow(yDist, 2));
    }

    private Vector CalcAttractionForce(LabelNode x, LabelNode y, double springLength)
    {
        int proximity = Math.Max(CalcDistance(x.Location, y.Location), 1);

        // Hooke's Law: F = -kx
        double force = ATTRACTION_CONSTANT * Math.Max(proximity - springLength, 0);
        double angle = GetBearingAngle(x.Location, y.Location);

        return new Vector(force, angle);
    }

    private Vector CalcRepulsionForce(LabelNode x, LabelNode y)
    {
        int proximity = Math.Max(CalcDistance(x.Location, y.Location), 1);

        // Coulomb's Law: F = k(Qq/r^2)
        double force = -(REPULSION_CONSTANT / Math.Pow(proximity, 2));
        double angle = GetBearingAngle(x.Location, y.Location);

        return new Vector(force, angle);
    }

    private void UpdateForces()
    {
        foreach (var label in labelNodes)
        {
            Vector netForce = new Vector(0d, 0d);

            foreach (var otherLabel in labelNodes)
            {
                if (label == otherLabel)
                {
                    netForce += CalcAttractionForce(
                        label, otherLabel.Player, DEFAULT_SPRING_LENGTH);
                } else
                {
                    netForce += CalcRepulsionForce(
                        label, otherLabel);
                    netForce += CalcRepulsionForce(
                        label, otherLabel.Player);
                }
            }

            label.Force = netForce;
        }
    }

    public void InitFrom(List<GameObject> lG, List<GameObject> l)
    {
        viewplaneCollider = GetViewplaneCollider();
        for (int i = 0; i < lG.Count; i++)
            AddLabel(lG[i], l[i]);
    }

    public void AddLabel(GameObject lG, GameObject l)
    {
        GameObject player = lG.transform.Find("player_parent").gameObject;
        LabelNode nodeP = new LabelNode
            (player.GetComponentInChildren<Collider>(), viewplaneCollider);

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<SphereCollider>());
        sphere.transform.localScale = Vector3.zero;
        sphere.name = l.name;
        LabelNode nodeL = new LabelNode
            (l.GetComponentInChildren<Collider>(), viewplaneCollider, nodeP, sphere);
        labelNodes.Add(nodeL);
    }

    private Collider GetViewplaneCollider()
    {
        if (viewplaneCollider != null)
        {
            return viewplaneCollider;
        }

        GameObject parallelToCam = GameObject.CreatePrimitive(PrimitiveType.Plane);
        parallelToCam.GetComponent<MeshRenderer>().enabled = false;
        parallelToCam.transform.position =
                Camera.main.transform.position + Camera.main.transform.forward;
        Vector3 dir = parallelToCam.GetComponent<Collider>().bounds.center -
            Camera.main.transform.position;
        parallelToCam.transform.up = dir;
        return parallelToCam.GetComponent<Collider>();
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
            Destroy(viewplaneCollider);

        labelNodes = new List<LabelNode>();
        viewplaneCollider = null;
    }
}
