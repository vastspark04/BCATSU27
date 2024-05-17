using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockingRadar : MonoBehaviour
{
	public class AdvLockData
	{
		public enum LockStatus
		{
			Tracked,
			Received,
			Standby
		}

		public LockStatus lockStatus;

		private FixedPoint targetPt;

		public Vector3 velocity;

		public Actor actor;

		public LockingRadar lockingRadar;

		public Vector3 position => targetPt.point;

		public AdvLockData(LockStatus status, Vector3 pos, Vector3 vel, Actor actor, LockingRadar radar)
		{
			lockStatus = status;
			targetPt = new FixedPoint(pos);
			velocity = vel;
			this.actor = actor;
			lockingRadar = radar;
		}

		public void UpdatePosition(Vector3 pos)
		{
			targetPt.point = pos;
		}
	}

	public Radar radar;

	public Actor myActor;

	[Header("New Radar Params")]
	public float transmissionStrength = 5000f;

	public float receiverSensitivity = 500f;

	private bool _locked;

	private float pingInterval = 0.25f;

	private float lastPingTime;

	public ModuleTurret turret;

	public bool disableOnDeath = true;

	public float chaffThreshold = 18f;

	public float fov = 90f;

	public Transform referenceTransform;

	public bool isMissile;

	public bool debugRadar;

	private ChaffCountermeasure chaffModule;

	[Header("Legacy")]
	public float maxRange;

	private float rangeSqr;

	private Dictionary<Actor, AdvLockData> twsTracks = new Dictionary<Actor, AdvLockData>();

	private bool checkTwsReceived;

	private WaitForEndOfFrame eof = new WaitForEndOfFrame();

	private bool locked
	{
		get
		{
			return _locked;
		}
		set
		{
			if (_locked != value)
			{
				_locked = value;
				if (_locked)
				{
					StartCoroutine(LockedRoutine());
				}
			}
		}
	}

	public RadarLockData currentLock { get; private set; }

	public event Action OnUnlocked;

	private void Awake()
	{
		if (!isMissile && !myActor)
		{
			myActor = GetComponentInParent<Actor>();
		}
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(H_OnDeath);
		}
	}

	private void Start()
	{
		rangeSqr = maxRange * maxRange;
		if (!referenceTransform)
		{
			referenceTransform = base.transform;
		}
		if ((bool)turret)
		{
			turret.useDeltaTime = true;
		}
	}

	private void H_OnDeath()
	{
		if (disableOnDeath)
		{
			if (locked)
			{
				Unlock();
			}
			base.enabled = false;
		}
	}

	private IEnumerator LockedRoutine()
	{
		if ((bool)currentLock.actor)
		{
			StartCoroutine(NonCollisionTerrainRoutine(currentLock.actor));
		}
		while (locked)
		{
			if (Time.time - lastPingTime > pingInterval)
			{
				Actor sourceActor = null;
				if ((bool)myActor)
				{
					sourceActor = myActor;
					if ((bool)currentLock.actor)
					{
						currentLock.actor.UpdateKnownPosition(myActor);
					}
				}
				else if ((bool)currentLock.lockingRadar.radar)
				{
					sourceActor = currentLock.lockingRadar.radar.myActor;
				}
				Radar.SendRadarLockEvent(currentLock.actor, sourceActor, currentLock.radarSymbol, pingInterval, base.transform.position, transmissionStrength);
				lastPingTime = Time.time;
			}
			if (!currentLock.actor || !CheckLockAbility(currentLock.actor) || CheckChaff())
			{
				Unlock();
			}
			else if ((bool)turret)
			{
				turret.AimToTarget(currentLock.actor.transform.position);
			}
			yield return null;
		}
	}

	private IEnumerator NonCollisionTerrainRoutine(Actor lockedActor)
	{
		if (!VTCustomMapManager.instance || lockedActor.role != Actor.Roles.Air || ((bool)lockedActor.flightInfo && lockedActor.flightInfo.isLanded))
		{
			yield break;
		}
		FixedPoint currPos = new FixedPoint(referenceTransform.position);
		int checksPerFrame = 1;
		if ((bool)myActor && myActor.isPlayer)
		{
			checksPerFrame = 4;
		}
		while (locked && (bool)currentLock.actor && currentLock.actor == lockedActor)
		{
			for (int i = 0; i < checksPerFrame; i++)
			{
				currPos.point = Vector3.MoveTowards(currPos.point, lockedActor.position, 150f);
				float altitude = WaterPhysics.GetAltitude(currPos.point);
				float heightmapAltitude = VTCustomMapManager.instance.mapGenerator.GetHeightmapAltitude(currPos.point);
				if (altitude < heightmapAltitude)
				{
					Unlock();
					yield break;
				}
				if ((currPos.point - lockedActor.position).sqrMagnitude < 22500f)
				{
					currPos.point = referenceTransform.position;
				}
			}
			yield return null;
		}
	}

	private bool CheckChaff()
	{
		if (!chaffModule)
		{
			return false;
		}
		if (Radar.ADV_RADAR)
		{
			return false;
		}
		float magnitude = (currentLock.actor.position - base.transform.position).magnitude;
		return 1000f * chaffModule.GetMagnitude() / magnitude > chaffThreshold;
	}

	public bool GetLock(out RadarLockData lockData, bool missilesOnly, bool prioritizeMissiles, bool targetShips, float maxRange, List<Actor> nonTargets, List<Actor> priorityTargets)
	{
		lockData = null;
		if (locked)
		{
			return false;
		}
		if (!radar)
		{
			return false;
		}
		float num = -1f;
		if (maxRange > 0f)
		{
			num = maxRange * maxRange;
		}
		bool flag = false;
		Actor actor = null;
		bool flag2 = false;
		int count = radar.detectedUnits.Count;
		int i = 0;
		for (int num2 = UnityEngine.Random.Range(0, count); i < count; i++, num2 = (num2 + 1) % count)
		{
			Actor actor2 = radar.detectedUnits[num2];
			if (!actor2 || !actor2.gameObject.activeInHierarchy)
			{
				continue;
			}
			float sqrMagnitude = (base.transform.position - actor2.position).sqrMagnitude;
			if ((num > 0f && sqrMagnitude > num) || (actor2.finalCombatRole == Actor.Roles.Ship && !targetShips) || (actor2.finalCombatRole != Actor.Roles.Ship && targetShips))
			{
				continue;
			}
			if (actor2.finalCombatRole == Actor.Roles.Missile)
			{
				flag = true;
				Missile component = actor2.GetComponent<Missile>();
				if (component.guidanceMode != Missile.GuidanceModes.Bomb && !component.hasTarget)
				{
					continue;
				}
				bool num3 = component.guidanceMode == Missile.GuidanceModes.Heat || component.guidanceMode == Missile.GuidanceModes.Radar;
				float num4 = Vector3.Dot(actor2.velocity.normalized, (base.transform.position - actor2.position).normalized);
				if ((num3 && num4 < 0.7f) || num4 < 0.45f || sqrMagnitude > 100000000f)
				{
					continue;
				}
			}
			else if (missilesOnly || (prioritizeMissiles && flag))
			{
				continue;
			}
			if ((nonTargets == null || !nonTargets.Contains(actor2)) && CheckLockAbility(actor2))
			{
				if (priorityTargets == null || priorityTargets.Count <= 0)
				{
					actor = actor2;
					break;
				}
				if (!actor || !flag2)
				{
					actor = actor2;
					flag2 = priorityTargets.Contains(actor2);
				}
			}
		}
		if (actor != null)
		{
			lockData = new RadarLockData();
			lockData.locked = true;
			lockData.actor = actor;
			lockData.lockingRadar = this;
			lockData.radarSymbol = radar.radarSymbol;
			currentLock = lockData;
			chaffModule = lockData.actor.GetChaffModule();
			locked = true;
			return true;
		}
		return false;
	}

	public bool GetLock(int idx, out RadarLockData lockData)
	{
		lockData = null;
		if (locked)
		{
			return false;
		}
		if (!radar)
		{
			return false;
		}
		if (idx < 0 || idx >= radar.detectedUnits.Count)
		{
			return false;
		}
		Actor actor = radar.detectedUnits[idx];
		if (!actor)
		{
			return false;
		}
		if (CheckLockAbility(actor))
		{
			lockData = new RadarLockData();
			lockData.locked = true;
			lockData.actor = actor;
			lockData.lockingRadar = this;
			lockData.radarSymbol = radar.radarSymbol;
			currentLock = lockData;
			chaffModule = lockData.actor.GetChaffModule();
			locked = true;
			return true;
		}
		return false;
	}

	public bool GetLock(Actor a)
	{
		RadarLockData lockData;
		return GetLock(a, out lockData);
	}

	public bool GetLock(Actor a, out RadarLockData lockData)
	{
		lockData = null;
		if (locked)
		{
			return false;
		}
		if (!radar)
		{
			return false;
		}
		foreach (Actor detectedUnit in radar.detectedUnits)
		{
			if (detectedUnit == a)
			{
				if (CheckLockAbility(a))
				{
					lockData = new RadarLockData();
					lockData.locked = true;
					lockData.actor = a;
					lockData.lockingRadar = this;
					lockData.radarSymbol = radar.radarSymbol;
					currentLock = lockData;
					chaffModule = lockData.actor.GetChaffModule();
					locked = true;
					return true;
				}
				return false;
			}
		}
		return false;
	}

	public bool TryPitbullLockActor(Actor a, out RadarLockData lockData)
	{
		lockData = null;
		if (locked)
		{
			return false;
		}
		if (CheckLockAbility(a))
		{
			lockData = new RadarLockData();
			lockData.locked = true;
			lockData.actor = a;
			lockData.lockingRadar = this;
			lockData.radarSymbol = "M";
			currentLock = lockData;
			chaffModule = lockData.actor.GetChaffModule();
			locked = true;
			return true;
		}
		return false;
	}

	public void ForceLock(Actor a, out RadarLockData lockData)
	{
		lockData = null;
		if (locked)
		{
			if (currentLock.actor == a)
			{
				Debug.Log("Tried to force lock radar, but it was already locked on the same target.");
				lockData = currentLock;
				return;
			}
			Unlock();
		}
		if ((bool)radar)
		{
			radar.ForceDetect(a);
		}
		lockData = new RadarLockData();
		lockData.locked = true;
		lockData.actor = a;
		lockData.lockingRadar = this;
		if ((bool)radar)
		{
			lockData.radarSymbol = radar.radarSymbol;
		}
		else
		{
			lockData.radarSymbol = "?";
		}
		currentLock = lockData;
		chaffModule = lockData.actor.GetChaffModule();
		locked = true;
	}

	public bool TransferLock(RadarLockData lockData)
	{
		if (currentLock != null)
		{
			Unlock();
		}
		if (CheckLockAbility(lockData.actor))
		{
			RadarLockData radarLockData = new RadarLockData();
			radarLockData.actor = lockData.actor;
			radarLockData.locked = true;
			radarLockData.lockingRadar = this;
			radarLockData.radarSymbol = lockData.radarSymbol;
			currentLock = radarLockData;
			locked = true;
			return true;
		}
		return false;
	}

	public void Unlock()
	{
		if (locked)
		{
			if (debugRadar)
			{
				Debug.Log("Radar unlocked!", base.gameObject);
			}
			locked = false;
			if (currentLock != null)
			{
				currentLock.locked = false;
				currentLock = null;
				this.OnUnlocked?.Invoke();
			}
		}
	}

	public bool CheckLockAbility(Actor actr)
	{
		if (!actr || !actr.gameObject.activeSelf)
		{
			return false;
		}
		if ((actr.role == Actor.Roles.Ground || actr.role == Actor.Roles.GroundArmor) && !actr.alive)
		{
			return false;
		}
		Vector3 position = actr.position;
		if (!referenceTransform)
		{
			return false;
		}
		if (Radar.ADV_RADAR)
		{
			if (Vector3.Angle(position - referenceTransform.position, referenceTransform.forward) > fov / 2f)
			{
				if (debugRadar)
				{
					Debug.Log("Lock exited FOV", base.gameObject);
				}
				return false;
			}
			if (Physics.Linecast(referenceTransform.position, position, out var hitInfo, 1) && (hitInfo.point - position).sqrMagnitude > 25f)
			{
				Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
				if (!component || component.actor != actr)
				{
					if (debugRadar)
					{
						Debug.Log("Radar lock view obstructed.", base.gameObject);
					}
					return false;
				}
			}
			bool isGroundTarget = actr.role == Actor.Roles.Ground || actr.role == Actor.Roles.GroundArmor || actr.role == Actor.Roles.Ship || ((bool)actr.flightInfo && actr.flightInfo.isLanded);
			float radarSignalStrength = Radar.GetRadarSignalStrength(referenceTransform.position, actr, isGroundTarget);
			if (transmissionStrength * radarSignalStrength / (position - referenceTransform.position).sqrMagnitude < 1f / receiverSensitivity)
			{
				if (debugRadar)
				{
					Debug.Log("Radar lock return below sensitivity threshold.", base.gameObject);
				}
				return false;
			}
			return true;
		}
		if ((position - base.transform.position).sqrMagnitude > rangeSqr)
		{
			if (debugRadar)
			{
				Debug.Log("Radar lock out of range", base.gameObject);
			}
			return false;
		}
		if (Vector3.Angle(position - referenceTransform.position, referenceTransform.forward) > fov / 2f)
		{
			if (debugRadar)
			{
				Debug.Log("Lock exited FOV", base.gameObject);
			}
			return false;
		}
		if (Physics.Linecast(referenceTransform.position, position, out var hitInfo2, 1) && (hitInfo2.point - position).sqrMagnitude > 25f)
		{
			if (debugRadar)
			{
				Debug.Log("Radar lock view obstructed.", base.gameObject);
			}
			return false;
		}
		return true;
	}

	public bool IsLocked()
	{
		if (locked)
		{
			return currentLock.actor;
		}
		return false;
	}

	private void OnDisable()
	{
		Unlock();
	}

	public void UpdateTWSLock(Actor actor)
	{
		if (twsTracks.TryGetValue(actor, out var value))
		{
			value.UpdatePosition(actor.position);
			value.velocity = actor.velocity;
			value.lockStatus = AdvLockData.LockStatus.Tracked;
			actor.UpdateKnownPosition(myActor);
		}
		else
		{
			AdvLockData value2 = new AdvLockData(AdvLockData.LockStatus.Tracked, actor.position, actor.velocity, actor, this);
			twsTracks.Add(actor, value2);
		}
	}

	public void UpdateTWSLock(Actor actor, Vector3 position, Vector3 velocity)
	{
		if (twsTracks.TryGetValue(actor, out var value))
		{
			value.UpdatePosition(position);
			value.velocity = velocity;
			value.lockStatus = AdvLockData.LockStatus.Tracked;
		}
		else
		{
			AdvLockData value2 = new AdvLockData(AdvLockData.LockStatus.Tracked, position, velocity, actor, this);
			twsTracks.Add(actor, value2);
		}
	}

	public void ForceOcclude(Actor actor)
	{
		twsTracks.Remove(actor);
		if (IsLocked() && currentLock.actor == actor)
		{
			Unlock();
		}
	}

	public void RemoveTWSLock(Actor actor)
	{
		twsTracks.Remove(actor);
	}

	public AdvLockData GetTWSLockUpdate(RadarLockData lockData)
	{
		if (twsTracks.TryGetValue(lockData.actor, out var value))
		{
			if (!checkTwsReceived)
			{
				checkTwsReceived = true;
				StartCoroutine(SetReceivedStandbyRtn());
			}
			return value;
		}
		return null;
	}

	private IEnumerator SetReceivedStandbyRtn()
	{
		yield return eof;
		checkTwsReceived = false;
		foreach (AdvLockData value in twsTracks.Values)
		{
			if (value.lockStatus == AdvLockData.LockStatus.Received)
			{
				value.lockStatus = AdvLockData.LockStatus.Standby;
			}
		}
	}

	public AdvLockData GetTWSLockUpdate(Actor a)
	{
		if (!a)
		{
			return null;
		}
		if (twsTracks.TryGetValue(a, out var value))
		{
			return value;
		}
		return null;
	}
}
