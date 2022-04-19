using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelNode
{
	Vector totalForce;
	LabelNode player;
	public Collider collider, viewplaneCollider;
	public GameObject sphere, plane, label;

	private const float MAX_DISTANCE = 1000000f;
	private const float STEP = 2f;
	public float DUMPING = .001f;

    public void UpdateSphere(bool debug)
    {
        if (!debug)
        {
			sphere.transform.localScale = Vector3.zero;
			return;
        }

		sphere.transform.localScale = 0.02f * Vector3.one;

		sphere.transform.position = label.transform.position;
		Debug.DrawLine(sphere.transform.position,
			GetNextPosFinal());

		// sphere.transform.position = viewplaneCollider.transform.TransformPoint(
		// GetObjPosOnViewPlane());

		// Debug.DrawLine(sphere.transform.position,
		// GetNextPos(dumping));
		// UpdatePlane();
		// sphere.transform.position = GetNextPosFinal();
	}

	private void UpdatePlane()
	{
		plane.transform.position =
				player.collider.bounds.center +
				new Vector3(0f, 1f, 0f);
		plane.transform.up = player.collider.transform.up;
	}

	public Vector3 GetNextPosFinal()
    {
		UpdatePlane();
		Vector3 from = viewplaneCollider.transform.TransformPoint(
			GetObjPosOnViewPlane()),
			to = GetNextPos(),
			dir = (to-from).normalized,
			newPos = label.transform.position + (STEP * dir);

		return plane.GetComponent<Collider>().ClosestPoint(newPos);
		/*
		UpdatePlane();
		Vector3 nextPosInViewpoint = GetNextPos();
		return plane.GetComponent<Collider>().ClosestPoint(nextPosInViewpoint);
		*/
    }

	public LabelNode(Collider collider, Collider viewplaneCollider)
	{
		this.collider = collider;
		this.viewplaneCollider = viewplaneCollider;
	}

	public LabelNode(Collider collider, Collider viewplaneCollider, LabelNode player, GameObject sphere)
	{
		this.collider = collider;
		this.viewplaneCollider = viewplaneCollider;
		this.player = player;
		this.sphere = sphere;

		plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
		plane.transform.localScale = 0.2f * Vector3.one;
		plane.GetComponent<MeshRenderer>().enabled = false;
		plane.GetComponent<MeshCollider>().convex = true;
	}

	private Vector3 GetObjPosOnViewPlane()
    {
		Vector3 dir = Camera.main.transform.position -
			collider.bounds.center;
		Ray ray = new Ray(collider.bounds.center, dir);
		RaycastHit hit;

		if (viewplaneCollider.Raycast(ray, out hit, MAX_DISTANCE))
		{
			Vector3 planePoint = hit.transform.InverseTransformPoint(hit.point);
			return planePoint;
		}

		// Debug.LogError("No intersection with camera plane!");
		return -8f * Vector3.one;
    }

	public Vector3 GetNextPos(float dumping = -1f)
    {
		if (dumping == -1f)
			dumping = DUMPING;
		Vector3 currentPos3 = GetObjPosOnViewPlane();
		Vector2 currentPos2 = new Vector2(currentPos3.x, currentPos3.z);

		Vector currentPosition = new Vector(
			BaselineForce.CalcDistance(Vector2.zero, currentPos2),
			BaselineForce.GetBearingAngle(Vector2.zero, currentPos2)),
			nextPosition = (currentPosition + totalForce) * dumping;
		Vector2 nextPos2 = nextPosition.ToPoint();
		Vector3 nextPos3 = new Vector3(nextPos2.x, currentPos3.y, nextPos2.y);
		return viewplaneCollider.transform.TransformPoint(nextPos3);
	}

	public Vector2 Location
	{
		get
		{
			Vector3 planePos = GetObjPosOnViewPlane();
			return new Vector2(planePos.x, planePos.z);
		}
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

	public void MoveTowardsNewPos(float movementSpeed)
    {
		Vector3 targetPos = GetNextPosFinal(),
			oldPos = label.transform.position;
		float step = movementSpeed * Time.deltaTime;
		label.transform.position = Vector3.MoveTowards(oldPos, targetPos, step);
	}
}