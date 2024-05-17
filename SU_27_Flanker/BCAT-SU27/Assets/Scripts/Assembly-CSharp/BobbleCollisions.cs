using UnityEngine;

public class BobbleCollisions : MonoBehaviour
{
	public Rigidbody bobbleRb;

	public Transform bobbleTf;

	public Transform[] colliderTfs;

	public float bobbleRadius;

	public float colliderRadii;

	public float separationVelMult = 1f;

	public float separationForce = 1f;

	public float separationDamper = 1f;

	private Vector3[] lastPositions;

	private void Start()
	{
		lastPositions = new Vector3[colliderTfs.Length];
		for (int i = 0; i < lastPositions.Length; i++)
		{
			lastPositions[i] = colliderTfs[i].position;
		}
	}

	private void FixedUpdate()
	{
		for (int i = 0; i < colliderTfs.Length; i++)
		{
			Vector3 position = colliderTfs[i].position;
			Vector3 vector = lastPositions[i];
			Vector3 vector2 = (position - vector) / Mathf.Max(Time.fixedDeltaTime, 0.001f);
			lastPositions[i] = position;
			Vector3 vector3 = position - bobbleTf.position;
			float magnitude = vector3.magnitude;
			float num = bobbleRadius + colliderRadii;
			if (magnitude < num)
			{
				if (Vector3.Dot(vector2, -vector3) > 0f)
				{
					bobbleRb.velocity += separationVelMult * vector2;
				}
				Vector3 vector4 = -vector3.normalized * (num - magnitude);
				bobbleRb.AddForce(vector4 * separationForce);
				Vector3 vector5 = bobbleRb.velocity - vector2;
				if (Vector3.Dot(vector5, vector3) > 0f)
				{
					bobbleRb.AddForce(-vector5 * separationDamper);
				}
			}
		}
	}
}
