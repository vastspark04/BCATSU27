using UnityEngine;

public class AIHelper : MonoBehaviour
{
	public enum SurfaceModes
	{
		Flat,
		Terrain
	}

	public AnimationCurve leadHelperCurve;

	public float leadHelpCurveRate;

	public float leadHelpMultiplier;

	public float groundHeight;

	public SurfaceModes surfaceMode;

	public static AIHelper instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	public float GetLeadHelp(float t)
	{
		t = Mathf.Repeat(t * leadHelpCurveRate, 1f);
		return leadHelperCurve.Evaluate(t) * leadHelpMultiplier;
	}

	public float GetSurfaceHeight(Vector3 unitPosition)
	{
		if (surfaceMode == SurfaceModes.Flat)
		{
			return WaterPhysics.instance.height + groundHeight;
		}
		return WaterPhysics.instance.height;
	}

	public static bool GetAirToAirFireSolution(Vector3 launcherVelocity, Vector3 launcherPosition, float simSpeed, Actor targetVessel, out Vector3 fireSolution, ModuleTurret turret = null)
	{
		Vector3 position = targetVessel.transform.position;
		float num = 0f;
		float num2 = Vector3.Distance(targetVessel.transform.position, launcherPosition);
		Vector3 vector = simSpeed * (position - launcherPosition).normalized;
		vector += launcherVelocity;
		num = num2 / (targetVessel.velocity - vector).magnitude;
		num = Mathf.Clamp(num, 0f, 8f);
		position += targetVessel.velocity * num;
		position = Missile.BallisticPoint(position, launcherPosition, vector.magnitude / 2f);
		if ((bool)turret)
		{
			for (float num3 = Vector3.Angle(position - launcherPosition, Vector3.ProjectOnPlane(position - launcherPosition, Vector3.up)); num3 < turret.maxPitch; num3 += 5f)
			{
				if (Physics.Linecast(position, launcherPosition, 1))
				{
					Vector3 vector2 = position - launcherPosition;
					vector2 = Quaternion.AngleAxis(-5f, Vector3.Cross(Vector3.up, vector2)) * vector2;
					position = launcherPosition + vector2;
					continue;
				}
				fireSolution = position;
				return true;
			}
		}
		else if (!Physics.Linecast(position, launcherPosition, 1))
		{
			fireSolution = position;
			return true;
		}
		fireSolution = position;
		return false;
	}
}
