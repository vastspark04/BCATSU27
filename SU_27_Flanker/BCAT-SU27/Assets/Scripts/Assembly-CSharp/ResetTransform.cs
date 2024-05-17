using UnityEngine;

public class ResetTransform : MonoBehaviour
{
	public bool position = true;

	public bool rotation = true;

	public bool scale = true;

	private void Awake()
	{
		if (position)
		{
			base.transform.localPosition = Vector3.zero;
		}
		if (rotation)
		{
			base.transform.localRotation = Quaternion.identity;
		}
		if (scale)
		{
			base.transform.localScale = Vector3.one;
		}
	}
}
