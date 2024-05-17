using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTMapEditorCamera : MonoBehaviour
{
	public VTMapEditor editor;

	public Camera cam;

	public float keyMoveSpeed = 0.5f;

	public float mouseRotateSpeed = 333f;

	public float zoomScrollRate = 0.2f;

	public float maxZoomDist = 40000f;

	public float maxRayDist = 10000f;

	private Transform focusTransform;

	public InputLock inputLock = new InputLock("input");

	public InputLock scrollLock = new InputLock("scroll");

	public InputLock doubleClickLock = new InputLock("double click");

	private float lastClickTime;

	private Vector3 vecToCam;

	private Vector3 prevPlanarVecToCam;

	private float lerpedDist;

	private float dist = 20f;

	private bool middleMouseGrabbed;

	private FixedPoint mouseDownPoint;

	private FixedPoint mouseCurrentPoint;

	private FixedPoint origFocusPoint;

	private float mouseGrabAltitude;

	private void Start()
	{
		focusTransform = new GameObject("FocusTf").transform;
		focusTransform.gameObject.AddComponent<FloatingOriginTransform>();
		vecToCam = new Vector3(20f, 20f, 20f);
		prevPlanarVecToCam = new Vector3(20f, 0f, 20f);
		mouseDownPoint = default(FixedPoint);
		mouseCurrentPoint = default(FixedPoint);
		origFocusPoint = default(FixedPoint);
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		while (!VTMapGenerator.fetch.HasFinishedInitialGeneration())
		{
			yield return null;
		}
		int num = VTMapGenerator.fetch.gridSize / 2;
		Vector3 vector = VTMapGenerator.fetch.GridToWorldPos(new IntVector2(num, num));
		origFocusPoint.point = vector;
		focusTransform.position = vector;
	}

	private void LateUpdate()
	{
		UpdateMovement();
	}

	private bool IsAnyInputFocused()
	{
		if ((bool)EventSystem.current.currentSelectedGameObject)
		{
			InputField component = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();
			if ((bool)component && component.isFocused)
			{
				return true;
			}
		}
		return false;
	}

	public void FocusOnPoint(Vector3 worldPos)
	{
		focusTransform.position = worldPos;
	}

	public void MouseClick()
	{
		if (doubleClickLock.isLocked)
		{
			return;
		}
		if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
		{
			if (GetMouseWorldPosition(out mouseDownPoint))
			{
				Vector3 point = mouseDownPoint.point;
				FocusOnPoint(point);
			}
		}
		else
		{
			lastClickTime = Time.unscaledTime;
		}
	}

	private void UpdateMovement()
	{
		if (inputLock.isLocked || IsAnyInputFocused())
		{
			return;
		}
		Vector3 position = focusTransform.position;
		Vector3 vector = UIUtils.RewiredMouseInput();
		if (Input.GetMouseButtonDown(1))
		{
			prevPlanarVecToCam = vecToCam;
			prevPlanarVecToCam.y = 0f;
		}
		else if (Input.GetMouseButton(1))
		{
			float num = Mathf.Min(Time.unscaledDeltaTime, 0.02f);
			float x = vector.x;
			float angle = mouseRotateSpeed * x * num;
			vecToCam = Quaternion.AngleAxis(angle, Vector3.up) * vecToCam;
			prevPlanarVecToCam = Quaternion.AngleAxis(angle, Vector3.up) * prevPlanarVecToCam;
			float y = vector.y;
			float angle2 = mouseRotateSpeed * y * num;
			Vector3 axis = Vector3.Cross(Vector3.up, vecToCam);
			vecToCam = Quaternion.AngleAxis(angle2, axis) * vecToCam;
			ClampCameraAngle();
		}
		if (Input.GetMouseButtonDown(2))
		{
			if (GetMouseWorldPosition(out mouseDownPoint))
			{
				mouseCurrentPoint.point = mouseDownPoint.point;
				origFocusPoint.point = focusTransform.position;
				mouseGrabAltitude = WaterPhysics.GetAltitude(mouseDownPoint.point);
				middleMouseGrabbed = true;
			}
		}
		else if (Input.GetMouseButton(2) && middleMouseGrabbed)
		{
			if (GetMousePositionPlanar(out mouseCurrentPoint, mouseGrabAltitude))
			{
				Vector3 vector2 = mouseCurrentPoint.point - mouseDownPoint.point;
				vector2.y = 0f;
				position -= vector2;
			}
		}
		else if (Input.GetMouseButtonUp(2))
		{
			middleMouseGrabbed = false;
		}
		else
		{
			bool flag = false;
			if (!scrollLock.isLocked && !GetCtrlKey() && !GetAltKey())
			{
				float z = vector.z;
				dist = Mathf.Clamp(dist - dist * zoomScrollRate * z, 8f, maxZoomDist);
			}
			lerpedDist = Mathf.Lerp(lerpedDist, dist, 20f * Time.unscaledDeltaTime);
			flag = Input.GetKey(KeyCode.LeftShift);
			if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
			{
				Vector3 zero = Vector3.zero;
				bool flag2 = false;
				if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
				{
					zero += Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
					flag2 = true;
				}
				if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
				{
					zero -= Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
					flag2 = true;
				}
				if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
				{
					zero += cam.transform.right;
					flag2 = true;
				}
				if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
				{
					zero -= cam.transform.right;
					flag2 = true;
				}
				if (flag2)
				{
					zero.Normalize();
					position += (float)((!flag) ? 1 : 3) * keyMoveSpeed * dist * Time.unscaledDeltaTime * zero;
				}
			}
		}
		if (!middleMouseGrabbed)
		{
			position.y = WaterPhysics.instance.height;
			if (Physics.Raycast(position + 9000f * Vector3.up, Vector3.down, out var hitInfo, 9000f, 1))
			{
				position.y = hitInfo.point.y;
			}
		}
		focusTransform.position = position;
		base.transform.position = focusTransform.position + vecToCam.normalized * lerpedDist;
		base.transform.LookAt(focusTransform);
		if (Physics.Linecast(base.transform.position + 9000f * Vector3.up, base.transform.position + 5f * Vector3.down, out var hitInfo2, 1))
		{
			Vector3 position2 = base.transform.position;
			position2.y = hitInfo2.point.y + 5f;
			base.transform.position = position2;
		}
	}

	private bool GetCtrlKey()
	{
		if (!Input.GetKey(KeyCode.LeftControl))
		{
			return Input.GetKey(KeyCode.RightControl);
		}
		return true;
	}

	private bool GetAltKey()
	{
		if (!Input.GetKey(KeyCode.LeftAlt))
		{
			return Input.GetKey(KeyCode.RightAlt);
		}
		return true;
	}

	private void ClampCameraAngle()
	{
		vecToCam = Vector3.RotateTowards(prevPlanarVecToCam, vecToCam, 1.3962634f, float.MaxValue);
		vecToCam = Vector3.RotateTowards(Vector3.up, vecToCam, 1.3962634f, float.MaxValue);
	}

	private bool GetMouseWorldPosition(out FixedPoint fixedPoint)
	{
		fixedPoint = default(FixedPoint);
		float num = maxRayDist;
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		float num2 = -1f;
		if (Physics.Raycast(ray, out var hitInfo, num, 1))
		{
			num2 = hitInfo.distance;
		}
		if (WaterPhysics.instance.waterPlane.Raycast(ray, out var enter) && enter < num)
		{
			if (num2 > 0f && enter < num2)
			{
				fixedPoint.point = ray.GetPoint(enter);
				return true;
			}
			if (num2 > 0f)
			{
				fixedPoint.point = hitInfo.point;
				return true;
			}
			fixedPoint.point = ray.GetPoint(enter);
			return true;
		}
		if (num2 > 0f)
		{
			fixedPoint.point = hitInfo.point;
			return true;
		}
		return false;
	}

	private bool GetMousePositionPlanar(out FixedPoint fixedPoint, float altitude)
	{
		fixedPoint = default(FixedPoint);
		Plane plane = new Plane(Vector3.up, new Vector3(0f, WaterPhysics.instance.height + altitude, 0f));
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (plane.Raycast(ray, out var enter))
		{
			if (enter < maxRayDist)
			{
				ray.GetPoint(enter);
				fixedPoint.point = ray.GetPoint(enter);
				return true;
			}
			return false;
		}
		return false;
	}
}
