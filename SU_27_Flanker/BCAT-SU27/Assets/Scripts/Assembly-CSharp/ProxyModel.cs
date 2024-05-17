using UnityEngine;

public class ProxyModel : MonoBehaviour
{
	public bool detachOnAwake = true;

	private Transform parent;

	private bool detached;

	private Vector3 localPos;

	private Vector3 localFwd;

	private Vector3 localUp;

	private Transform myTransform;

	private void Awake()
	{
		myTransform = base.transform;
		GetParent();
		if (detachOnAwake)
		{
			Detach();
		}
	}

	private void OnEnable()
	{
		if (detached)
		{
			FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		}
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		myTransform.position += offset;
	}

	private void OnDisable()
	{
		FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
	}

	private void Update()
	{
		if (detached)
		{
			if (parent == null)
			{
				FloatingOrigin.instance.OnOriginShift -= FloatingOrigin_instance_OnOriginShift;
				Object.Destroy(base.gameObject);
				return;
			}
			myTransform.position = parent.TransformPoint(localPos);
			Vector3 forward = parent.TransformDirection(localFwd);
			Vector3 upwards = parent.TransformDirection(localUp);
			myTransform.rotation = Quaternion.LookRotation(forward, upwards);
		}
	}

	[ContextMenu("Detach")]
	public void Detach()
	{
		if (!detached && Application.isPlaying)
		{
			GetParent();
			detached = true;
			base.transform.parent = null;
			FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
		}
	}

	private void GetParent()
	{
		if (!parent)
		{
			localPos = base.transform.localPosition;
			localFwd = base.transform.localRotation * Vector3.forward;
			localUp = base.transform.localRotation * Vector3.up;
			parent = base.transform.parent;
		}
	}

	public void Reattach()
	{
		if (detached)
		{
			myTransform.parent = parent;
			myTransform.localPosition = localPos;
			myTransform.localRotation = Quaternion.LookRotation(localFwd, localUp);
			detached = false;
		}
	}
}
