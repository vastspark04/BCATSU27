using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class SAMLauncher : MonoBehaviour, IEngageEnemies, IQSVehicleComponent, ITargetPreferences
{
	public LockingRadar[] lockingRadars;

	private LockingRadar currentLockingRadar;

	public GameObject missilePrefab;

	public string missileResourcePath;

	public bool useEdgeTf;

	public Transform[] fireTransforms;

	private Missile[] _missiles;

	private bool init_missiles;

	public Transform fireSafetyReferenceTf;

	public float fireInterval;

	private float lastFireTime;

	public bool allowReload;

	public float reloadTime = 60f;

	public ModuleTurret turret;

	public float minLaunchRange = 100f;

	private float maxLaunchRange = -1f;

	public float holdLockBeforeLaunch = 1f;

	public float holdLockAfterLaunch = 1f;

	public VerticalLauncher[] vLaunchers;

	private RadarLockData lockData;

	private bool readyToFire = true;

	private Missile firedMissile;

	private bool alive = true;

	public Actor actor;

	public bool engageEnemies = true;

	public bool weaponEnabled = true;

	public bool targetMissilesOnly;

	public bool prioritizeMissiles;

	public bool targetShips;

	public bool onlySpawnOnLaunch;

	public bool debugMissile;

	private bool quickloaded;

	private bool mpRemote;

	private bool reloading;

	private float reloadStartTime;

	private List<Actor> nonTargets = new List<Actor>();

	private List<Actor> priorityTargets = new List<Actor>();

	public Missile[] missiles
	{
		get
		{
			if (!init_missiles)
			{
				init_missiles = true;
				_missiles = new Missile[fireTransforms.Length];
				missileCount = 0;
			}
			return _missiles;
		}
	}

	public int missileCount { get; private set; }

	public float targetingSimSpeed { get; private set; }

	public bool engagingTarget { get; private set; }

	public Actor engagedTarget { get; private set; }

	public event Action<Actor> OnTurretEngageActor;

	public event Action<int> OnFiringMissileIdx;

	public event Action<Missile> OnFiredMissile;

	public event Action OnWillReloadMissiles;

	public void SetToMPRemote()
	{
		mpRemote = true;
	}

	private void Awake()
	{
		if (!fireSafetyReferenceTf)
		{
			fireSafetyReferenceTf = fireTransforms[0];
		}
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(H_OnDeath);
		}
	}

	private void Start()
	{
		if (!actor)
		{
			actor = GetComponentInParent<Actor>();
		}
		if ((bool)missilePrefab && !quickloaded && !VTOLMPUtils.IsMultiplayer())
		{
			LoadAllMissiles();
		}
		lastFireTime = Time.time - UnityEngine.Random.Range(0f, fireInterval);
		engagingTarget = false;
		engagedTarget = null;
	}

	public void LoadAllMissiles()
	{
		for (int i = 0; i < missiles.Length; i++)
		{
			LoadMissile(i);
		}
	}

	private void LoadMissiles(int count)
	{
		for (int i = 0; i < missiles.Length && i < count; i++)
		{
			LoadMissile(i);
		}
	}

	public void RemoveAllMissiles()
	{
		if (missiles != null)
		{
			for (int i = 0; i < missiles.Length; i++)
			{
				if (missiles[i] != null)
				{
					UnityEngine.Object.Destroy(missiles[i].gameObject);
					missiles[i] = null;
				}
			}
		}
		missileCount = 0;
	}

	[ContextMenu("Reload Missiles")]
	public void ReloadAllMissilesNow()
	{
		RemoveAllMissiles();
		LoadAllMissiles();
	}

	private void H_OnDeath()
	{
		alive = false;
	}

	private void OnDrawGizmos()
	{
		if (lockingRadars == null || lockingRadars.Length == 0)
		{
			return;
		}
		Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
		for (int i = 0; i < lockingRadars.Length; i++)
		{
			if ((bool)lockingRadars[i])
			{
				Gizmos.DrawLine(base.transform.position, lockingRadars[i].transform.position);
			}
		}
	}

	private void OnEnable()
	{
		if (!mpRemote)
		{
			StartCoroutine(UpdateRoutine());
		}
	}

	private IEnumerator UpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(fireInterval);
		while (base.enabled && alive && !mpRemote)
		{
			if (engageEnemies && weaponEnabled && !firedMissile && missileCount > 0 && readyToFire)
			{
				lastFireTime = Time.time;
				if (GetLock(out lockData))
				{
					yield return StartCoroutine(FireRoutine());
				}
			}
			yield return wait;
		}
	}

	private bool GetLock(out RadarLockData lockData)
	{
		if (lockingRadars != null && lockingRadars.Length != 0)
		{
			for (int i = 0; i < lockingRadars.Length; i++)
			{
				if ((bool)lockingRadars[i] && !lockingRadars[i].IsLocked() && lockingRadars[i].GetLock(out lockData, targetMissilesOnly, prioritizeMissiles, targetShips, maxLaunchRange, nonTargets, priorityTargets))
				{
					currentLockingRadar = lockingRadars[i];
					return true;
				}
			}
		}
		currentLockingRadar = null;
		lockData = new RadarLockData();
		return false;
	}

	private IEnumerator FireRoutine()
	{
		readyToFire = false;
		engagingTarget = true;
		engagedTarget = lockData.actor;
		float t2 = Time.time;
		bool hasFireSolution = false;
		WaitForFixedUpdate waitFixedUpdate = new WaitForFixedUpdate();
		Vector3 fireSolution;
		if ((bool)turret)
		{
			this.OnTurretEngageActor?.Invoke(lockData.actor);
			while (lockData.locked && alive && weaponEnabled)
			{
				bool airToAirFireSolution;
				hasFireSolution = (airToAirFireSolution = GetAirToAirFireSolution(fireTransforms[0].position, actor.velocity, targetingSimSpeed, lockData.actor, turret, fireSafetyReferenceTf, out fireSolution));
				if (airToAirFireSolution)
				{
					turret.AimToTarget(fireSolution);
					if (Vector3.Dot(turret.pitchTransform.forward, (fireSolution - turret.pitchTransform.position).normalized) > 0.99f)
					{
						break;
					}
					yield return waitFixedUpdate;
					continue;
				}
				break;
			}
		}
		while (lockData.locked && Time.time - t2 < holdLockBeforeLaunch && alive && weaponEnabled)
		{
			bool airToAirFireSolution;
			hasFireSolution = (airToAirFireSolution = GetAirToAirFireSolution(fireTransforms[0].position, actor.velocity, targetingSimSpeed, lockData.actor, turret, fireSafetyReferenceTf, out fireSolution));
			if (!airToAirFireSolution)
			{
				break;
			}
			if ((bool)turret)
			{
				turret.AimToTarget(fireSolution);
			}
			yield return waitFixedUpdate;
		}
		if (lockData.locked && (bool)lockData.actor)
		{
			float num = Radar.EstimateDetectionDistance(30f, lockData.lockingRadar.transmissionStrength, lockData.lockingRadar.receiverSensitivity);
			float num2 = Mathf.Min(maxLaunchRange, num);
			if (maxLaunchRange < 0f)
			{
				num2 = num;
			}
			float num3 = Vector3.Distance(lockData.actor.transform.position, base.transform.position);
			if (!alive || !weaponEnabled || !hasFireSolution)
			{
				if ((bool)currentLockingRadar)
				{
					currentLockingRadar.Unlock();
				}
			}
			else if (num3 > minLaunchRange && num3 < num2)
			{
				FireMissile(lockData);
				t2 = Time.time;
				while (alive && weaponEnabled && lockData.locked && Time.time - t2 < holdLockAfterLaunch)
				{
					yield return null;
				}
				while (alive && (bool)firedMissile && !firedMissile.isPitbull)
				{
					yield return null;
				}
				if ((bool)currentLockingRadar)
				{
					currentLockingRadar.Unlock();
				}
			}
			else if ((bool)currentLockingRadar)
			{
				currentLockingRadar.Unlock();
			}
		}
		if (alive)
		{
			engagingTarget = false;
			engagedTarget = null;
			readyToFire = true;
			if ((bool)turret)
			{
				turret.ReturnTurretOneshot();
			}
			lastFireTime = Time.time;
		}
		else
		{
			engagingTarget = false;
			engagedTarget = null;
			if ((bool)currentLockingRadar)
			{
				currentLockingRadar.Unlock();
			}
			readyToFire = false;
		}
		if ((bool)turret)
		{
			this.OnTurretEngageActor?.Invoke(null);
		}
	}

	private void OnDisable()
	{
		if (alive)
		{
			readyToFire = true;
			engagedTarget = null;
			if ((bool)currentLockingRadar)
			{
				currentLockingRadar.Unlock();
			}
		}
		engagingTarget = false;
	}

	public void FireMissile(RadarLockData lockData)
	{
		StartCoroutine(FireMissileRoutine(lockData));
	}

	private IEnumerator FireMissileRoutine(RadarLockData lockData)
	{
		VerticalLauncher vls = null;
		WaitForSeconds waitForSeconds = new WaitForSeconds(1f);
		for (int i = 0; i < missiles.Length; i++)
		{
			if (!(missiles[i] != null))
			{
				continue;
			}
			this.OnFiringMissileIdx?.Invoke(i);
			firedMissile = missiles[i];
			if (onlySpawnOnLaunch)
			{
				firedMissile.gameObject.SetActive(value: true);
				if ((bool)firedMissile.hiddenMissileObject)
				{
					firedMissile.hiddenMissileObject.SetActive(value: true);
				}
			}
			VerticalLauncher[] array = vLaunchers;
			foreach (VerticalLauncher verticalLauncher in array)
			{
				if (verticalLauncher.EnableLauncher(fireTransforms[i]))
				{
					vls = verticalLauncher;
					yield return waitForSeconds;
					break;
				}
			}
			if (debugMissile)
			{
				firedMissile.debugMissile = true;
			}
			if ((bool)actor)
			{
				Actor obj = firedMissile.gameObject.AddComponent<Actor>();
				obj.role = Actor.Roles.Missile;
				obj.SetMissile(firedMissile);
				obj.team = actor.team;
				obj.drawIcon = false;
				obj.actorName = $"{missiles[i].gameObject.name} ({actor.actorName})";
				string unitName = UIUtils.GetUnitName(lockData.actor);
				string unitName2 = UIUtils.GetUnitName(actor);
				Debug.Log(unitName2 + " is firing " + firedMissile.gameObject.name + " at " + unitName);
			}
			else
			{
				Debug.LogError("SAM launcher does not have an actor! " + base.gameObject.name);
			}
			if (firedMissile.guidanceMode == Missile.GuidanceModes.Radar)
			{
				firedMissile.SetRadarLock(lockData);
			}
			else if (firedMissile.guidanceMode == Missile.GuidanceModes.GPS)
			{
				GPSTarget gPSTarget = new GPSTarget(lockData.actor.position, "SAM", 0);
				firedMissile.SetGPSTarget(gPSTarget);
				AntiShipGuidance component = firedMissile.GetComponent<AntiShipGuidance>();
				if ((bool)component)
				{
					GPSTargetGroup gPSTargetGroup = new GPSTargetGroup("ASM", 1);
					gPSTargetGroup.AddTarget(gPSTarget);
					component.SetTarget(gPSTargetGroup);
				}
			}
			else if (firedMissile.guidanceMode == Missile.GuidanceModes.Optical)
			{
				firedMissile.SetOpticalTarget(lockData.actor.transform, lockData.actor);
			}
			firedMissile.launchedByActor = actor;
			firedMissile.Fire();
			this.OnFiredMissile?.Invoke(firedMissile);
			if ((bool)vls)
			{
				vls.FireMissile();
			}
			missiles[i] = null;
			missileCount--;
			break;
		}
		if (debugMissile && missileCount == 0)
		{
			LoadAllMissiles();
		}
		if (missileCount == 0 && allowReload)
		{
			StartCoroutine(ReloadRoutine());
		}
		if ((bool)vls)
		{
			yield return new WaitForSeconds(1.5f);
			vls.DisableLauncher();
		}
	}

	public void MP_RemoteFireMissile(int i)
	{
		StartCoroutine(MP_RemoteFireMissileRoutine(i));
	}

	private IEnumerator MP_RemoteFireMissileRoutine(int i)
	{
		VerticalLauncher vls = null;
		WaitForSeconds waitForSeconds = new WaitForSeconds(1f);
		if (missiles[i] != null)
		{
			this.OnFiringMissileIdx?.Invoke(i);
			firedMissile = missiles[i];
			if (onlySpawnOnLaunch)
			{
				firedMissile.gameObject.SetActive(value: true);
				if ((bool)firedMissile.hiddenMissileObject)
				{
					firedMissile.hiddenMissileObject.SetActive(value: true);
				}
			}
			VerticalLauncher[] array = vLaunchers;
			foreach (VerticalLauncher verticalLauncher in array)
			{
				if (verticalLauncher.EnableLauncher(fireTransforms[i]))
				{
					vls = verticalLauncher;
					yield return waitForSeconds;
					break;
				}
			}
			if (debugMissile)
			{
				firedMissile.debugMissile = true;
			}
			firedMissile.GetComponent<MissileSync>().Client_FireMissile();
			if ((bool)vls)
			{
				vls.FireMissile();
			}
			missiles[i] = null;
			missileCount--;
		}
		if ((bool)vls)
		{
			yield return new WaitForSeconds(1.5f);
			vls.DisableLauncher();
		}
	}

	private IEnumerator ReloadRoutine(float elapsed = 0f)
	{
		if (!reloading)
		{
			reloading = true;
			reloadStartTime = Time.time - elapsed;
			while (Time.time - reloadStartTime < reloadTime)
			{
				yield return null;
			}
			this.OnWillReloadMissiles?.Invoke();
			if (!VTOLMPUtils.IsMultiplayer())
			{
				LoadAllMissiles();
			}
			reloading = false;
		}
	}

	public void LoadMissile()
	{
		for (int i = 0; i < missiles.Length; i++)
		{
			if (missiles[i] == null)
			{
				LoadMissile(i);
				break;
			}
		}
	}

	public void LoadMissile(int idx)
	{
		if (missiles[idx] == null)
		{
			missiles[idx] = MissileLauncher.LoadMissile(missilePrefab, fireTransforms[idx], useEdgeTf, onlySpawnOnLaunch);
			missiles[idx].SetLowPoly();
			missileCount++;
			targetingSimSpeed = missiles[idx].maxTorqueSpeed;
			SAMLaunchRanges component = missiles[idx].GetComponent<SAMLaunchRanges>();
			if ((bool)component)
			{
				minLaunchRange = component.minLaunchRange;
				maxLaunchRange = component.maxLaunchRange;
			}
		}
	}

	public void LoadMissile(Missile m, int idx)
	{
		if (missiles[idx] != null)
		{
			Debug.LogError($"{actor.actorName} tried to load a missile into #{idx} but there was already a missile there.  Destroying it.");
			VTNetworkManager.NetDestroyObject(missiles[idx].gameObject);
			missileCount--;
		}
		missiles[idx] = m;
		missiles[idx].SetLowPoly();
		missileCount++;
		targetingSimSpeed = missiles[idx].maxTorqueSpeed;
		SAMLaunchRanges component = missiles[idx].GetComponent<SAMLaunchRanges>();
		if ((bool)component)
		{
			minLaunchRange = component.minLaunchRange;
			maxLaunchRange = component.maxLaunchRange;
		}
	}

	public bool GetLockedFireSolution(out Vector3 fireSolution)
	{
		if (!lockData.locked)
		{
			fireSolution = Vector3.zero;
			return false;
		}
		return GetAirToAirFireSolution(fireTransforms[0].position, actor.velocity, targetingSimSpeed, lockData.actor, turret, fireSafetyReferenceTf, out fireSolution);
	}

	public static bool GetAirToAirFireSolution(Vector3 launcherPos, Vector3 launcherVelocity, float simSpeed, Actor targetVessel, ModuleTurret turret, Transform fireSafetyTf, out Vector3 fireSolution)
	{
		if (!targetVessel)
		{
			fireSolution = Vector3.zero;
			return false;
		}
		Vector3 position = targetVessel.transform.position;
		float num = 0f;
		float num2 = Vector3.Distance(targetVessel.transform.position, launcherPos);
		Vector3 vector = simSpeed * (position - launcherPos).normalized;
		vector += launcherVelocity;
		num = num2 / (targetVessel.velocity - vector).magnitude;
		num = Mathf.Clamp(num, 0f, 8f);
		position += targetVessel.velocity * num;
		position = Missile.BallisticPoint(position, launcherPos, (vector / 2f).magnitude);
		if ((bool)turret)
		{
			for (float num3 = Vector3.Angle(position - launcherPos, Vector3.ProjectOnPlane(position - launcherPos, Vector3.up)); num3 < turret.maxPitch; num3 += 5f)
			{
				if (Physics.Linecast(position, launcherPos, 1))
				{
					Vector3 vector2 = position - launcherPos;
					vector2 = Quaternion.AngleAxis(-5f, Vector3.Cross(Vector3.up, vector2)) * vector2;
					position = launcherPos + vector2;
					continue;
				}
				fireSolution = position;
				return true;
			}
		}
		else if ((bool)fireSafetyTf && !Physics.Linecast(position, fireSafetyTf.position, 1))
		{
			fireSolution = position;
			return true;
		}
		fireSolution = position;
		return false;
	}

	public void SetEngageEnemies(bool engage)
	{
		engageEnemies = engage;
		if (engage)
		{
			return;
		}
		LockingRadar[] array = lockingRadars;
		foreach (LockingRadar lockingRadar in array)
		{
			if (lockingRadar.IsLocked())
			{
				lockingRadar.Unlock();
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("SAMLauncher_" + base.gameObject.name);
		configNode.SetValue("missileCount", missileCount);
		configNode.SetValue("reloading", reloading);
		if (reloading)
		{
			configNode.SetValue("reloadElapsedTime", Time.time - reloadStartTime);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("SAMLauncher_" + base.gameObject.name);
		if (node != null)
		{
			int value = node.GetValue<int>("missileCount");
			RemoveAllMissiles();
			LoadMissiles(value);
			if (node.GetValue<bool>("reloading"))
			{
				float value2 = node.GetValue<float>("reloadElapsedTime");
				StartCoroutine(ReloadRoutine(value2));
			}
			quickloaded = true;
		}
	}

	public void SetNonTargets(UnitReferenceList list)
	{
		nonTargets.Clear();
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				nonTargets.Add(unit.GetActor());
			}
		}
	}

	public void AddNonTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				nonTargets.Add(unit.GetActor());
			}
		}
	}

	public void RemoveNonTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				nonTargets.Remove(unit.GetActor());
			}
		}
	}

	public void ClearNonTargets()
	{
		nonTargets.Clear();
	}

	public void SetPriorityTargets(UnitReferenceList list)
	{
		priorityTargets.Clear();
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
	}

	public void AddPriorityTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Add(unit.GetActor());
			}
		}
	}

	public void RemovePriorityTargets(UnitReferenceList list)
	{
		foreach (UnitReference unit in list.units)
		{
			if ((bool)unit.GetActor())
			{
				priorityTargets.Remove(unit.GetActor());
			}
		}
	}

	public void ClearPriorityTargets()
	{
		priorityTargets.Clear();
	}
}
