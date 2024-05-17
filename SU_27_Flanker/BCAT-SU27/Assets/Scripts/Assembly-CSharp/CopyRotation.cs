using UnityEngine;

[ExecuteAlways]
public class CopyRotation : MonoBehaviour
{
	public Transform target;

	public bool local;

	private void LateUpdate()
	{
		if ((bool)target)
		{
			if (local)
			{
				base.transform.localRotation = target.localRotation;
			}
			else
			{
				base.transform.rotation = target.rotation;
			}
		}
	}
}
