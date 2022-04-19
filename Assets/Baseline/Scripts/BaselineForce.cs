using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class BaselineForce : MonoBehaviour
{
    private List<LabelNode> labelNodes;

    private const double ATTRACTION_CONSTANT = 0.1;     
    private const double REPULSION_CONSTANT = 10000;
    private const int DEFAULT_SPRING_LENGTH = 100;

    private double GetBearingAngle(Vector2 start, Vector2 end)
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
}
