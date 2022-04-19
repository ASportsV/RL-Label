using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelNode
{
	Vector totalForce;
	LabelNode player;
	Collider collider, viewplaneCollider;
	private const float MAX_DISTANCE = 1000000f;

	private Vector2 GetObjPosOnViewPlane()
    {
		Vector3 dir = Camera.main.transform.position -
			collider.bounds.center;
		Ray ray = new Ray(collider.bounds.center, dir);
		RaycastHit hit;

		if (viewplaneCollider.Raycast(ray, out hit, MAX_DISTANCE))
		{
			Vector3 planePoint = hit.transform.InverseTransformPoint(hit.point);
			return new Vector2(planePoint.x, planePoint.z);
		}

		Debug.LogError("No intersection with camera plane!");
		return new Vector2();
    }

	// change this with label to viewpoint
	public Vector2 Location
	{
		get { return GetObjPosOnViewPlane(); }
	}

	public Vector Force
    {
		get { return totalForce; }
		set { totalForce = value; }
    }

	public LabelNode Player
    {
		get { return player; }
		set { player = value; }
    }
}
