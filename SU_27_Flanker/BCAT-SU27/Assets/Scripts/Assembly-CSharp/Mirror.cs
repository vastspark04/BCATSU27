using UnityEngine;

public class Mirror : MonoBehaviour
{
	public Transform cameraTf;

	private Camera cam;

	private void Start()
	{
		cam = cameraTf.GetComponent<Camera>();
	}

	private void LateUpdate()
	{
		Vector3 vector = Vector3.Reflect(base.transform.position - VRHead.position, base.transform.forward);
		Vector3 vector2 = vector * 0.33f;
		cameraTf.position = base.transform.position - vector2;
		cam.nearClipPlane = vector2.magnitude + 0.1f;
		cameraTf.rotation = Quaternion.LookRotation(vector, base.transform.up);
	}
}
