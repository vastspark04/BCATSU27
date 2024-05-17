using System;
using System.Collections;
using UnityEngine;
using VTOLVR.Multiplayer;

public class OpticalTargeter : MonoBehaviour, IQSVehicleComponent
{
	private class LaserWidthUpdater : MonoBehaviour
	{
		public LineRenderer lr;

		public float widthFactor;

		public float minWidth;

		public float maxWidth;

		private void OnWillRenderObject()
		{
			Camera current = Camera.current;
			float num = current.fieldOfView / 60f;
			Vector3 b = lr.transform.TransformPoint(lr.GetPosition(0));
			Vector3 b2 = lr.transform.TransformPoint(lr.GetPosition(1));
			lr.startWidth = Mathf.Clamp(widthFactor * Vector3.Distance(current.transform.position, b) * num, minWidth, maxWidth);
			lr.endWidth = Mathf.Clamp(widthFactor * Vector3.Distance(current.transform.position, b2) * num, minWidth, maxWidth);
		}
	}

	public Actor actor;

	public ModuleTurret sensorTurret;

	public Transform lockTransform;

	public Transform cameraTransform;

	public WeaponManager wm;

	public float maxLockingDistance = 14000f;

	public float lockingFOV = 10f;

	public float actorLockingSizeRequirement = -1f;

	private bool _vLase;

	public LineRenderer laserLine;

	public float laserLineWidthFactor;

	public float minLaserLineWidth;

	public float maxLaserLineWidth;

	public float laserRaycastStartDist = 10f;

	private bool hasLaser;

	private bool _overriddenDirection;

	private Vector3 _oDir;

	private Vector3 _oUp;

	public float overriddenDirSmoothRate = 5f;

	public bool powered = true;

	private Coroutine eofSlewRoutine;

	private float stoppedEofSlewTime;

	public Actor lockedActor { get; private set; }

	public bool locked { get; private set; }

	public bool visibleLaser
	{
		get
		{
			return _vLase;
		}
		set
		{
			if (value != _vLase)
			{
				_vLase = value;
				this.OnSetVisibleLaser?.Invoke(value);
			}
		}
	}

	public FixedPoint laserPoint { get; private set; }

	public bool isGimbalLimit { get; private set; }

	public bool lockedSky { get; private set; }

	public Vector3 targetVelocity
	{
		get
		{
			if ((bool)lockedActor)
			{
				return lockedActor.velocity;
			}
			return Vector3.zero;
		}
	}

	public bool laserOccluded { get; private set; }

	public event Action<bool> OnSetVisibleLaser;

	public event Action<Vector3> OnOverridenDirection;

	public event Action<Vector3> OnSlewedDirection;

	public event Action OnUnlocked;

	public event Action<Actor> OnUnlockedActor;

	public event Action<Vector3> OnLockedGround;

	public event Action<Actor> OnLockedActor;

	public event Action<Vector3> OnLockedSky;

	public void OverrideAimToDirection(Vector3 direction, Vector3 up)
	{
		_oDir = direction.normalized;
		_oUp = up;
		_overriddenDirection = true;
	}

	private void Awake()
	{
		if (!lockTransform)
		{
			lockTransform = new GameObject("lockTf").transform;
		}
		lockTransform.parent = base.transform;
		if ((bool)laserLine)
		{
			hasLaser = true;
			laserLine.enabled = false;
			LaserWidthUpdater laserWidthUpdater = laserLine.gameObject.AddComponent<LaserWidthUpdater>();
			laserWidthUpdater.lr = laserLine;
			laserWidthUpdater.minWidth = minLaserLineWidth;
			laserWidthUpdater.maxWidth = maxLaserLineWidth;
			laserWidthUpdater.widthFactor = laserLineWidthFactor;
		}
	}

	private void Start()
	{
		FloatingOrigin.instance.OnOriginShift += FloatingOrigin_instance_OnOriginShift;
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		if ((bool)lockTransform && lockTransform.parent == null)
		{
			lockTransform.position += offset;
		}
	}

