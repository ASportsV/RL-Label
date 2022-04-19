using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LabelNode : MonoBehaviour
{
	Vector totalForce;
	LabelNode player;
	public Collider collider, viewplaneCollider;
	private GameObject sphere;
	public bool debug = false;

	private const float MAX_DISTANCE = 1000000f;

    void Update()
    {
        if(debug)
        {
			sphere.transform.localScale = Vector3.one;
			sphere.transform.position = GetObjPosOnViewPlane();
        }
    }

    public LabelNode(Collider collider, Collider viewplaneCollider)
	{
		this.collider = collider;
		this.viewplaneCollider = viewplaneCollider;
	}

	public LabelNode(Collider collider, Collider viewplaneCollider, LabelNode player)
	{
		this.collider = collider;
		this.viewplaneCollider = viewplaneCollider;
		this.player = player;

		sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		Destroy(sphere.GetComponent<SphereCollider>());
		sphere.transform.localScale = Vector3.zero;
		sphere.name = collider.name;
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

	// change this with label to viewpoint
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
