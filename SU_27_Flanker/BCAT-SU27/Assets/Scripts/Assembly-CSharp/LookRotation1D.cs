using UnityEngine;

[ExecuteInEditMode]
public class LookRotation1D : MonoBehaviour
{
	public Transform target;

	public Vector3 planeNormal = Vector3.up;

	public bool invert;

	private void LateUpdate()
	{
		if ((bool)target)
		{
			Vector3 worldPosition = Vector3.ProjectOnPlane(((!invert) ? 1 : (-1)) * (target.position - base.transform.position), base.transform.TransformDirection(planeNormal)) + base.transform.position;
			base.transform.LookAt(worldPosition, base.transform.up);
		}
	}
}
