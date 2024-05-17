using UnityEngine;

[ExecuteInEditMode]
public class LookRotationReference : MonoBehaviour
{
	public Transform target;

	public Transform upReference;

	private Transform myTransform;

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void LateUpdate()
	{
		UpdateLook();
	}

	public void SetTarget(Transform tgt)
	{
		target = tgt;
	}

	public void UpdateLook()
	{
		myTransform.LookAt(target, upReference.up);
	}
}
