using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class PassengerBay : MonoBehaviour, IQSVehicleComponent, IMassObject
{
	public enum RampStates
	{
		Open,
		Closed,
		Opening,
		Closing
	}

	public static List<PassengerBay> passengerBays = new List<PassengerBay>();

	public Transform[] seats;

	public Soldier[] loadedSoldiers;

	public Transform exitTransform;

	public FlightInfo flightInfo;

	public Transform loadingTransform;

	public float loadRadius;

	private float loadSqrRadius;

	public Animator animator;

	public string animSpeedName;

	public int doorAnimLayer = 1;

	private RampStates _rs = RampStates.Closed;

	public SimpleDrag dragComponent;

	public FollowPath[] pickupPaths;

	private bool mpRemote;

	private float lastUnloadTime;

	private float unloadInterval = 1f;

	public float soldierPickupRadius = 80f;

	private Coroutine rotateRoutine;

	private Coroutine checkForSoldiersRoutine;

	public RampStates rampState
	{
		get
		{
			return _rs;
		}
		set
		{
			_rs = value;
		}
	}

	public UnloadingZone inUnloadZone { get; set; }

	public Actor actor { get; private set; }

	public event Action<RampStates> OnRampState;

	public void SetToRemote()
	{
		mpRemote = true;
	}

	private void Awake()
	{
		actor = GetComponentInParent<Actor>();
	}

	private void Start()
	{
		loadedSoldiers = new Soldier[seats.Length];
		rampState = RampStates.Closed;
		loadSqrRadius = loadRadius * loadRadius;
		passengerBays.Add(this);
	}

	private void OnDestroy()
	{
		passengerBays.Remove(this);
	}

	private void Update()
	{
		if ((bool)inUnloadZone && flightInfo.isLanded && rampState == RampStates.Open && flightInfo.surfaceSpeed < 0.1f && Time.time - lastUnloadTime > unloadInterval)
		{
			UnloadSoldier(inUnloadZone);
			lastUnloadTime = Time.time;
		}
	}

	private IEnumerator OpenRoutine()
	{
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(doorAnimLayer);
		while (currentAnimatorStateInfo.normalizedTime < 1f)
		{
			_ = (bool)dragComponent;
			if (!mpRemote)
			{
				AudioController.instance.SetExteriorOpening("bayDoor", currentAnimatorStateInfo.normalizedTime / 2f);
			}
			yield return null;
			currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(doorAnimLayer);
		}
		animator.SetFloat(animSpeedName, 0f);
		if (!mpRemote)
		{
			AudioController.instance.SetExteriorOpening("bayDoor", 0.5f);
		}
		rampState = RampStates.Open;
		this.OnRampState?.Invoke(rampState);
		if (!mpRemote)
		{
			if (checkForSoldiersRoutine != null)
			{
				StopCoroutine(checkForSoldiersRoutine);
			}
			checkForSoldiersRoutine = StartCoroutine(CheckForSoldiersRoutine());
		}
		rotateRoutine = null;
	}

	private IEnumerator CloseRoutine()
	{
		AnimatorStateInfo currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(doorAnimLayer);
		while (currentAnimatorStateInfo.normalizedTime > 0f)
		{
			_ = (bool)dragComponent;
			if (!mpRemote)
			{
				AudioController.instance.SetExteriorOpening("bayDoor", currentAnimatorStateInfo.normalizedTime / 2f);
			}
			yield return null;
			currentAnimatorStateInfo = animator.GetCurrentAnimatorStateInfo(doorAnimLayer);
		}
		animator.SetFloat(animSpeedName, 0f);
		if (!mpRemote)
		{
			AudioController.instance.SetExteriorOpening("bayDoor", 0f);
		}
		rampState = RampStates.Closed;
		this.OnRampState?.Invoke(rampState);
		rotateRoutine = null;
	}

	private IEnumerator CheckForSoldiersRoutine()
	{
		Soldier soldierToLoad = null;
		while (rampState == RampStates.Open)
		{
			if (Soldier.soldiersForPickup != null && flightInfo.surfaceSpeed < 0.1f)
			{
				int count = Soldier.soldiersForPickup.Count;
				for (int i = 0; i < count; i++)
				{
					Soldier soldier = Soldier.soldiersForPickup[i];
					if ((bool)soldier && soldier.waitingForPickup && (bool)soldier.actor && soldier.actor.team == actor.team && !soldier.isLoadedInBay && (soldier.modelTransform.position - loadingTransform.position).sqrMagnitude < loadSqrRadius)
					{
						soldierToLoad = soldier;
						i = count;
					}
				}
				if (soldierToLoad != null)
				{
					LoadSoldier(soldierToLoad);
					soldierToLoad = null;
				}
			}
			yield return null;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)loadingTransform)
		{
			Gizmos.color = new Color(1f, 1f, 0f, 1f);
			Gizmos.DrawWireSphere(loadingTransform.position, loadRadius);
		}
		Gizmos.DrawWireSphere(base.transform.position, soldierPickupRadius);
	}

	public void ToggleRamp()
	{
		if (rampState == RampStates.Closed || rampState == RampStates.Closing)
		{
			OpenRamp();
		}
		else
		{
			CloseRamp();
		}
	}

	public void SetRamp(int r)
	{
		if (r > 0)
		{
			OpenRamp();
		}
		else
		{
			CloseRamp();
		}
	}

	private void OpenRamp()
	{
		rampState = RampStates.Opening;
		this.OnRampState?.Invoke(rampState);
		animator.SetFloat(animSpeedName, 1f);
		if (rotateRoutine != null)
		{
			StopCoroutine(rotateRoutine);
		}
		rotateRoutine = StartCoroutine(OpenRoutine());
	}

	private void CloseRamp()
	{
		rampState = RampStates.Closing;
		this.OnRampState?.Invoke(rampState);
		animator.SetFloat(animSpeedName, -1f);
		if (rotateRoutine != null)
		{
			StopCoroutine(rotateRoutine);
		}
		rotateRoutine = StartCoroutine(CloseRoutine());
	}

	public void LoadSoldier(Soldier s, bool instantMove = false)
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			SoldierSync component = s.GetComponent<SoldierSync>();
			VTNetEntity componentInParent = GetComponentInParent<VTNetEntity>();
			if (component.isMine)
			{
				component.HostLoadIntoBay(componentInParent);
			}
			else
			{
				component.ClientRequestLoadIntoBay(componentInParent);
			}
			return;
		}
		s.StopWaitingForPickup();
		for (int i = 0; i < loadedSoldiers.Length; i++)
		{
			if (!loadedSoldiers[i])
			{
				StartLoadSoldierRoutine(s, i, instantMove);
				break;
			}
		}
	}

	public void MP_LoadSoldier(Soldier s, bool instantMove = false)
	{
		s.StopWaitingForPickup();
		for (int i = 0; i < loadedSoldiers.Length; i++)
		{
			if (!loadedSoldiers[i])
			{
				StartLoadSoldierRoutine(s, i, instantMove);
				break;
			}
		}
	}

	public void UnloadSoldier(UnloadingZone unloadZone)
	{
		Soldier soldier = null;
		for (int i = 0; i < loadedSoldiers.Length; i++)
		{
			if ((bool)loadedSoldiers[i] && (loadedSoldiers[i].targetUnloadZones == null || loadedSoldiers[i].targetUnloadZones.Contains(unloadZone)))
			{
				soldier = loadedSoldiers[i];
				loadedSoldiers[i] = null;
				if (soldier.targetUnloadZones != null)
				{
					soldier.targetUnloadZones.Remove(unloadZone);
				}
				break;
			}
		}
		if ((bool)soldier)
		{
			StartCoroutine(UnloadSoldierRoutine(soldier, unloadZone));
		}
	}

	private void StartLoadSoldierRoutine(Soldier s, int seatIdx, bool instantMove)
	{
		s.OnLoadInBay(this);
		Transform seatTf = seats[seatIdx];
		loadedSoldiers[seatIdx] = s;
		StartCoroutine(HoldPassengerInBay(s, seatTf, instantMove));
		s.mover.ClearSurfaceObj();
	}

	private IEnumerator HoldPassengerInBay(Soldier s, Transform seatTf, bool instantMove)
	{
		Vector3 lp = seatTf.InverseTransformPoint(s.transform.position);
		if (!instantMove)
		{
			while (Vector3.SqrMagnitude(lp) > 0.1f)
			{
				lp = Vector3.Lerp(lp, Vector3.zero, 5f * Time.deltaTime);
				s.transform.rotation = Quaternion.Lerp(s.transform.rotation, seatTf.rotation, 5f * Time.deltaTime);
				s.transform.position = seatTf.TransformPoint(lp);
				yield return null;
			}
		}
		while (s.isLoadedInBay)
		{
			s.transform.position = seatTf.transform.position;
			s.transform.rotation = seatTf.rotation;
			yield return null;
		}
	}

	private IEnumerator UnloadSoldierRoutine(Soldier s, UnloadingZone unloadZone)
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			s.GetComponent<SoldierSync>().UnloadFromBay(unloadZone);
			yield break;
		}
		s.isLoadedInBay = false;
		Vector3 tgtFwd2 = exitTransform.forward;
		tgtFwd2.y = 0f;
		tgtFwd2 = tgtFwd2.normalized;
		Quaternion targetRot = Quaternion.LookRotation(tgtFwd2, Vector3.up);
		Vector3 lp = exitTransform.InverseTransformPoint(s.transform.position);
		while (Vector3.SqrMagnitude(lp) > 0.1f)
		{
			lp = Vector3.MoveTowards(lp, Vector3.zero, s.mover.moveSpeed * Time.deltaTime);
			s.transform.rotation = Quaternion.Lerp(s.transform.rotation, targetRot, 15f * Time.deltaTime);
			s.transform.position = exitTransform.TransformPoint(lp);
			yield return null;
		}
		FollowPath followPath = null;
		Waypoint waypoint = null;
		s.SetVelocity(tgtFwd2 * s.mover.moveSpeed);
		if (unloadZone.unloadPaths != null && unloadZone.unloadPaths.Length != 0)
		{
			float num = float.MaxValue;
			FollowPath followPath2 = null;
			FollowPath[] unloadPaths = unloadZone.unloadPaths;
			foreach (FollowPath followPath3 in unloadPaths)
			{
				float sqrMagnitude = (s.transform.position - followPath3.pointTransforms[0].position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					followPath2 = followPath3;
				}
			}
			s.mover.path = followPath2;
			s.mover.behavior = GroundUnitMover.Behaviors.Path;
			followPath = followPath2;
		}
		else
		{
			s.mover.MoveToWaypoint(unloadZone.unloadRallyPoint);
			s.mover.parkWhenInRallyRadius = true;
			waypoint = unloadZone.unloadRallyWpt;
		}
		s.mover.rallyRadius = 1f;
		s.OnUnloadFromBay();
		s.mover.RefreshBehaviorRoutines();
		if ((bool)s.mover.squad && s.mover.squad.leaderMover == s.mover)
		{
			if (waypoint != null)
			{
				s.mover.squad.MoveTo(waypoint.GetTransform());
			}
			else if (followPath != null)
			{
				s.mover.squad.MovePath(followPath);
			}
		}
	}

	public FollowPath GetPickupPath(Vector3 soldierPosition)
	{
		float num = float.MaxValue;
		FollowPath result = null;
		for (int i = 0; i < pickupPaths.Length; i++)
		{
			float sqrMagnitude = (soldierPosition - pickupPaths[i].transform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = pickupPaths[i];
			}
		}
		return result;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode(base.gameObject.name + "_PassengerBay");
		Soldier[] array = loadedSoldiers;
		foreach (Soldier soldier in array)
		{
			if ((bool)soldier && (bool)soldier.actor && soldier.actor.alive)
			{
				configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(soldier.actor, "loadedSoldier"));
			}
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode(base.gameObject.name + "_PassengerBay");
		if (node == null)
		{
			return;
		}
		List<Soldier> list = new List<Soldier>();
		foreach (ConfigNode node2 in node.GetNodes("loadedSoldier"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor)
			{
				Soldier component = actor.GetComponent<Soldier>();
				if ((bool)component && !list.Contains(component))
				{
					list.Add(component);
				}
			}
		}
		foreach (Soldier item in list)
		{
			LoadSoldier(item, instantMove: true);
		}
	}

	public float GetMass()
	{
		float num = 0f;
		for (int i = 0; i < loadedSoldiers.Length; i++)
		{
			if ((bool)loadedSoldiers[i])
			{
				num += loadedSoldiers[i].soldierMass;
			}
		}
		return num;
	}
}
