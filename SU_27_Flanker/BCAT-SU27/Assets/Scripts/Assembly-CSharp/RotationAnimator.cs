using UnityEngine;

public class RotationAnimator : MonoBehaviour
{
	public Vector3 axis;

	public Transform rotationTransform;

	private Vector3 localAxis;

	public float speed;

	private void Start()
	{
		if (!rotationTransform)
		{
			rotationTransform = base.transform;
		}
		if ((bool)rotationTransform.parent)
		{
			localAxis = rotationTransform.parent.InverseTransformDirection(rotationTransform.TransformDirection(axis));
		}
		else
		{
			localAxis = base.transform.TransformDirection(axis);
		}
	}

	private void Update()
	{
		Quaternion quaternion = Quaternion.AngleAxis(speed * Time.deltaTime, localAxis);
		rotationTransform.localRotation = quaternion * rotationTransform.localRotation;
	}
}
