using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class Soldier : MonoBehaviour, IRescuable, IPickupableUnit, IGroundColumnUnit, IEngageEnemies, IQSVehicleComponent, IOptionalStopToEngage
{
	public enum SoldierTypes
	{
		Standard,
		IRMANPAD
	}

	public class DismountWaitForActive : MonoBehaviour
	{
		private Soldier soldier;

		private Transform unloadRallyTf;

		public void BeginWait(Soldier soldier, Transform unloadRallyTf)
		{
			this.soldier = soldier;
			this.unloadRallyTf = unloadRallyTf;
			StartCoroutine(WaitRoutine());
		}

		private IEnumerator WaitRoutine()
		{
			while (!soldier.loadedInAIBay.pilot.autoPilot.flightInfo.isLanded)
			{
				yield return null;
			}
			soldier.gameObject.SetActive(value: true);
			soldier.DismountAIBay(unloadRallyTf);
		}
	}

	public SoldierTypes soldierType;

	public GroundUnitMover mover;

	public bool attackEnemies = true;

	public bool waitingForPickup;

	[Tooltip("Gross mass of soldier in tons, so it weighs down passenger carrier vehicle")]
	public float soldierMass = 0.09f;

	public Transform modelTransform;

	public bool stopToEngage;

	public Transform armTransform;

	public VisualTargetFinder targetFinder;

	private Actor attackingTarget;

	private Coroutine attackRoutine;

	private Health health;

	private Coroutine pickupRoutine;

	private bool _gotActor;

	private Actor _a;

	[Header("Standard Gun")]
	public AudioSource audioSource;

	public AudioClip fireSound;

	public Transform fireTransform;

	public float burstCount;

	public float burstInterval;

	public float rpm;

	public float bulletSpeed;

	public float damage;

	private Light muzzleLight;

	private ParticleSystem muzzleParticle;

	private bool alive = true;

	private GroundUnitSeparator softCollider;

	private GroundUnitColumn column;

	[Header("IRMANPAD")]
	public IRMissileLauncher irMissileLauncher;

	public float irLockHoldTime = 2f;

	public float irMLReloadTime = 20f;

	private bool reloading;

	[Header("Runtime stuff")]
	public List<UnloadingZone> targetUnloadZones = new List<UnloadingZone>();

	public bool isLoadedInBay;

	public static List<Soldier> soldiersForPickup = new List<Soldier>();

	private bool isRemote;

	private Actor _aat;

	private bool boardingAIBay;

	private AIPassengerBay boardingAIBayTarget;

	private AIPassengerBay loadedInAIBay;

	private bool dismountingAIBay;

	private Transform dismountingAIBayRallyTf;

	private GameObject dismountBayRoutineObj;

	private Coroutine dismountAiBayRoutine;

	public Gun gun;

	private FixedPoint loadedInBayPoint;

	private PassengerBay passengerBay;

	private bool rescued;

	private bool wasPickedUp;

	public Actor actor
	{
		get
		{
			if (!_gotActor)
			{
				_a = GetComponent<Actor>();
				_gotActor = true;
			}
			return _a;
		}
	}

	public float aimPitch { get; private set; }

	public bool isAiming { get; set; }

	public bool isLoadedInAIBay => loadedInAIBay != null;

	public event Action OnWillReloadManpad;

	public event Action<Vector3> OnManpadAiming;

	public event Action OnManpadStopAiming;

	public event Action<Actor> OnAimingAtTarget;

	public event Action OnPassengerBayDiedWhileInvincible;

	public void SetStopToEngage(bool s)
	{
		stopToEngage = s;
	}

	public void SetToRemote()
	{
		isRemote = true;
	}

	private void Awake()
	{
		health = GetComponent<Health>();
		health.OnDeath.AddListener(OnDeath);
	}

	private void Start()
	{
		mover = GetComponent<GroundUnitMover>();
		if ((bool)fireTransform)
		{
			muzzleLight = fireTransform.GetComponentInChildren<Light>();
			muzzleParticle = fireTransform.GetComponentInChildren<ParticleSystem>();
		}
		softCollider = GetComponentInChildren<GroundUnitSeparator>();
		if (waitingForPickup)
		{
			StartWaitingForPickup();
		}
	}

	public void StartWaitingForPickup()
	{
		if (!isLoadedInBay)
		{
			if (soldiersForPickup == null)
			{
				soldiersForPickup = new List<Soldier>();
			}
			if (!soldiersForPickup.Contains(this))
			{
				soldiersForPickup.Add(this);
			}
			waitingForPickup = true;
			wasPickedUp = false;
			rescued = false;
		}
	}

	public void StopWaitingForPickup()
	{
		if (soldiersForPickup != null)
		{
			soldiersForPickup.Remove(this);
		}
		waitingForPickup = false;
	}

	private void OnDestroy()
	{
		StopWaitingForPickup();
		if ((bool)dismountBayRoutineObj)
		{
			UnityEngine.Object.Destroy(dismountBayRoutineObj);
		}
	}

	private void OnDeath()
	{
		mover.move = false;
		alive = false;
		if ((bool)gun)
		{
			gun.SetFire(fire: false);
		}
		Hitbox[] componentsInChildren = GetComponentsInChildren<Hitbox>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].GetComponent<Collider>().enabled = false;
		}
		if (waitingForPickup)
		{
			StopWaitingForPickup();
		}
		if (attackRoutine != null)
		{
			StopCoroutine(attackRoutine);
			attackRoutine = null;
		}
		if ((bool)irMissileLauncher && (bool)irMissileLauncher.GetNextMissile())
		{
			irMissileLauncher.GetNextMissile().heatSeeker.enabled = false;
		}
	}

	private void Update()
	{
		if (!alive || isRemote)
		{
			return;
		}
		if (!isLoadedInBay)
		{
			PassengerBay passengerBay = null;
			if (waitingForPickup)
			{
				passengerBay = CheckIsWithinPickupRadius(1f);
			}
			if (attackEnemies && !passengerBay)
			{
				attackingTarget = targetFinder.attackingTarget;
			}
			else
			{
				attackingTarget = null;
			}
			if ((bool)attackingTarget && !reloading && stopToEngage)
			{
				mover.move = false;
			}
			else
			{
				mover.move = true;
			}
			if ((bool)column)
			{
				mover.move = column.canMove;
			}
			if ((bool)passengerBay)
			{
				GoToPickupShip(passengerBay);
			}
			if ((bool)attackingTarget && !reloading)
			{
				if (soldierType == SoldierTypes.Standard)
				{
					RecordGunTarget(attackingTarget);
					AimAtTarget(attackingTarget.position);
					isAiming = true;
					if (attackRoutine == null)
					{
						attackRoutine = StartCoroutine(AttackTargetRoutine());
					}
				}
				else if (attackRoutine == null)
				{
					attackRoutine = StartCoroutine(AttackTargetIRMANPADRoutine());
				}
			}
			else
			{
				RecordGunTarget(null);
				isAiming = false;
				LookToVelocity();
			}
		}
		else if (!this.passengerBay)
		{
			Debug.Log("Soldier was loaded in a bay but the bay no longer exists.");
			OnPassengerBayDied();
		}
	}

	private void RecordGunTarget(Actor a)
	{
		if (_aat != a)
		{
			_aat = a;
			this.OnAimingAtTarget?.Invoke(a);
		}
	}

	public void BoardAIBay(AIPassengerBay bay)
	{
		if (!boardingAIBay)
		{
			StartCoroutine(BoardAIBayRoutine(bay));
		}
	}

	public void BoardAIBayImmediate(AIPassengerBay bay)
	{
		bay.LoadSoldier(this);
		loadedInAIBay = bay;
	}

	private IEnumerator BoardAIBayRoutine(AIPassengerBay bay)
	{
		boardingAIBayTarget = bay;
		boardingAIBay = true;
		bay.ExpectPickupUnit(this);
		float sqrRad = bay.loadRadius * bay.loadRadius;
		Func<bool> unitCanBoard = () => bay.pilot.actor.alive && alive && !bay.IsFull();
		while (unitCanBoard() && (base.transform.position - bay.transform.position).sqrMagnitude > sqrRad)
		{
			if (bay.pilot.autoPilot.flightInfo.isLanded)
			{
				mover.move = true;
				mover.behavior = GroundUnitMover.Behaviors.StayInRadius;
				mover.parkWhenInRallyRadius = false;
				mover.rallyTransform = bay.transform;
				mover.rallyRadius = 0.5f;
			}
			else
			{
				mover.move = false;
			}
			yield return null;
		}
		boardingAIBay = false;
		boardingAIBayTarget = null;
		if (unitCanBoard())
		{
			bay.LoadSoldier(this);
			loadedInAIBay = bay;
		}
	}

	public void DismountAIBay(Transform unloadRallyTf)
	{
		if (!loadedInAIBay)
		{
			return;
		}
		dismountingAIBay = true;
		dismountingAIBayRallyTf = unloadRallyTf;
		if ((bool)dismountBayRoutineObj)
		{
			UnityEngine.Object.Destroy(dismountBayRoutineObj);
		}
		if (base.gameObject.activeInHierarchy)
		{
			if (dismountAiBayRoutine != null)
			{
				StopCoroutine(dismountAiBayRoutine);
			}
			dismountAiBayRoutine = StartCoroutine(DismountAIBayRoutine(unloadRallyTf));
		}
		else
		{
			dismountBayRoutineObj = new GameObject(base.gameObject.name + " dismount wait");
			dismountBayRoutineObj.AddComponent<DismountWaitForActive>().BeginWait(this, unloadRallyTf);
		}
	}

	private IEnumerator DismountAIBayRoutine(Transform unloadRallyTf)
	{
		AIPilot pilot = loadedInAIBay.pilot;
		while ((bool)loadedInAIBay && pilot.actor.alive && !pilot.autoPilot.flightInfo.isLanded && pilot.autoPilot.targetSpeed < 1f && pilot.autoPilot.flightInfo.surfaceSpeed < 1f)
		{
			yield return null;
		}
		if ((bool)loadedInAIBay && loadedInAIBay.pilot.actor.alive && loadedInAIBay.pilot.autoPilot.flightInfo.isLanded)
		{
			loadedInAIBay.UnloadSoldier(this, unloadRallyTf.position);
			loadedInAIBay = null;
			dismountingAIBay = false;
			dismountingAIBayRallyTf = null;
			if (!mover.squad)
			{
				mover.behavior = GroundUnitMover.Behaviors.StayInRadius;
				mover.rallyTransform = unloadRallyTf;
				mover.move = true;
			}
		}
	}

	public void DismountAIBayImmediate(Transform unloadRallyTf)
	{
		if ((bool)loadedInAIBay && loadedInAIBay.pilot.actor.alive && loadedInAIBay.pilot.autoPilot.flightInfo.isLanded)
		{
			loadedInAIBay.UnloadSoldier(this, unloadRallyTf.position);
			loadedInAIBay = null;
			if (!mover.squad)
			{
				mover.behavior = GroundUnitMover.Behaviors.StayInRadius;
				mover.rallyTransform = unloadRallyTf;
				mover.move = true;
			}
		}
	}

	private void GoToPickupShip(PassengerBay bay)
	{
		if ((bool)attackingTarget && stopToEngage)
		{
			attackingTarget = null;
			if (attackRoutine != null)
			{
				StopCoroutine(attackRoutine);
				attackRoutine = null;
			}
		}
		if (pickupRoutine == null)
		{
			if (mover.move)
			{
				mover.FullStop();
			}
			if ((bool)mover.squad)
			{
				mover.squad.UnregisterUnit(mover);
			}
			pickupRoutine = StartCoroutine(PickupRoutine(bay));
		}
	}

	private IEnumerator PickupRoutine(PassengerBay bay)
	{
		mover.squad = null;
		FollowPath fp = bay.GetPickupPath(base.transform.position);
		mover.path = fp;
		mover.behavior = GroundUnitMover.Behaviors.Path;
		mover.rallyRadius = 0.01f;
		mover.move = true;
		softCollider.ignoreShip = true;
		while ((bool)CheckIsWithinPickupRadius(1.25f) && waitingForPickup)
		{
			if (mover.behavior == GroundUnitMover.Behaviors.Parked)
			{
				mover.behavior = GroundUnitMover.Behaviors.Path;
			}
			yield return null;
		}
		if (waitingForPickup)
		{
			mover.rallyTransform = fp.pointTransforms[0];
			mover.behavior = GroundUnitMover.Behaviors.StayInRadius;
			softCollider.ignoreShip = false;
		}
		pickupRoutine = null;
	}

	private PassengerBay CheckIsWithinPickupRadius(float radiusFactor)
	{
		foreach (PassengerBay passengerBay in PassengerBay.passengerBays)
		{
			if ((bool)passengerBay && passengerBay.rampState == PassengerBay.RampStates.Open && passengerBay.actor.team == actor.team && passengerBay.flightInfo.isLanded && passengerBay.flightInfo.surfaceSpeed < 0.1f && (passengerBay.transform.position - base.transform.position).sqrMagnitude < radiusFactor * radiusFactor * passengerBay.soldierPickupRadius * passengerBay.soldierPickupRadius)
			{
				return passengerBay;
			}
		}
		return null;
	}

	private IEnumerator ReloadIRMLRoutine()
	{
		reloading = true;
		yield return new WaitForSeconds(irMLReloadTime);
		this.OnWillReloadManpad?.Invoke();
		if (!VTOLMPUtils.IsMultiplayer())
		{
			irMissileLauncher.LoadAllMissiles();
		}
		reloading = false;
	}

	private float GetMissileSimSpeed(float dist)
	{
		float num = 0f;
		float num2 = 0.5f;
		float num3 = 0f;
		float num4 = 0f;
		Missile nextMissile = irMissileLauncher.GetNextMissile();
		SimpleDrag component = nextMissile.GetComponent<SimpleDrag>();
		while (num3 < dist)
		{
			float num5 = 0f;
			if (num4 < nextMissile.boostTime)
			{
				num5 = nextMissile.boostThrust;
			}
			else if (num4 < nextMissile.boostTime + nextMissile.cruiseTime)
			{
				num5 = nextMissile.cruiseThrust;
			}
			num5 -= component.CalculateDragForceMagnitudeAtSeaLevel(num);
			num += num5 / nextMissile.mass * num2;
			num3 += num * num2;
			num4 += num2;
		}
		return dist / num4;
	}

	private IEnumerator AttackTargetIRMANPADRoutine()
	{
		_ = Time.time;
		if (!reloading && irMissileLauncher.missileCount == 0)
		{
			StartCoroutine(ReloadIRMLRoutine());
		}
		while (reloading)
		{
			isAiming = false;
			LookToVelocity();
			yield return null;
		}
		if (!alive)
		{
			yield break;
		}
		if ((bool)attackingTarget)
		{
			float lockHoldTime = 2f;
			HeatSeeker seeker = irMissileLauncher.GetNextMissile().heatSeeker;
			Vector3 vector = attackingTarget.position + attackingTarget.velocity * lockHoldTime;
			float simSpeed = GetMissileSimSpeed((vector - base.transform.position).magnitude);
			seeker.headTransform = irMissileLauncher.headTransform;
			seeker.SetSeekerMode(HeatSeeker.SeekerModes.HeadTrack);
			irMissileLauncher.EnableWeapon();
			float t2 = Time.time;
			while (alive && (bool)attackingTarget && (!IRTargetLocked(attackingTarget, seeker) || Time.time - t2 < lockHoldTime))
			{
				float num = VectorUtils.CalculateLeadTime(attackingTarget.position - base.transform.position, attackingTarget.velocity, simSpeed);
				float num2 = Mathf.Clamp01((Time.time - t2) / lockHoldTime);
				Vector3 target = attackingTarget.position + num * num2 * attackingTarget.velocity - seeker.transform.position;
				target = Vector3.RotateTowards(attackingTarget.position - seeker.transform.position, target, seeker.gimbalFOV * 0.45f * ((float)Math.PI / 180f), 0f);
				Vector3 vector2 = seeker.transform.position + target;
				AimAtTarget(vector2);
				isAiming = true;
				this.OnManpadAiming?.Invoke(vector2);
				Vector3 vector3 = attackingTarget.position + attackingTarget.velocity * Time.deltaTime;
				irMissileLauncher.headTransform.rotation = Quaternion.LookRotation(vector3 - irMissileLauncher.headTransform.position);
				yield return null;
			}
			if (!alive)
			{
				yield break;
			}
			if (IRTargetLocked(attackingTarget, seeker))
			{
				irMissileLauncher.TryFireMissile();
			}
			t2 = Time.time;
			while ((bool)attackingTarget && Time.time - t2 < 2f)
			{
				AimAtTarget(attackingTarget.position);
				isAiming = true;
				this.OnManpadAiming?.Invoke(attackingTarget.position);
				yield return null;
			}
			irMissileLauncher.DisableWeapon();
			this.OnManpadStopAiming?.Invoke();
			if (!reloading && irMissileLauncher.missileCount == 0)
			{
				StartCoroutine(ReloadIRMLRoutine());
			}
		}
		attackRoutine = null;
	}

	private bool IRTargetLocked(Actor tgt, HeatSeeker seeker)
	{
		if ((bool)tgt && seeker.seekerLock > 0.8f)
		{
			return (seeker.targetPosition - tgt.position).sqrMagnitude < 100f;
		}
		return false;
	}

	private IEnumerator AttackTargetRoutine()
	{
		WaitForSeconds rpmWait = new WaitForSeconds(60f / rpm);
		while ((bool)attackingTarget && Vector3.Angle(attackingTarget.transform.position - armTransform.position, armTransform.forward) > 2f)
		{
			yield return null;
		}
		if (!attackingTarget)
		{
			yield break;
		}
		if ((bool)gun)
		{
			gun.SetFire(fire: true);
			float burstTime = burstCount * (60f / rpm);
			float t = Time.time;
			while (Time.time - t < burstTime)
			{
				if ((bool)attackingTarget)
				{
					fireTransform.LookAt(attackingTarget.position);
				}
				yield return null;
			}
			gun.SetFire(fire: false);
		}
		else
		{
			for (int i = 0; (float)i < burstCount; i++)
			{
				if ((bool)attackingTarget)
				{
					fireTransform.LookAt(attackingTarget.position);
				}
				else
				{
					FireBullet();
				}
				yield return rpmWait;
			}
		}
		yield return new WaitForSeconds(burstInterval);
		attackRoutine = null;
	}

	private void FireBullet()
	{
		Bullet.FireBullet(fireTransform.position, fireTransform.forward, bulletSpeed, 0.1f, 3f, 0f, damage, Vector3.zero, new Color(1f, 0.7f, 0f, 0.75f), actor, 0f, 6f, 0f, 1.1E-05f);
		audioSource.PlayOneShot(fireSound);
		StartCoroutine(MuzzleFlashRoutine());
	}

	private IEnumerator MuzzleFlashRoutine()
	{
		muzzleLight.enabled = true;
		muzzleParticle.Emit(1);
		yield return null;
		yield return null;
		muzzleLight.enabled = false;
	}

	public void AimAtTarget(Vector3 tgtPos)
	{
		Vector3 vector = tgtPos;
		vector.y = base.transform.position.y;
		base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(vector - base.transform.position), 10f * Time.deltaTime);
		armTransform.rotation = Quaternion.RotateTowards(armTransform.rotation, Quaternion.LookRotation(tgtPos - armTransform.position), 90f * Time.deltaTime);
		aimPitch = VectorUtils.SignedAngle(base.transform.forward, Vector3.ProjectOnPlane(armTransform.forward, base.transform.right), Vector3.up);
	}

	public void LookToVelocity()
	{
		Vector3 velocity = mover.velocity;
		velocity.y = 0f;
		if (velocity.sqrMagnitude > 0.1f)
		{
			base.transform.rotation = Quaternion.Slerp(base.transform.rotation, Quaternion.LookRotation(velocity), 15f * Time.deltaTime);
		}
	}

	private Vector3 PlanarPos()
	{
		return new Vector3(base.transform.position.x, 0f, base.transform.position.z);
	}

	public void OnPassengerBayDied()
	{
		Debug.Log("Soldier.OnPassengerBayDied");
		if (!isLoadedInBay || !health)
		{
			return;
		}
		if (health.invincible)
		{
			isLoadedInBay = false;
			StartWaitingForPickup();
			Quaternion rotation = Quaternion.identity;
			if (!isRemote)
			{
				mover.move = true;
				softCollider.ignoreShip = false;
			}
			if ((bool)actor.unitSpawn && actor.unitSpawn is GroundUnitSpawn)
			{
				GroundUnitSpawn groundUnitSpawn = (GroundUnitSpawn)actor.unitSpawn;
				if (groundUnitSpawn.unitGroup != null)
				{
					((VTUnitGroup.UnitGroup.GroundGroupActions)groundUnitSpawn.unitGroup.groupActions).squad.RegisterUnit(mover);
				}
				loadedInBayPoint.point = actor.unitSpawn.unitSpawner.transform.position + actor.unitSpawn.heightFromSurface * actor.unitSpawn.transform.up;
				rotation = actor.unitSpawn.unitSpawner.transform.rotation;
			}
			base.transform.position = loadedInBayPoint.point;
			base.transform.rotation = rotation;
			Debug.Log(" - soldier is invincible.  Teleporting back to pickup location.");
			this.OnPassengerBayDiedWhileInvincible?.Invoke();
		}
		else
		{
			Debug.Log(" - killing soldier.");
			health.Kill();
		}
	}

	public void OnLoadInBay(PassengerBay bay)
	{
		loadedInBayPoint.point = base.transform.position;
		passengerBay = bay;
		if (!isRemote && (bool)health)
		{
			Health componentInParent = bay.GetComponentInParent<Health>();
			if ((bool)componentInParent)
			{
				componentInParent.OnDeath.AddListener(OnPassengerBayDied);
			}
		}
		StopWaitingForPickup();
		wasPickedUp = true;
		isLoadedInBay = true;
		modelTransform.localPosition = Vector3.zero;
		attackingTarget = null;
		StopAllCoroutines();
		pickupRoutine = null;
		mover.FullStop();
		if ((bool)mover.squad)
		{
			mover.squad.UnregisterUnit(mover);
		}
	}

	public void OnUnloadFromBay()
	{
		isLoadedInBay = false;
		rescued = true;
		mover.move = true;
		softCollider.ignoreShip = false;
		if ((bool)actor.unitSpawn && actor.unitSpawn is GroundUnitSpawn)
		{
			GroundUnitSpawn groundUnitSpawn = (GroundUnitSpawn)actor.unitSpawn;
			if (groundUnitSpawn.unitGroup != null)
			{
				((VTUnitGroup.UnitGroup.GroundGroupActions)groundUnitSpawn.unitGroup.groupActions).squad.RegisterUnit(mover);
			}
		}
	}

	public void SetVelocity(Vector3 v)
	{
		mover.SetVelocity(v);
	}

	public bool GetIsRescued()
	{
		if (alive)
		{
			return rescued;
		}
		return false;
	}

	public bool GetWasPickedUp()
	{
		return wasPickedUp;
	}

	public bool GetIsAlive()
	{
		return alive;
	}

	public bool GetCanMove()
	{
		if (!attackingTarget)
		{
			return !mover.railComplete;
		}
		return false;
	}

	public void SetColumn(GroundUnitColumn c)
	{
		column = c;
	}

	public void SetEngageEnemies(bool engage)
	{
		attackEnemies = engage;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(base.gameObject.name + "_Soldier");
		configNode.SetValue("waitingForPickup", waitingForPickup);
		configNode.SetValue("wasPickedUp", wasPickedUp);
		configNode.SetValue("boardingAIBay", boardingAIBay);
		if (boardingAIBay)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(boardingAIBayTarget.pilot.actor, "boardingAIBayTargetActor"));
		}
		configNode.SetValue("dismountingAIBay", dismountingAIBay);
		if (dismountingAIBay)
		{
			configNode.SetValue("dismountingAIBayRallyWp", VTSConfigUtils.WriteObject(VTScenario.current.waypoints.GetWaypoint(dismountingAIBayRallyTf)));
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_Soldier");
		if (node != null)
		{
			if (node.GetValue<bool>("waitingForPickup"))
			{
				StartWaitingForPickup();
			}
			else
			{
				StopWaitingForPickup();
			}
			wasPickedUp = node.GetValue<bool>("wasPickedUp");
			if (node.GetValue<bool>("boardingAIBay"))
			{
				Actor actor = QuicksaveManager.RetrieveActorFromNode(node.GetNode("boardingAIBayTargetActor"));
				BoardAIBay(actor.GetComponentInChildren<AIPassengerBay>(includeInactive: true));
			}
			dismountingAIBay = node.GetValue<bool>("dismountingAIBay");
			if (dismountingAIBay)
			{
				Waypoint waypoint = VTSConfigUtils.ParseObject<Waypoint>(node.GetValue("dismountingAIBayRallyWp"));
				DismountAIBay(waypoint.GetTransform());
			}
		}
	}
}
