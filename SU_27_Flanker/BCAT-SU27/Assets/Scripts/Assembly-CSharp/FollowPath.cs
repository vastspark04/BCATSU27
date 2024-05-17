using UnityEngine;
using UnityEngine.Events;

public class FollowPath : MonoBehaviour
{
	public Transform[] pointTransforms;

	public bool uniformlyPartition;

	public bool loop;

	private Curve3D curve;

	public Color gizmoColor = Color.white;

	public int scenarioPathID = -1;

	public Curve3D.PathModes pathMode;

	private const float formSpread = 10f;

	private bool finalLooped
	{
		get
		{
			if (loop)
			{
				return pointTransforms.Length > 1;
			}
			return false;
		}
	}

	public event UnityAction OnPathChanged;

	public void UpdatePathMode()
	{
		if (curve != null && curve.pathMode != pathMode)
		{
			SetupCurve();
		}
	}

	public void SetPathMode(Curve3D.PathModes mode)
	{
		pathMode = mode;
		UpdatePathMode();
	}

	private void Awake()
	{
		if (curve == null)
		{
			SetupCurve();
		}
	}

	private void OnValidate()
	{
		if (pointTransforms != null && pointTransforms.Length >= 2)
		{
			SetupCurve();
		}
		if (curve != null && curve.pathMode != pathMode)
		{
			SetupCurve();
		}
	}

	public void SetupCurve()
	{
		curve = null;
		if (pointTransforms != null)
		{
			int num = pointTransforms.Length;
			if (finalLooped)
			{
				num++;
			}
			Vector3[] array = new Vector3[num];
			bool flag = false;
			for (int i = 0; i < pointTransforms.Length; i++)
			{
				if ((bool)pointTransforms[i])
				{
					array[i] = base.transform.InverseTransformPoint(pointTransforms[i].position);
					continue;
				}
				flag = true;
				break;
			}
			if (!flag)
			{
				if (finalLooped)
				{
					array[num - 1] = base.transform.InverseTransformPoint(pointTransforms[0].position);
				}
				curve = new Curve3D(array, pathMode);
				if (uniformlyPartition)
				{
					curve.UniformlyParition(0.5f);
				}
			}
		}
		if (this.OnPathChanged != null)
		{
			this.OnPathChanged();
		}
	}

	public bool IsCurveReady()
	{
		if (curve != null)
		{
			return curve.isCurveReady;
		}
		return false;
	}

	public float GetClosestTimeWorld(Vector3 worldPos, int curveIterations = 4)
	{
		return curve.GetClosestTime(base.transform.InverseTransformPoint(worldPos), curveIterations, loop);
	}

	public Vector3 GetFollowPoint(Vector3 actorPosition, float leadDistance, int curveIterations = 4, int lastFormationIdx = -1)
	{
		if (curve == null)
		{
			SetupCurve();
		}
		float closestTime = curve.GetClosestTime(base.transform.InverseTransformPoint(actorPosition), curveIterations, loop);
		float num = leadDistance / curve.approximateLength;
		closestTime += num;
		if (loop)
		{
			closestTime = Mathf.Repeat(closestTime, 1f);
		}
		Vector3 vector = base.transform.TransformPoint(curve.GetPoint(closestTime));
		if (lastFormationIdx > -1)
		{
			int num2 = ((lastFormationIdx % 2 != 0) ? 1 : (-1));
			int num3 = lastFormationIdx / 2;
			Vector3 rhs = base.transform.TransformDirection(curve.GetTangent(closestTime));
			Vector3 vector2 = Vector3.Cross(Vector3.up, rhs);
			vector += num2 * vector2 * 10f * num3;
		}
		Debug.DrawLine(actorPosition, vector, Color.red);
		return vector;
	}

	public Vector3 GetFollowPoint(Vector3 actorPosition, float leadDistance, out float currentT, int curveIterations = 4)
	{
		if (curve == null)
		{
			SetupCurve();
		}
		float num = (currentT = curve.GetClosestTime(base.transform.InverseTransformPoint(actorPosition), curveIterations, loop));
		float num2 = leadDistance / curve.approximateLength;
		num += num2;
		if (loop)
		{
			num = Mathf.Repeat(num, 1f);
		}
		Vector3 vector = base.transform.TransformPoint(curve.GetPoint(num));
		Debug.DrawLine(actorPosition, vector, Color.red, 1f);
		return vector;
	}

	public float GetClosestTime(Vector3 position, int curveIterations = 4)
	{
		return curve.GetClosestTime(base.transform.InverseTransformPoint(position), curveIterations, loop);
	}

	public Vector3 GetPoint(float t)
	{
		Vector3 result = Vector3.zero;
		if (curve != null)
		{
			result = curve.GetPoint(t);
		}
		return result;
	}

	public Vector3 GetWorldPoint(float t)
	{
		return base.transform.TransformPoint(GetPoint(t));
	}

	public Vector3 GetTangent(float t)
	{
		Vector3 result = Vector3.forward;
		if (curve != null)
		{
			result = curve.GetTangent(t);
		}
		return result;
	}

	public Vector3 GetWorldTangent(float t)
	{
		return base.transform.TransformDirection(GetTangent(t));
	}

	public float GetApproximateLength()
	{
		return curve.approximateLength;
	}

	public void UniformlyPartition(float intervalDistance)
	{
		if (curve != null)
		{
			curve.UniformlyParition(intervalDistance);
		}
	}
}
