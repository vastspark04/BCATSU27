using UnityEngine;

[ExecuteInEditMode]
public class RotationInterpolation : MonoBehaviour
{
	public Transform aTransform;

	public Transform bTransform;

	public float t;

	public bool spherical;

	private void LateUpdate()
	{
		if ((bool)aTransform && (bool)bTransform)
		{
			if (spherical)
			{
				base.transform.rotation = Quaternion.Slerp(aTransform.rotation, bTransform.rotation, t);
			}
			else
			{
				base.transform.rotation = Quaternion.Lerp(aTransform.rotation, bTransform.rotation, t);
			}
		}
	}
}
