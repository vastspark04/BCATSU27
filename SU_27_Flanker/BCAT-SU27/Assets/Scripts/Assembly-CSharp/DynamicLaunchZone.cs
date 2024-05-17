using UnityEngine;

public class DynamicLaunchZone : MonoBehaviour
{
	public struct LaunchParams
	{
		public float minLaunchRange;

		public float maxLaunchRange;

		public float rangeTr;

		public LaunchParams(float min, float max)
		{
			minLaunchRange = min;
			maxLaunchRange = max;
			rangeTr = (min + max) / 2f;
		}

		public LaunchParams(float min, float max, float rangeTr)
		{
			this.rangeTr = rangeTr;
			minLaunchRange = min;
			maxLaunchRange = max;
		}
	}

	public bool displayInHUD = true;

	public float minLaunchRange;

	public float maxLaunchRange;

	public float offBoresightMinAdjustFactor;

	public MissileDLZData missileDlzData;

	public LaunchParams launchParams { get; private set; }

	public bool inRangeMax { get; private set; }

	public bool inRangeOptimal { get; private set; }

	public float targetRange { get; private set; }

	public bool targetAcquired { get; private set; }

	public float timeToTarget { get; private set; } = -1f;


	private void Awake()
	{
		targetAcquired = false;
	}

	public void UpdateLaunchParams(Vector3 launcherPosition, Vector3 launcherVelocity, Vector3 targetPosition, Vector3 targetVelocity, float minTargetRTRSpeed = 343f)
	{
		targetRange = (launcherPosition - targetPosition).magnitude;
		if ((bool)missileDlzData)
		{
			missileDlzData.GetDLZData(launcherVelocity.magnitude, launcherPosition, targetPosition, targetVelocity, minTargetRTRSpeed, out var minRange, out var maxRange, out var rangeTr, out var num);
			launchParams = new LaunchParams(minRange, maxRange, rangeTr);
			timeToTarget = num;
		}
		else
		{
			launchParams = GetDynamicLaunchParams(launcherVelocity, targetPosition, targetVelocity);
			timeToTarget = -1f;
		}
		inRangeMax = targetRange > launchParams.minLaunchRange && targetRange < launchParams.maxLaunchRange;
		inRangeOptimal = targetRange > launchParams.minLaunchRange && targetRange < launchParams.rangeTr;
		targetAcquired = true;
	}

	public void SetNoTarget()
	{
		targetAcquired = false;
		inRangeMax = false;
		inRangeOptimal = false;
	}

	public LaunchParams GetDynamicLaunchParams(Vector3 launcherVelocity, Vector3 targetPosition, Vector3 targetVelocity)
	{
		float magnitude = launcherVelocity.magnitude;
		float num = 0f;
		float num2 = 0f;
		Vector3 lhs = targetVelocity - launcherVelocity;
		Vector3 planeNormal = targetPosition - base.transform.position;
		float num3 = 0f - Vector3.Dot(lhs, planeNormal.normalized);
		num += num3 * 4f;
		num2 += num3 * 12f;
		num += magnitude * 4f;
		num2 += magnitude * 4f;
		float num4 = WaterPhysics.GetAltitude(base.transform.position) - WaterPhysics.GetAltitude(targetPosition);
		num2 += num4;
		if (offBoresightMinAdjustFactor > 0f)
		{
			num += Vector3.Dot(launcherVelocity, Vector3.ProjectOnPlane(launcherVelocity, planeNormal).normalized) * offBoresightMinAdjustFactor;
		}
		float num5 = Mathf.Max(minLaunchRange + num, 0f);
		float max = Mathf.Max(maxLaunchRange + num2, num5 + 100f);
		return new LaunchParams(num5, max);
	}
}
