using System.Collections;
using UnityEngine;

public class CameraFPSMover : MonoBehaviour
{
	public float lookSensitivity;

	public float moveSpeed;

	public float shiftMultiplier;

	public float lookSmoothing = 10f;

	private Quaternion rotation;

	private IEnumerator Startup()
	{
		base.transform.parent = null;
		FloatingOriginShifter fls = GetComponent<FloatingOriginShifter>();
		if ((bool)fls)
		{
			fls.enabled = false;
		}
		rotation = base.transform.rotation;
		while (!VRHead.instance)
		{
			yield return null;
		}
		if ((bool)LevelBuilder.fetch)
		{
			while (!LevelBuilder.fetch.playerReady)
			{
				yield return null;
			}
			base.transform.position = LevelBuilder.fetch.playerTransform.position;
		}
		if (!GetComponent<VRHead>())
		{
			VRHead.instance.DisableCameras();
			base.gameObject.AddComponent<VRHead>();
		}
		if ((bool)fls)
		{
			fls.enabled = true;
		}
		if ((bool)LevelBuilder.fetch)
		{
			LevelBuilder.fetch.playerTransform = base.transform;
		}
		yield return null;
		if ((bool)AudioController.instance)
		{
			AudioController.instance.SetExteriorOpening("camFps", 1f);
		}
	}

	private void OnEnable()
	{
		StartCoroutine(Startup());
	}

	private void Update()
	{
		Vector3 zero = Vector3.zero;
		if (Input.GetKey(KeyCode.W))
		{
			zero += base.transform.forward;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zero += -base.transform.forward;
		}
		if (Input.GetKey(KeyCode.A))
		{
			zero += Vector3.ProjectOnPlane(-base.transform.right, Vector3.up);
		}
		if (Input.GetKey(KeyCode.D))
		{
			zero += Vector3.ProjectOnPlane(base.transform.right, Vector3.up);
		}
		if (Input.GetKey(KeyCode.E))
		{
			zero += Vector3.up;
		}
		if (Input.GetKey(KeyCode.Q))
		{
			zero += Vector3.down;
		}
		moveSpeed += Input.mouseScrollDelta.y;
		float num = moveSpeed;
		if (Input.GetKey(KeyCode.LeftShift))
		{
			num *= shiftMultiplier;
		}
		Vector3 vector = zero.normalized * num;
		base.transform.position += vector * Time.deltaTime;
		Vector2 vector2 = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
		Vector3 vector3 = rotation * Vector3.forward;
		rotation = Quaternion.AngleAxis(vector2.x * lookSensitivity, Vector3.up) * rotation;
		rotation = Quaternion.AngleAxis(vector2.y * lookSensitivity, -base.transform.right) * rotation;
		rotation = Quaternion.RotateTowards(Quaternion.LookRotation(Vector3.ProjectOnPlane(vector3, Vector3.up)), rotation, 88f);
		vector3 = rotation * Vector3.forward;
		Vector3 vector4 = Vector3.up;
		if (Input.GetKey(KeyCode.X))
		{
			vector4 = Quaternion.AngleAxis(-45f, vector3) * vector4;
		}
		if (Input.GetKey(KeyCode.Z))
		{
			vector4 = Quaternion.AngleAxis(45f, vector3) * vector4;
		}
		rotation = Quaternion.LookRotation(vector3, vector4);
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, rotation, lookSmoothing * Time.deltaTime);
	}
}
