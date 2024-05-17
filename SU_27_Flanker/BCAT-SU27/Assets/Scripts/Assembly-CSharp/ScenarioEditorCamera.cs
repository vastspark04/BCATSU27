using OC;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScenarioEditorCamera : MonoBehaviour
{
	public enum CursorLocations
	{
		Air,
		Ground,
		Water
	}

	public VTScenarioEditor editor;

	public float keyMoveSpeed;

	public float mouseRotateSpeed = 1f;

	public float zoomScrollRate = 10f;

	public float altitudeScrollRate = 10f;

	public float altitudeKeyRate = 10f;

	public float rotationRate = 60f;

	public float maxAltitude = 8000f;

	public float maxRayDist = 80000f;

	public Transform focusTransform;

	public LineRenderer focusLine;

	public float focusLineWidth = 0.005f;

	private float dist = 1000f;

	private float lerpedDist = 1000f;

	private Vector3 vecToCam;

	private Vector3 prevPlanarVecToCam;

	private FixedPoint mouseDownPoint;

	private FixedPoint mouseCurrentPoint;

	private FixedPoint origFocusPoint;

	private bool middleMouseGrabbed;

	private bool middleMouseRotation;

	private Vector3 middleMousePosStart;

	private float middleMouseRotRate = 0.1f;

	private float mouseGrabAltitude;

	private float lastClickTime = -1f;

	private float altitude = 1f;

	private float lerpedAltitude = 1f;

	public InputLock inputLock = new InputLock("input");

	public InputLock scrollLock = new InputLock("scroll");

	public InputLock doubleClickLock = new InputLock("double click");

	private Vector3 slerpedVecToCam;

	private Vector3 surfaceNormal;

	private VTLineDrawer lineDrawer;

	public Camera cam { get; private set; }

	public CursorLocations cursorLocation { get; private set; }

	public float surfaceAlt { get; private set; }

	public float cursorAlt => altitude;

	public float cursorHeading { get; private set; }

	public float cursorAGL { get; private set; }

	public void SetCursorAlt(float alt)
	{
		if (alt <= surfaceAlt)
		{
			alt = surfaceAlt;
		}
		else
		{
			cursorLocation = CursorLocations.Air;
		}
		altitude = Mathf.Clamp(alt, 0f, maxAltitude);
		lerpedAltitude = altitude;
	}

	private void Start()
	{
		cam = GetComponent<Camera>();
		focusTransform.parent = null;
		focusTransform.position = base.transform.position + base.transform.forward * 1000f + base.transform.up * -1000f;
		focusTransform.gameObject.AddComponent<FloatingOriginTransform>();
		Vector3 position = focusTransform.position;
		position.y = WaterPhysics.instance.height;
		focusTransform.position = position;
		vecToCam = base.transform.position - focusTransform.position;
		prevPlanarVecToCam = Vector3.ProjectOnPlane(vecToCam, Vector3.up);
		ClampCameraAngle();
		mouseDownPoint = default(FixedPoint);
		mouseCurrentPoint = default(FixedPoint);
		origFocusPoint = new FixedPoint(focusTransform.position);
		lineDrawer = base.gameObject.AddComponent<VTLineDrawer>();
		if (VTResources.useOverCloud)
		{
			OverCloudCamera component = GetComponent<OverCloudCamera>();
			if ((bool)component)
			{
				component.enabled = true;
			}
		}
	}

	private void Update()
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

	public void SetCursorHeading(float heading)
	{
		cursorHeading = Mathf.Repeat(heading, 360f);
		UpdateLocationInfo();
	}

	private void UpdateMovement()
	{
		if (inputLock.isLocked || IsAnyInputFocused())
		{
			return;
		}
		Vector3 vector = UIUtils.RewiredMouseInput();
		Vector3 position = focusTransform.position;
		if ((bool)VTMapGenerator.fetch && !VTMapGenerator.fetch.colliderBakeComplete)
		{
			VTMapGenerator.fetch.BakeColliderAtPosition(focusTransform.position);
		}
		if (Input.GetMouseButtonDown(0) && !doubleClickLock.isLocked)
		{
			if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
			{
				if (GetMouseWorldPosition(out mouseDownPoint))
				{
					Vector3 point = mouseDownPoint.point;
					FocusOnPoint(point);
					position = point;
				}
			}
			else
			{
				lastClickTime = Time.unscaledTime;
			}
		}
		UpdateSurfaceAlt();
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
		slerpedVecToCam = Vector3.Slerp(slerpedVecToCam, vecToCam, 15f * Time.unscaledDeltaTime);
		if (Input.GetMouseButtonDown(2))
		{
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				middleMouseRotation = true;
				middleMousePosStart = Input.mousePosition;
			}
			else if (GetMouseWorldPosition(out mouseDownPoint))
			{
				mouseCurrentPoint.point = mouseDownPoint.point;
				origFocusPoint.point = focusTransform.position;
				mouseGrabAltitude = WaterPhysics.GetAltitude(mouseDownPoint.point);
				middleMouseGrabbed = true;
			}
		}
		else if (Input.GetMouseButton(2))
		{
			if (middleMouseGrabbed)
			{
				if (GetMousePositionPlanar(out mouseCurrentPoint, mouseGrabAltitude))
				{
					Vector3 vector2 = mouseCurrentPoint.point - mouseDownPoint.point;
					vector2.y = 0f;
					position -= vector2;
				}
			}
			else if (middleMouseRotation)
			{
				float x2 = (Input.mousePosition - middleMousePosStart).x;
				middleMousePosStart = Input.mousePosition;
				float num2 = x2 * middleMouseRotRate;
				cursorHeading += num2;
				cursorHeading = Mathf.Repeat(cursorHeading, 360f);
			}
		}
		else if (Input.GetMouseButtonUp(2))
		{
			middleMouseGrabbed = false;
			middleMouseRotation = false;
		}
		else
		{
			bool flag = false;
			bool flag2 = true;
			if (Input.GetKey(KeyCode.LeftShift))
			{
				if (!scrollLock.isLocked && Mathf.Abs(vector.z) > 0f)
				{
					altitude += altitudeScrollRate * dist * 0.01f * vector.z;
					flag2 = false;
				}
				flag = true;
			}
			else if (!scrollLock.isLocked)
			{
				dist = Mathf.Clamp(dist - dist * zoomScrollRate * vector.z, 8f, 25000f);
			}
			if (Input.GetKey(KeyCode.X))
			{
				altitude += (float)((!flag) ? 1 : 3) * dist * altitudeKeyRate * Time.unscaledDeltaTime;
				flag2 = false;
			}
			if (Input.GetKey(KeyCode.Z))
			{
				altitude -= (float)((!flag) ? 1 : 3) * dist * altitudeKeyRate * Time.unscaledDeltaTime;
				flag2 = false;
			}
			altitude = Mathf.Clamp(altitude, surfaceAlt, maxAltitude);
			lerpedAltitude = Mathf.Lerp(lerpedAltitude, altitude, 20f * Time.unscaledDeltaTime);
			lerpedDist = Mathf.Lerp(lerpedDist, dist, 20f * Time.unscaledDeltaTime);
			float num5 = (focusLine.startWidth = (focusLine.endWidth = focusLineWidth * lerpedDist));
			if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
			{
				Vector3 zero = Vector3.zero;
				if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
				{
					zero += Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
				}
				if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
				{
					zero -= Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
				}
				if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
				{
					zero += cam.transform.right;
				}
				if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
				{
					zero -= cam.transform.right;
				}
				zero.Normalize();
				if (Input.GetKey(KeyCode.E))
				{
					cursorHeading += (float)((!flag) ? 1 : 3) * rotationRate * Time.unscaledDeltaTime;
					cursorHeading = Mathf.Repeat(cursorHeading, 360f);
				}
				if (Input.GetKey(KeyCode.Q))
				{
					cursorHeading -= (float)((!flag) ? 1 : 3) * rotationRate * Time.unscaledDeltaTime;
					cursorHeading = Mathf.Repeat(cursorHeading, 360f);
				}
				position += (float)((!flag) ? 1 : 3) * keyMoveSpeed * dist * Time.unscaledDeltaTime * zero;
			}
			if (flag2 && cursorLocation != 0)
			{
				altitude = (lerpedAltitude = surfaceAlt);
			}
		}
		UpdateLocationInfo();
		position.y = WaterPhysics.instance.height + lerpedAltitude;
		focusTransform.position = position;
		base.transform.position = focusTransform.position + slerpedVecToCam.normalized * lerpedDist;
		base.transform.LookAt(focusTransform);
		if (Input.GetKeyDown(KeyCode.U))
		{
			editor.newUnitsWindow.OpenWindow();
		}
	}

	private void ClampCameraAngle()
	{
		vecToCam = Vector3.RotateTowards(prevPlanarVecToCam, vecToCam, 1.3962634f, float.MaxValue);
		vecToCam = Vector3.RotateTowards(Vector3.up, vecToCam, 1.3962634f, float.MaxValue);
	}

	private void UpdateSurfaceAlt()
	{
		surfaceNormal = Vector3.up;
		RaycastHit hitInfo;
		if (altitude > 0f && Physics.Raycast(new Ray(PositionAtAlt(focusTransform.position, altitude + 1f), Vector3.up), 500f, 1))
		{
			if (Physics.Raycast(new Ray(PositionAtAlt(focusTransform.position, altitude + 1f), Vector3.down), out hitInfo, maxAltitude, 1))
			{
				surfaceAlt = WaterPhysics.GetAltitude(hitInfo.point);
				surfaceNormal = hitInfo.normal;
			}
			else
			{
				surfaceAlt = 0f;
			}
		}
		else if (Physics.Raycast(new Ray(focusTransform.position + maxAltitude * Vector3.up, Vector3.down), out hitInfo, altitude + maxAltitude, 1))
		{
			surfaceAlt = WaterPhysics.GetAltitude(hitInfo.point);
			surfaceNormal = hitInfo.normal;
		}
		else
		{
			surfaceAlt = 0f;
		}
		cursorAGL = WaterPhysics.GetAltitude(focusTransform.position) - surfaceAlt;
	}

	public void UpdateLocationInfo()
	{
		if (altitude < 1f)
		{
			cursorLocation = CursorLocations.Water;
		}
		else if (altitude < surfaceAlt + 1f)
		{
			cursorLocation = CursorLocations.Ground;
		}
		else
		{
			cursorLocation = CursorLocations.Air;
		}
		if (cursorLocation == CursorLocations.Ground)
		{
			Vector3 forward = Vector3.ProjectOnPlane(VectorUtils.BearingVector(cursorHeading), surfaceNormal);
			focusTransform.rotation = Quaternion.LookRotation(forward, surfaceNormal);
		}
		else
		{
			focusTransform.rotation = Quaternion.LookRotation(VectorUtils.BearingVector(cursorHeading), Vector3.up);
		}
	}

	private Vector3 PositionAtAlt(Vector3 position, float alt)
	{
		position.y = WaterPhysics.instance.height + alt;
		return position;
	}

	public void FocusOnPoint(Vector3 point)
	{
		focusTransform.position = point;
		altitude = WaterPhysics.GetAltitude(point);
		lerpedAltitude = altitude;
		UpdateSurfaceAlt();
		altitude = Mathf.Clamp(altitude, surfaceAlt, maxAltitude);
		Debug.LogFormat("Focused on point at altitude:{0} (surfAlt:{1}", altitude, surfaceAlt);
		lerpedAltitude = altitude;
		UpdateLocationInfo();
	}

	public bool GetMouseWorldPosition(out FixedPoint fixedPoint)
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

	public void DrawLine(Vector3 a, Vector3 b, Color color)
	{
		lineDrawer.DrawLine(a, b, color);
	}

	public void DrawCircle(Vector3 center, float radius, Color color)
	{
		lineDrawer.DrawCircle(center, radius, color);
	}

	public void DrawCircle(Vector3 center, float radius, Color color, Vector3 axis)
	{
		lineDrawer.DrawCircle(center, radius, color, axis);
	}

	public void DrawWireSphere(Vector3 center, float radius, Color color)
	{
		lineDrawer.DrawWireSphere(center, radius, color);
	}
}
