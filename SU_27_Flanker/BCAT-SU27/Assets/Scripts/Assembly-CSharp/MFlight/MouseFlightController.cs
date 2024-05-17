using UnityEngine;

namespace MFlight{

public class MouseFlightController : MonoBehaviour
{
	[Header("Components")]
	[SerializeField]
	[Tooltip("Transform of the aircraft the rig follows and references")]
	private Transform aircraft;

	[SerializeField]
	[Tooltip("Transform of the object the mouse rotates to generate MouseAim position")]
	private Transform mouseAim;

	[SerializeField]
	[Tooltip("Transform of the object on the rig which the camera is attached to")]
	private Transform cameraRig;

	[SerializeField]
	[Tooltip("Transform of the camera itself")]
	private Transform cam;

	[Header("Options")]
	[SerializeField]
	[Tooltip("Follow aircraft using fixed update loop")]
	private bool useFixed = true;

	[SerializeField]
	[Tooltip("How quickly the camera tracks the mouse aim point.")]
	private float camSmoothSpeed = 5f;

	[SerializeField]
	[Tooltip("Mouse sensitivity for the mouse flight target")]
	private float mouseSensitivity = 3f;

	[SerializeField]
	[Tooltip("How far the boresight and mouse flight are from the aircraft")]
	private float aimDistance = 500f;

	[Space]
	[SerializeField]
	[Tooltip("How far the boresight and mouse flight are from the aircraft")]
	private bool showDebugInfo;

	[SerializeField]
	[Tooltip("Lock the mouse inside the game window.")]
	private bool lockMouse;

	public Vector3 BoresightPos
	{
		get
		{
			if (!(aircraft == null))
			{
				return aircraft.transform.forward * aimDistance + aircraft.transform.position;
			}
			return base.transform.forward * aimDistance;
		}
	}

	public Vector3 MouseAimPos
	{
		get
		{
			if (!(mouseAim == null))
			{
				return mouseAim.transform.forward * aimDistance + mouseAim.transform.position;
			}
			return base.transform.forward * aimDistance;
		}
	}

	private void Awake()
	{
		if (aircraft == null)
		{
			Debug.LogError(base.name + "MouseFlightController - No aircraft transform assigned!");
		}
		if (mouseAim == null)
		{
			Debug.LogError(base.name + "MouseFlightController - No mouse aim transform assigned!");
		}
		if (cameraRig == null)
		{
			Debug.LogError(base.name + "MouseFlightController - No camera rig transform assigned!");
		}
		if (cam == null)
		{
			Debug.LogError(base.name + "MouseFlightController - No camera transform assigned!");
		}
		base.transform.parent = null;
		Cursor.lockState = (lockMouse ? CursorLockMode.Locked : CursorLockMode.None);
	}

	private void OnValidate()
	{
		Cursor.lockState = ((Application.isPlaying && lockMouse) ? CursorLockMode.Locked : CursorLockMode.None);
	}

	private void Update()
	{
		if (!useFixed)
		{
			UpdateCameraPos();
		}
		RotateRig();
	}

	private void FixedUpdate()
	{
		if (useFixed)
		{
			UpdateCameraPos();
		}
	}

	private void RotateRig()
	{
		if (!(mouseAim == null) && !(cam == null) && !(cameraRig == null))
		{
			float angle = Input.GetAxis("Mouse X") * mouseSensitivity;
			float angle2 = (0f - Input.GetAxis("Mouse Y")) * mouseSensitivity;
			mouseAim.Rotate(cam.right, angle2, Space.World);
			mouseAim.Rotate(cam.up, angle, Space.World);
			Vector3 upwards = ((Mathf.Abs(mouseAim.forward.y) > 0.9f) ? cameraRig.up : Vector3.up);
			cameraRig.rotation = Damp(cameraRig.rotation, Quaternion.LookRotation(MouseAimPos - cameraRig.position, upwards), camSmoothSpeed, Time.deltaTime);
		}
	}

	private void UpdateCameraPos()
	{
		if (aircraft != null)
		{
			base.transform.position = aircraft.position;
		}
	}

	private Quaternion Damp(Quaternion a, Quaternion b, float lambda, float dt)
	{
		return Quaternion.Slerp(a, b, 1f - Mathf.Exp((0f - lambda) * dt));
	}

	private void OnDrawGizmos()
	{
		if (showDebugInfo)
		{
			Color color = Gizmos.color;
			if (aircraft != null)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(BoresightPos, 10f);
			}
			if (mouseAim != null)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawWireSphere(MouseAimPos, 10f);
				Gizmos.color = Color.blue;
				Gizmos.DrawRay(mouseAim.position, mouseAim.forward * 50f);
				Gizmos.color = Color.green;
				Gizmos.DrawRay(mouseAim.position, mouseAim.up * 50f);
				Gizmos.color = Color.red;
				Gizmos.DrawRay(mouseAim.position, mouseAim.right * 50f);
			}
			Gizmos.color = color;
		}
	}
}}
