using UnityEngine;

public class ReorientPosition : MonoBehaviour
{
	public Transform headTransform;

	public float height = 1f;

	public void Reorient()
	{
		Quaternion rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(base.transform.parent.InverseTransformVector(headTransform.forward), Vector3.up));
		base.transform.localRotation = Quaternion.Inverse(rotation) * base.transform.localRotation;
		Vector3 vector = base.transform.parent.InverseTransformPoint(headTransform.position);
		vector.y -= height;
		base.transform.localPosition -= vector;
	}
}
