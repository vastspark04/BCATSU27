using UnityEngine;

[CreateAssetMenu(menuName = "VTOL VR/Missiles/Missile DLZ Data")]
public class MissileDLZData : ScriptableObject
{
	public Vector4Data2D data;

	public float minManeuverTime;

	public float speedScale;

	public float altitudeScale;

	public float rangeScale;

	public float avgVelocityRangeScale;

	public float avgVelocityScale = 1372f;

	public float avgVelAltThresh0 = 2000f;

	public float avgVelAltThresh1 = 4000f;

	public float overallFactor = 0.75f;

	public float fleeFactor = 0.9f;

	public void GetDLZData(float initialSpeed, Vector3 myPos, Vector3 targetPos, Vector3 targetVel, float minTargetRTRSpeed, out float minRange, out float maxRange, out float rangeTr, out float timeToTarget)
	{
		float altitude = WaterPhysics.GetAltitude(myPos);
		float num = data.GetData(initialSpeed / speedScale, altitude / altitudeScale).x * rangeScale * overallFactor;
		Vector3 delta = targetPos - myPos;
		float magnitude = delta.magnitude;
		Vector3 normalized = delta.normalized;
		Vector3 vector = normalized * initialSpeed;
		float averageVel = GetAverageVel(initialSpeed, altitude, magnitude);
		timeToTarget = VectorUtils.CalculateLeadTime(delta, targetVel - vector, averageVel);
		float magnitude2 = (targetPos + targetVel * timeToTarget - myPos).magnitude;
		float num2 = magnitude - magnitude2;
		maxRange = num + num2;
		float magnitude3 = targetVel.magnitude;
		float num3 = VectorUtils.CalculateLeadTime(delta, normalized * magnitude3 - vector, averageVel);
		if (num3 > 0f)
		{
			float num4 = Mathf.Max(minTargetRTRSpeed, magnitude3) * num3;
			rangeTr = Mathf.Max(1f, (num - num4) * fleeFactor);
		}
		else
		{
			rangeTr = 1f;
		}
		minRange = GetAverageVel(initialSpeed, altitude, 1000f) * minManeuverTime;
	}

	private float GetAverageVel(float initialSpeed, float altitude, float range)
	{
		float num = speedScale / (float)data.rows.Length;
		Vector4 vector = data.GetData(Mathf.Max(0f, initialSpeed - num) / speedScale, range / avgVelocityRangeScale);
		if (altitude < avgVelAltThresh0)
		{
			return vector.y * avgVelocityScale;
		}
		if (altitude < avgVelAltThresh1)
		{
			return vector.z * avgVelocityScale;
		}
		return vector.w * avgVelocityScale;
	}
}
