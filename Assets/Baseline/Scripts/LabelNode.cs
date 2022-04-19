using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelNode
{
	Vector totalForce;
	LabelNode player;
	public Collider collider, viewplaneCollider;
	public GameObject sphere, plane;

	private const float MAX_DISTANCE = 1000000f;
	public float DUMPING = .001f;

    public void UpdateSphere(bool debug, float dumping)
    {
        if (!debug)
        {
			sphere.transform.localScale = Vector3.zero;
			return;
        }
		Vector3 currentPos3 = GetObjPosOnViewPlane();
		Vector2 currentPos2 = new Vector2(currentPos3.x, currentPos3.z);
		sphere.transform.localScale = 0.01f * Vector3.one;
		sphere.transform.position = viewplaneCollider.transform.TransformPoint(
			currentPos3);

		Vector currentPosition = new Vector(
			BaselineForce.CalcDistance(Vector2.zero, currentPos2),
			BaselineForce.GetBearingAngle(Vector2.zero, currentPos2)),
			nextPosition = (currentPosition + totalForce) * dumping;
		Vector2 nextPos2 = nextPosition.ToPoint();
		Vector3 nextPos3 = new Vector3(nextPos2.x, currentPos3.y, nextPos2.y);

		Debug.DrawLine(sphere.transform.position,
			viewplaneCollider.transform.TransformPoint(nextPos3));
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

		Debug.LogError("No intersection with camera plane!");
		return Vector3.zero;
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
}