	private void LateUpdate()
	{
		CheckOcclusion();
		if (locked)
		{
			_overriddenDirection = false;
			if ((bool)lockedActor)
			{
				if ((!lockedActor.alive && lockedActor.velocity.sqrMagnitude < 1f) || !lockedActor.gameObject.activeInHierarchy)
				{
					AreaLockPosition(lockedActor.position);
				}
				else
				{
					lockTransform.position = lockedActor.position;
					if ((bool)actor)
					{
						lockedActor.UpdateKnownPosition(actor);
					}
				}
			}
			if ((bool)sensorTurret)
			{
				isGimbalLimit = IsGimbalLimit();
				sensorTurret.AimToTarget(lockTransform.position);
				if (isGimbalLimit)
				{
					laserOccluded = true;
					cameraTransform.rotation = Quaternion.LookRotation(sensorTurret.pitchTransform.forward, Vector3.up);
				}
				else
				{
					cameraTransform.rotation = Quaternion.LookRotation(lockTransform.position - cameraTransform.position, Vector3.up);
				}
			}
			else
			{
				isGimbalLimit = false;
				cameraTransform.LookAt(lockTransform);
			}
			if (hasLaser)
			{
				if (visibleLaser && !lockedSky && !isGimbalLimit && (lockTransform.position - cameraTransform.position).sqrMagnitude < maxLockingDistance * maxLockingDistance)
				{
					laserLine.enabled = true;
					Vector3 vector = cameraTransform.position;
					if ((bool)wm && (bool)wm.vesselRB)
					{
						Vector3 vector2 = wm.vesselRB.transform.InverseTransformPoint(vector);
						vector = wm.vesselRB.position + wm.vesselRB.rotation * vector2;
					}
					if (Physics.Raycast(vector + laserRaycastStartDist * cameraTransform.forward, cameraTransform.forward, out var hitInfo, maxLockingDistance, 8449, QueryTriggerInteraction.Ignore))
					{
						laserPoint = new FixedPoint(hitInfo.point);
					}
					else
					{
						laserPoint = new FixedPoint(cameraTransform.position + cameraTransform.forward * maxLockingDistance);
					}
					laserLine.SetPosition(0, Vector3.zero);
					laserLine.SetPosition(1, laserLine.transform.InverseTransformPoint(laserPoint.point));
				}
				else
				{
					laserLine.enabled = false;
				}
			}
			if ((bool)wm.battery && !wm.battery.Drain(0.01f * Time.deltaTime))
			{
				Unlock();
			}
			return;
		}
		if ((bool)sensorTurret)
		{
			if (_overriddenDirection)
			{
				isGimbalLimit = IsGimbalLimit();
				Quaternion rotation = cameraTransform.rotation;
				sensorTurret.AimToTarget(sensorTurret.yawTransform.position + 8000f * _oDir);
				Quaternion b = ((!isGimbalLimit) ? Quaternion.LookRotation(_oDir, _oUp) : Quaternion.LookRotation(sensorTurret.pitchTransform.forward, _oUp));
				float t = ((overriddenDirSmoothRate < 50f) ? (overriddenDirSmoothRate * Time.deltaTime) : 1f);
				cameraTransform.rotation = Quaternion.Slerp(rotation, b, t);
				lockTransform.position = cameraTransform.position + cameraTransform.forward * 5000f;
				this.OnOverridenDirection?.Invoke(_oDir);
				_overriddenDirection = false;
			}
			else
			{
				isGimbalLimit = false;
				sensorTurret.ReturnTurret();
			}
		}
		else
		{
			isGimbalLimit = false;
			if (_overriddenDirection)
			{
				cameraTransform.rotation = Quaternion.LookRotation(_oDir, _oUp);
				this.OnOverridenDirection?.Invoke(_oDir);
				_overriddenDirection = false;
			}
		}
		lockTransform.position = cameraTransform.position + cameraTransform.forward * 8000f;
		if (hasLaser)
		{
			laserLine.enabled = false;
		}
	}

