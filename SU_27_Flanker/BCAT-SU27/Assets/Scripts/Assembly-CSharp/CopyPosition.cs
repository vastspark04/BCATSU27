using UnityEngine;

[ExecuteAlways]
public class CopyPosition : MonoBehaviour
{
	public Transform target;

	public bool lateUpdate;

	public bool local;

	public Transform referenceTransform;

	private Transform myTransform;

	public bool executeInEditMode;

	private void Start()
	{
		if (!referenceTransform)
		{
			referenceTransform = target.parent;
		}
		myTransform = base.transform;
	}

	private void UpdateTransform()
	{
		if ((bool)target)
		{
			if (local)
			{
				myTransform.localPosition = referenceTransform.InverseTransformPoint(target.position);
			}
			else
			{
				myTransform.position = target.position;
			}
		}
	}

	private void Update()
	{
		if (!lateUpdate)
		{
			UpdateTransform();
		}
	}

	private void LateUpdate()
	{
		if (lateUpdate)
		{
			UpdateTransform();
		}
	}
}
