using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class CameraFollowMe : MonoBehaviour
{
	public List<Actor> targets;

	private int idx;

	public Camera cam;

	public float distance = 8f;

	public float rotateSpeed = 3f;

	public float minDist = 1f;

	public float maxDist = 5000f;

	public float smoothRate = 10f;

	public int timeIdx = 3;

	public float[] timeScales = new float[6] { 0.1f, 0.25f, 0.5f, 1f, 2f, 4f };

	private float currDist;

	private Vector3 vectorToCam;

	private Vector3 finalVectorToCam;

	private bool tgtMode;

	private float tgtFov = 10f;

	private bool locked;

	private bool gunEnabled;

	private float gunLastFireTime;

	private Vector3 offsetLookVec = Vector3.forward;

	private Vector3 smoothOffsetLookVec = Vector3.forward;

	private bool extCam;

	private bool debugFlightInfo;

	private VRThrottle playerThrottle;

	private bool showPartKill;

	private List<VehiclePart> vParts;

	private bool heatDebug;

	private FlightInfo targetFlightInfo;

	private Transform lastTgt;

	private Transform secondTarget;

	private bool showUnitList;

	private bool drawSubdivDebug;

	public float debugSubdivTestRadius = 1400f;

	public static CameraFollowMe instance { get; private set; }

	private Transform currentTarget
	{
		get
		{
			if (idx >= 0 && idx < targets.Count && targets.Count > 0)
			{
				return targets[idx].transform;
			}
			return null;
		}
	}

	private bool hasTarget
	{
		get
		{
			if (idx > 0 && idx < targets.Count)
			{
				return targets[idx];
			}
			return false;
		}
	}

	private void Awake()
	{
		instance = this;
	}

	private IEnumerator Startup()
	{
		while (!VRHead.instance)
		{
			yield return null;
		}
		while (!ShipController.instance)
		{
			yield return null;
		}
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		if (!GetComponent<VRHead>())
		{
			base.gameObject.AddComponent<VRHead>();
		}
		FloatingOriginShifter component = GetComponent<FloatingOriginShifter>();
		component.enabled = false;
		component.enabled = true;
		if ((bool)LevelBuilder.fetch)
		{
			LevelBuilder.fetch.playerTransform = base.transform;
		}
		cam.transform.position = targets[0].position + Vector3.right;
		vectorToCam = Vector3.ProjectOnPlane(cam.transform.position - targets[0].position, Vector3.up);
		currDist = distance;
	}

	private void OnEnable()
	{
		StartCoroutine(Startup());
	}

	public void AddTarget(Actor actor)
	{
		targets.Add(actor);
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.V))
		{
			ToggleMode();
		}
		if (Input.GetKeyDown(KeyCode.T))
		{
			tgtMode = !tgtMode;
		}
		if (Input.GetKey(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.A))
		{
			extCam = !extCam;
			AudioController.instance.SetExteriorOpening("camFollow", extCam ? 1 : 0);
		}
		if (Input.GetKeyDown(KeyCode.LeftBracket) && targets.Count > 0)
		{
			SetTargetDebug(enable: false);
			targets.RemoveAll((Actor t) => t == null || !t.gameObject.activeInHierarchy);
			idx--;
			if (idx < 0)
			{
				idx = targets.Count - 1;
			}
			SetTargetDebug(enable: true);
			if (showPartKill)
			{
				ShowPartKill();
			}
		}
		if (Input.GetKeyDown(KeyCode.RightBracket) && targets.Count > 0)
		{
			SetTargetDebug(enable: false);
			targets.RemoveAll((Actor t) => t == null || !t.gameObject.activeInHierarchy);
			idx = (idx + 1) % targets.Count;
			SetTargetDebug(enable: true);
			if (showPartKill)
			{
				ShowPartKill();
			}
		}
		if (!VTOLMPUtils.IsMultiplayer())
		{
			if (Input.GetKeyDown(KeyCode.Period))
			{
				timeIdx++;
				if (timeIdx >= timeScales.Length)
				{
					timeIdx = timeScales.Length - 1;
				}
				Time.timeScale = timeScales[timeIdx];
			}
			if (Input.GetKeyDown(KeyCode.Comma))
			{
				timeIdx--;
				if (timeIdx < 0)
				{
					timeIdx = 0;
				}
				Time.timeScale = timeScales[timeIdx];
			}
			if (Input.GetKeyDown(KeyCode.H))
			{
				heatDebug = !heatDebug;
			}
		}
		if (Input.GetKeyDown(KeyCode.Slash))
		{
			timeIdx = 3;
			Time.timeScale = 1f;
		}
	}

	private void ShowPartKill()
	{
		if (!currentTarget)
		{
			HidePartKill();
			return;
		}
		showPartKill = true;
		vParts = new List<VehiclePart>();
		VehiclePart[] componentsInChildren = currentTarget.GetComponentsInChildren<VehiclePart>();
		foreach (VehiclePart item in componentsInChildren)
		{
			vParts.Add(item);
		}
	}

	private void HidePartKill()
	{
		showPartKill = false;
	}

	private void SetTargetDebug(bool enable)
	{
		targetFlightInfo = null;
		if (!hasTarget)
		{
			return;
		}
		if (enable)
		{
			targetFlightInfo = targets[idx].GetComponent<FlightInfo>();
		}
		AutoPilot component = targets[idx].GetComponent<AutoPilot>();
		if ((bool)component)
		{
			component.debug = enable;
		}
		Missile component2 = targets[idx].GetComponent<Missile>();
		if ((bool)component2)
		{
			component2.debugMissile = enable;
		}
		ShipMover component3 = targets[idx].GetComponent<ShipMover>();
		if ((bool)component3)
		{
			component3.debug = enable;
		}
		VRJoystick[] componentsInChildren;
		if (enable)
		{
			playerThrottle = targets[idx].GetComponentInChildren<VRThrottle>();
			componentsInChildren = targets[idx].GetComponentsInChildren<VRJoystick>(includeInactive: true);
			foreach (VRJoystick vRJoystick in componentsInChildren)
			{
				if ((bool)vRJoystick)
				{
					vRJoystick.debug = true;
				}
			}
			return;
		}
		playerThrottle = null;
		componentsInChildren = targets[idx].GetComponentsInChildren<VRJoystick>(includeInactive: true);
		foreach (VRJoystick vRJoystick2 in componentsInChildren)
		{
			if ((bool)vRJoystick2)
			{
				vRJoystick2.debug = false;
			}
		}
	}

	private void LateUpdate()
	{
		if (targets.Count == 0)
		{
			if (TargetManager.instance.allActors.Count == 0)
			{
				return;
			}
			foreach (Actor allActor in TargetManager.instance.allActors)
			{
				if (!allActor.parentActor)
				{
					targets.Add(allActor);
				}
			}
		}
		Vector3 vector = UIUtils.RewiredMouseInput();
		if (targets.Count > 0 && (idx >= targets.Count || idx < 0 || targets[idx] == null))
		{
			targets.RemoveAll((Actor t) => t == null);
			if (targets.Count == 0)
			{
				return;
			}
			idx %= targets.Count;
		}
		Actor actor = targets[idx];
		Transform transform = actor.transform;
		if ((bool)actor && (bool)actor.currentlyTargetingActor)
		{
			if (lastTgt != transform || secondTarget == null || secondTarget != actor.currentlyTargetingActor)
			{
				secondTarget = actor.currentlyTargetingActor.transform;
			}
		}
		else if (lastTgt != transform)
		{
			secondTarget = null;
		}
		lastTgt = transform;
		if ((!VTOLMPUtils.IsMultiplayer() || Application.isEditor) && Input.GetKeyDown(KeyCode.Tab))
		{
			gunEnabled = !gunEnabled;
			offsetLookVec = Vector3.forward;
		}
		if (gunEnabled && Input.GetMouseButton(0) && Time.time - gunLastFireTime > 0.05f)
		{
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			Bullet.FireBullet(ray.origin - cam.transform.up, ray.direction, 1100f, 0.1f, 0.01f, 0f, 10f, Vector3.zero, Color.red, null);
			gunLastFireTime = Time.time;
		}
		if (Input.GetMouseButton(1))
		{
			if (Input.GetKey(KeyCode.LeftShift))
			{
				float num = cam.fieldOfView / 40f;
				offsetLookVec = Quaternion.AngleAxis(rotateSpeed * num * vector.x / 10f, Vector3.up) * offsetLookVec;
				Vector3 axis = Vector3.Cross(-offsetLookVec, Vector3.up);
				offsetLookVec = Quaternion.AngleAxis((0f - rotateSpeed) * num * vector.y / 10f, axis) * offsetLookVec;
			}
			else
			{
				vectorToCam = Quaternion.AngleAxis(rotateSpeed * vector.x / 10f, Vector3.up) * vectorToCam;
				Vector3 axis2 = Vector3.Cross(vectorToCam, Vector3.up);
				vectorToCam = Quaternion.AngleAxis((0f - rotateSpeed) * vector.y / 10f, axis2) * vectorToCam;
			}
		}
		if (Input.GetKey(KeyCode.LeftShift))
		{
			float fieldOfView = cam.fieldOfView;
			fieldOfView += 0.1f * fieldOfView * vector.z;
			fieldOfView = Mathf.Clamp(fieldOfView, 1f, 60f);
			cam.fieldOfView = fieldOfView;
			tgtFov += 0.1f * tgtFov * vector.z;
			tgtFov = Mathf.Clamp(tgtFov, 1f, 30f);
		}
		else
		{
			distance -= vector.z * Mathf.Clamp(distance, 1f, 100f) * 0.15f;
		}
		if (Input.GetKeyDown(KeyCode.U))
		{
			showUnitList = !showUnitList;
		}
		if (Input.GetKeyDown(KeyCode.I))
		{
			drawSubdivDebug = !drawSubdivDebug;
		}
		distance = Mathf.Clamp(distance, minDist, maxDist);
		currDist = Mathf.Lerp(currDist, distance, smoothRate * Time.unscaledDeltaTime);
		if (vectorToCam == Vector3.zero)
		{
			vectorToCam = Vector3.forward;
		}
		if (tgtMode && (bool)secondTarget)
		{
			finalVectorToCam = Vector3.Slerp(finalVectorToCam, (transform.position - secondTarget.position).normalized * distance, smoothRate * Time.unscaledDeltaTime);
			cam.transform.position = transform.position + finalVectorToCam;
			if (locked)
			{
				cam.transform.position += Vector3.Cross(transform.up, -finalVectorToCam).normalized * 26f;
				cam.transform.rotation = Quaternion.Lerp(Quaternion.LookRotation(transform.position - cam.transform.position, transform.up), Quaternion.LookRotation(secondTarget.position - cam.transform.position, transform.up), 0.5f);
			}
			else
			{
				cam.transform.position += Vector3.Cross(Vector3.up, -finalVectorToCam).normalized * 26f;
				cam.transform.rotation = Quaternion.Lerp(Quaternion.LookRotation(transform.position - cam.transform.position), Quaternion.LookRotation(secondTarget.position - cam.transform.position), 0.5f);
			}
			cam.fieldOfView = Vector3.Angle(transform.position - cam.transform.position, secondTarget.position - cam.transform.position) + tgtFov;
			cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 1f, 60f);
			return;
		}
		vectorToCam = vectorToCam.normalized * currDist;
		finalVectorToCam = Vector3.Slerp(finalVectorToCam, vectorToCam, smoothRate * Time.unscaledDeltaTime);
		Vector3 position = transform.position + finalVectorToCam;
		if (locked)
		{
			position = transform.TransformPoint(finalVectorToCam);
		}
		cam.transform.position = position;
		smoothOffsetLookVec = Vector3.Slerp(smoothOffsetLookVec, offsetLookVec, smoothRate * Time.deltaTime);
		if (locked)
		{
			cam.transform.LookAt(transform, transform.up);
			Vector3 worldPosition = cam.transform.position + cam.transform.TransformDirection(smoothOffsetLookVec) * 1000f;
			cam.transform.LookAt(worldPosition, transform.up);
		}
		else
		{
			cam.transform.LookAt(transform);
			Vector3 worldPosition2 = cam.transform.position + cam.transform.TransformDirection(smoothOffsetLookVec) * 1000f;
			cam.transform.LookAt(worldPosition2);
		}
		if ((bool)WaterPhysics.instance)
		{
			if (Physics.Linecast(cam.transform.position + maxDist * Vector3.up, cam.transform.position + Vector3.down, out var hitInfo, 1) && hitInfo.point.y > WaterPhysics.instance.height + 1f)
			{
				cam.transform.position = hitInfo.point + Vector3.up;
			}
			else if (WaterPhysics.GetAltitude(cam.transform.position) < 1f)
			{
				Vector3 position2 = cam.transform.position;
				position2.y = WaterPhysics.instance.height + 1f;
				cam.transform.position = position2;
			}
		}
	}

	private void ToggleMode()
	{
		locked = !locked;
		if (locked)
		{
			vectorToCam = targets[idx].transform.InverseTransformVector(vectorToCam);
		}
		else
		{
			vectorToCam = targets[idx].transform.TransformVector(vectorToCam);
		}
	}

	private void UnitList()
	{
		float num = 18f;
		float num2 = 122f;
		float num3 = 20f;
		float num4 = 5f;
		int num5 = Mathf.FloorToInt(((float)Screen.height - 2f * num3) / num);
		float num6 = (float)Screen.width - num4 - num2;
		float num7 = num3;
		int num8 = 0;
		foreach (Actor target in targets)
		{
			if (!target)
			{
				continue;
			}
			if (GUI.Button(new Rect(num6, num7, num2, num), target.DebugName()))
			{
				SetTargetDebug(enable: false);
				idx = targets.IndexOf(target);
				SetTargetDebug(enable: true);
				if (showPartKill)
				{
					ShowPartKill();
				}
			}
			num8++;
			if (num8 > num5)
			{
				num8 = 0;
				num7 = num3;
				num6 -= num2 + 1f;
			}
			else
			{
				num7 += num;
			}
		}
	}

	private void ActorSubdivDebug()
	{
	}
}
