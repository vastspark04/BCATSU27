using UnityEngine;

public class VehicleMover : GroundUnitMover
{
	public float rotationRate;

	public Transform rotationTransform;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.velocity.sqrMagnitude > 0f)
		{
			Vector3 vector = Vector3.ProjectOnPlane(base.velocity, base.surfaceNormal);
			if (vector != Vector3.zero)
			{
				Quaternion to = Quaternion.LookRotation(vector, base.surfaceNormal);
				rotationTransform.rotation = Quaternion.RotateTowards(rotationTransform.rotation, to, rotationRate * Time.fixedDeltaTime);
				float num = Vector3.Angle(Vector3.ProjectOnPlane(base.velocity, rotationTransform.up), rotationTransform.forward);
				num = Mathf.Clamp(num / 5f, 1f, 2f);
				SetVelocity(base.velocity / num);
			}
		}
	}

	protected override void OnPlaceOnTerrain(Vector3 point, Vector3 normal)
	{
		Vector3 forward = Vector3.Cross(normal, -base.transform.right);
		base.transform.rotation = Quaternion.LookRotation(forward, normal);
		base.transform.position = point + height * normal;
	}
}
