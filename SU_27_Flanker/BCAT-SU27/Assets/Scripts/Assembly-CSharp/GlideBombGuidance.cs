using UnityEngine;

public class GlideBombGuidance : MissileGuidanceUnit
{
	public float glideAngle;

	public override Vector3 GetGuidedPoint()
	{
		Vector3 vector = Missile.BallisticLeadTargetPoint(base.missile.estTargetPos, base.missile.estTargetVel, base.missile.transform.position, base.missile.rb.velocity, base.missile.rb.velocity.magnitude, base.missile.leadTimeMultiplier, base.missile.maxBallisticOffset, base.missile.maxLeadTime);
		Vector3 vector2 = vector - base.transform.position;
		vector2.y = 0f;
		float num = VectorUtils.SignedAngle(vector2, vector - base.transform.position, Vector3.up);
		if (base.missile.rb.velocity.y > 0f && num > glideAngle * 2f)
		{
			Vector3 velocity = base.missile.rb.velocity;
			velocity.y = 0f;
			Vector3 vector3 = Quaternion.AngleAxis(VectorUtils.SignedAngle(velocity, vector2, Vector3.Cross(Vector3.up, velocity)), Vector3.up) * base.missile.rb.velocity;
			return base.transform.position + vector3;
		}
		if (VectorUtils.SignedAngle(vector2, base.missile.estTargetPos - base.transform.position, Vector3.up) < glideAngle && num > glideAngle)
		{
			Vector3 vector4 = Quaternion.AngleAxis(glideAngle, -Vector3.Cross(Vector3.up, vector2)) * vector2;
			return base.transform.position + vector4;
		}
		return vector;
	}
}