	private bool IsGimbalLimit()
	{
		if (!sensorTurret)
		{
			return false;
		}
		Vector3 targetPosition = (locked ? lockTransform.position : (cameraTransform.position + _oDir * 8000f));
		bool num = !sensorTurret.TargetInGimbalRange(targetPosition);
		if (num && locked && (bool)lockedActor)
		{
			AreaLockPosition(lockedActor.position);
		}
		return num;
	}

	public bool CheckOcclusion()
	{
		laserOccluded = false;
		if (!locked || lockedSky)
		{
			laserOccluded = true;
		}
		if (!laserOccluded && Physics.Linecast(cameraTransform.position, lockTransform.position, out var hitInfo, 1))
		{
			if (!visibleLaser)
			{
				laserPoint = new FixedPoint(hitInfo.point);
			}
			if ((hitInfo.point - lockTransform.position).sqrMagnitude > 25f)
			{
				laserOccluded = true;
			}
			if (laserOccluded && (bool)lockedActor)
			{
				Actor componentInParent = hitInfo.collider.GetComponentInParent<Actor>();
				if ((bool)componentInParent && componentInParent == lockedActor)
				{
					laserOccluded = false;
				}
			}
		}
		if (laserOccluded && locked && (bool)lockedActor)
		{
			AreaLockPosition(lockedActor.position);
		}
		return laserOccluded;
	}

	public bool Lock(Vector3 point, bool lockActor = true)
	{
		return Lock(cameraTransform.position, point - cameraTransform.position, lockActor);
	}

	public bool Lock(Vector3 origin, Vector3 direction, bool lockActor = true)
	{
		lockedSky = false;
		lockTransform.parent = null;
		if (locked)
		{
			Unlock();
		}
		lockedActor = null;
		bool flag = false;
		Ray ray = new Ray(origin, direction);
		if (Physics.Raycast(ray, out var hitInfo, maxLockingDistance, 1, QueryTriggerInteraction.Ignore))
		{
			lockTransform.position = hitInfo.point;
			if (hitInfo.point.y < WaterPhysics.instance.height)
			{
				WaterPhysics.instance.waterPlane.Raycast(ray, out var enter);
				lockTransform.position = ray.GetPoint(enter);
			}
		}
		else
		{
			Vector3 position = ray.origin + ray.direction * maxLockingDistance;
			if (position.y < WaterPhysics.instance.height)
			{
				WaterPhysics.instance.waterPlane.Raycast(ray, out var enter2);
				position = ray.GetPoint(enter2);
			}
			else
			{
				lockTransform.parent = base.transform;
				lockedSky = true;
			}
			lockTransform.position = position;
		}
		if (lockActor)
		{
			int roleMask = 286;
			lockedActor = TargetManager.instance.GetOpticalTargetFromView(actor, maxLockingDistance, roleMask, 10f, origin, direction, lockingFOV, random: false, allActors: true, null, updateDetection: false, Teams.Allied, actorLockingSizeRequirement, raycastVisibilityOnly: true);
		}
		if ((bool)lockedActor)
		{
			Teams teams = Teams.Allied;
			if (VTOLMPUtils.IsMultiplayer())
			{
				teams = VTOLMPLobbyManager.localPlayerInfo.team;
			}
			if (actor.team == teams)
			{
				lockedActor.DiscoverActor();
			}
			lockedActor.DetectActor(actor.team, actor);
			lockedSky = false;
			this.OnLockedActor?.Invoke(lockedActor);
		}
		else if (!lockedSky)
		{
			flag = true;
			this.OnLockedGround?.Invoke(lockTransform.position);
		}
		if ((bool)lockedActor || flag || lockedSky)
		{
			if (lockedSky)
			{
				this.OnLockedSky?.Invoke(direction);
			}
			locked = true;
			lockTransform.gameObject.SetActive(value: true);
		}
		StopEofSlew();
		return locked;
	}

	public void ForceLockActor(Actor a)
	{
		Lock(a.position);
		if (lockedActor != a)
		{
			lockedActor = a;
			this.OnLockedActor?.Invoke(a);
		}
		Teams teams = Teams.Allied;
		if (VTOLMPUtils.IsMultiplayer())
		{
			teams = VTOLMPLobbyManager.localPlayerInfo.team;
		}
		if (actor.team == teams)
		{
			lockedActor.DiscoverActor();
		}
		lockedActor.DetectActor(actor.team, actor);
		lockedSky = false;
		locked = true;
		lockTransform.gameObject.SetActive(value: true);
	}

