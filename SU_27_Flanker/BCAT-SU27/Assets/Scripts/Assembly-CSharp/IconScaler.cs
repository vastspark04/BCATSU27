using UnityEngine;

public class IconScaler : MonoBehaviour
{
	public float scale = 0.1f;

	public bool applyScale = true;

	public bool applyRotation = true;

	public bool reverseRotation;

	public Camera cam;

	public float maxDistance = -1f;

	private void LateUpdate()
	{
		if (applyRotation)
		{
			if (reverseRotation)
			{
				base.transform.rotation = Quaternion.LookRotation(cam.transform.position - base.transform.position);
			}
			else
			{
				base.transform.rotation = Quaternion.LookRotation(base.transform.position - cam.transform.position);
			}
		}
		if (applyScale)
		{
			float num = Vector3.Distance(base.transform.position, cam.transform.position);
			if (maxDistance > 0f)
			{
				num = Mathf.Clamp(num, 0f, maxDistance);
			}
			float num2 = num * (cam.fieldOfView / 180f);
			base.transform.localScale = Vector3.one * scale * num2;
		}
	}
}
