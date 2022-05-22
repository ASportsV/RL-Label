using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
public class BaselineAgent : Agent
{

    public LabelNode labelNode;
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {

        if (labelNode == null) return;
        labelNode.UpdateSphere(false);
        Vector3 newPos = labelNode.MoveTowardsNewPos(10f);

        var x = newPos.x;
        var z = newPos.z;
        transform.position = new Vector3(x, transform.position.y, z);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {

    }
}