	public void AreaLockPosition(Vector3 position)
	{
		if (locked)
		{
			Unlock();
		}
		lockTransform.parent = null;
		lockTransform.position = position;
		lockedSky = false;
		lockedActor = null;
		locked = true;
		this.OnLockedGround?.Invoke(position);
	}

	public void Unlock()
	{
		Actor actor = lockedActor;
		lockedActor = null;
		locked = false;
		lockTransform.parent = base.transform;
		this.OnUnlocked?.Invoke();
		if ((bool)actor)
		{
			this.OnUnlockedActor?.Invoke(actor);
		}
	}

	public void Slew(Vector2 direction, float slewRate)
	{
		if (eofSlewRoutine != null)
		{
			StopCoroutine(eofSlewRoutine);
		}
		eofSlewRoutine = StartCoroutine(SlewAtEndOfFrame(direction, slewRate));
		lockedActor = null;
	}

	public void StopEofSlew()
	{
		if (eofSlewRoutine != null)
		{
			StopCoroutine(eofSlewRoutine);
		}
		stoppedEofSlewTime = Time.time;
	}

	private IEnumerator SlewAtEndOfFrame(Vector3 direction, float slewRate)
	{
		yield return new WaitForEndOfFrame();
		if (!(Time.time - stoppedEofSlewTime < 0.2f))
		{
			Vector3 vector = cameraTransform.position + 100f * cameraTransform.forward;
			vector += (cameraTransform.up * direction.y + cameraTransform.right * direction.x) * slewRate * Time.deltaTime;
			lockTransform.position = cameraTransform.position + (vector - cameraTransform.position).normalized * Mathf.Max(50f, Vector3.Distance(lockTransform.position, cameraTransform.position));
			isGimbalLimit = IsGimbalLimit();
			if (isGimbalLimit)
			{
				float pitchSpeedDPS = sensorTurret.pitchSpeedDPS;
				sensorTurret.pitchSpeedDPS = float.MaxValue;
				sensorTurret.yawSpeedDPS = float.MaxValue;
				sensorTurret.AimToTarget(lockTransform.position);
				cameraTransform.rotation = Quaternion.LookRotation(sensorTurret.pitchTransform.forward, Vector3.up);
				sensorTurret.pitchSpeedDPS = pitchSpeedDPS;
				sensorTurret.yawSpeedDPS = pitchSpeedDPS;
			}
			else
			{
				cameraTransform.rotation = Quaternion.LookRotation(lockTransform.position - cameraTransform.position, Vector3.up);
			}
			this.OnSlewedDirection?.Invoke(cameraTransform.forward);
			lockedActor = null;
		}
	}

	public void RemoteSlewToDirection(Vector3 direction)
	{
		sensorTurret.AimToTarget(direction);
		Vector3 vector = lockTransform.position - cameraTransform.position;
		vector = Quaternion.FromToRotation(vector, direction) * vector;
		lockTransform.position = cameraTransform.position + vector;
		cameraTransform.rotation = Quaternion.LookRotation(lockTransform.position - cameraTransform.position, Vector3.up);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_OpticalTargeter");
		configNode.SetValue("powered", powered);
		configNode.SetValue("locked", locked);
		configNode.SetValue("tgtGlobalPoint", ConfigNodeUtils.WriteVector3D(VTMapManager.WorldToGlobalPoint(lockTransform.position)));
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_OpticalTargeter";
		if (qsNode.HasValue(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			powered = ConfigNodeUtils.ParseBool(node.GetValue("powered"));
			ConfigNodeUtils.ParseBool(node.GetValue("locked"));
			if (locked)
			{
				Vector3 position = VTMapManager.GlobalToWorldPoint(ConfigNodeUtils.ParseVector3D(node.GetValue("tgtGlobalPoint")));
				AreaLockPosition(position);
			}
		}
	}
}
