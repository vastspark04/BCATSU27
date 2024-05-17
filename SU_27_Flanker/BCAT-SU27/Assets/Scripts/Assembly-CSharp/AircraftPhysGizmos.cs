using UnityEngine;

public class AircraftPhysGizmos : MonoBehaviour
{
	public float aeroVectorLength = 0.2f;

	public Vector3 CoL;

	private void OnDrawGizmos()
	{
		CenterOfMass componentInChildren = base.transform.parent.GetComponentInChildren<CenterOfMass>();
		Rigidbody componentInParent = GetComponentInParent<Rigidbody>();
		Gizmos.color = Color.yellow;
		Vector3 vector;
		if ((bool)componentInChildren && !Application.isPlaying)
		{
			vector = componentInChildren.transform.position;
		}
		else if ((bool)componentInParent)
		{
			vector = componentInParent.worldCenterOfMass;
		}
		else
		{
			Gizmos.color = Color.magenta;
			vector = base.transform.position;
		}
		Gizmos.DrawSphere(vector, 1f);
		Vector3 zero = Vector3.zero;
		float num = 0f;
		Wing[] componentsInChildren = base.transform.parent.GetComponentsInChildren<Wing>();
		foreach (Wing wing in componentsInChildren)
		{
			if (wing.enabled)
			{
				float num2 = wing.liftArea * wing.liftCoefficient * Mathf.Abs(Vector3.Dot(wing.transform.up, base.transform.up));
				num += num2;
				Vector3 vector2 = wing.transform.position;
				if (wing.useManualOffset)
				{
					vector2 = componentInParent.transform.TransformPoint(componentInParent.transform.InverseTransformPoint(vector) + wing.manualOffset);
				}
				zero += vector2 * num2;
				if (Application.isPlaying)
				{
					Gizmos.color = Color.cyan;
					Gizmos.DrawLine(vector2, vector2 + wing.liftVector * aeroVectorLength);
					Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
					Gizmos.DrawLine(vector2, vector2 + wing.dragVector * aeroVectorLength);
				}
			}
		}
		if (num > 0f)
		{
			zero /= num;
			CoL = base.transform.InverseTransformPoint(zero);
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere(zero, 1f);
		}
	}
}
