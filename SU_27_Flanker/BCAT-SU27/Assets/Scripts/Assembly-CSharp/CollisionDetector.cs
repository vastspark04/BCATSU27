using System.Collections;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
	public Transform[] rayOriginTransforms;

	private int roCount;

	public float time;

	private Rigidbody rb;

	private FlightInfo flightInfo;

	private bool collisionDetected;

	private void Start()
	{
		roCount = rayOriginTransforms.Length;
		flightInfo = GetComponentInParent<FlightInfo>();
		rb = flightInfo.rb;
	}

	private void OnEnable()
	{
		StartCoroutine(DetectRoutine());
	}

	private IEnumerator DetectRoutine()
	{
		collisionDetected = false;
		yield return null;
		while (base.enabled)
		{
			if (flightInfo.surfaceSpeed < 10f)
			{
				collisionDetected = false;
				yield return null;
				continue;
			}
			float t = time;
			bool newC = false;
			for (int i = 0; i < roCount; i++)
			{
				t -= Time.deltaTime;
				if (RayCheck(rayOriginTransforms[i]))
				{
					collisionDetected = true;
					newC = true;
					break;
				}
				yield return null;
			}
			if (!newC)
			{
				collisionDetected = false;
			}
			if (t > 0f)
			{
				yield return new WaitForSeconds(t);
			}
			else
			{
				yield return null;
			}
		}
	}

	private bool RayCheck(Transform origin)
	{
		Ray ray = new Ray(origin.position, rb.velocity);
		float maxDistance = rb.velocity.magnitude * time;
		if (Physics.Raycast(ray, out var hitInfo, maxDistance, 1))
		{
			Debug.DrawLine(ray.origin, hitInfo.point);
			return true;
		}
		return false;
	}

	public bool GetCollisionDetected()
	{
		return collisionDetected;
	}
}
