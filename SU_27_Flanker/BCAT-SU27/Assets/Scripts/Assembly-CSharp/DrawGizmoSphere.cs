using UnityEngine;

public class DrawGizmoSphere : MonoBehaviour
{
	public bool onlyOnSelected;

	public bool wire;

	public Color color;

	public float radius;

	public bool localTransform;

	private void OnDrawGizmos()
	{
		if (!onlyOnSelected)
		{
			DrawSphere();
		}
	}

	private void OnDrawGizmosSelected()
	{
		if (onlyOnSelected)
		{
			DrawSphere();
		}
	}

	private void DrawSphere()
	{
		Gizmos.color = color;
		if (localTransform)
		{
			Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
			if (wire)
			{
				Gizmos.DrawWireSphere(Vector3.zero, radius);
			}
			else
			{
				Gizmos.DrawSphere(Vector3.zero, radius);
			}
			Gizmos.matrix = Matrix4x4.identity;
		}
		else if (wire)
		{
			Gizmos.DrawWireSphere(base.transform.position, radius);
		}
		else
		{
			Gizmos.DrawSphere(base.transform.position, radius);
		}
	}
}
