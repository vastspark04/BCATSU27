using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using VTNetworking;
using VTOLVR.DLC.Rotorcraft;

namespace VTOLVR.Multiplayer{

public class MultiUserVehicleSync : VTNetSyncRPCOnly
{
	public delegate void SeatOccupantDelegate(int seatIdx, ulong userID);

	[Serializable]
	public struct LeverLockInfo
	{
		public int lockTo;

		public VRLever lever;
	}

	public Rigidbody rb;

	public FlightInfo flightInfo;

	public VehicleMaster vm;

	public VTOLCollisionEffects collisionEffects;

	public MultiSlotVehicleManager msVehicleManager;

	public GameObject[] disableOnRemote;

	public GameObject[] destroyOnMP;

	public GameObject[] passengerOnlyObjects;

	public GameObject[] nonPassengerObjects;

	public MFDCommsPage commsPage;

	public VehicleInputManager vim;

	private ulong[] seatOccupants;

	private float lastSendTime;

	private bool lockedOwner;

	public List<VRInteractable> controlInteractables;

	[Header("Synced components")]
	public Battery battery;

	public WeaponManager wm;

	private bool wasArmed;

	public TargetingMFDPage tgpPage;

	public MFDGPSTargets gpsPage;

	public CountermeasureManager cmm;

	public ConnectedJoystickSync cStickSync;

	public ConnectedThrottlesSync cThrottleSync;

	private bool _localSeated;

	private bool gpsDirty;

	private bool hasInitialized;

	private List<VRInteractable> localControlledInteractables = new List<VRInteractable>();

	private bool returningToBriefingRoom;

	private bool unity_isDestroyed;

	private float timeLocalRequesting;

	private float timeOtherRequested;

	private Vector3 myPyr;

	private Vector3 myInputPyr;

	private float myBrakes;

	private float myThrottle;

	private FixedPoint syncedPos;

	private Vector3 syncedVel;

	private Vector3 syncedAccel;

	private Quaternion syncedRot;

	private Vector3 syncedPyr;

	private Vector3 syncedInputPyr;

	private float syncedBrakes;

	private float syncedThrottle;

	private float currCorrectionDist = 50f;

	private float targetCorrectionDist = 50f;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 9f;

	public static float interpSpeedDiv = 200f;

	private bool interpolatingPos;

	private string lastEqShortname;

	public VRInteractable[] disableInteractableOnRearming;

	public LeverLockInfo[] lockLeversOnRearming;

	private bool ignoreNextCMReleaseMode;

	private bool ignoreNextRelRate;

	private VRDoor[] vrDoors;

	public VehiclePart[] vehicleParts;

	public int seatCount => seatOccupants.Length;

	public ulong controlOwner { get; private set; }

	private bool localPlayerSeated
	{
		get
		{
			return _localSeated;
		}
		set
		{
			if (_localSeated != value)
			{
				_localSeated = value;
				Debug.Log($"{base.gameObject.name}: localPlayerSeated = {value}");
			}
		}
	}

	public bool isRemoteControlRequesting => Time.time - timeOtherRequested < 1f;

	public bool isLocalControlRequesting => Time.time - timeLocalRequesting < 1f;

	public ulong weaponControllerId { get; private set; }

	public ulong tgpControllerId { get; private set; }

	public int flareCountL { get; private set; }

	public int flareCountR { get; private set; }

	public int flareCountTotal => flareCountL + flareCountR;

	public int chaffCountL { get; private set; }

	public int chaffCountR { get; private set; }

	public int chaffCountTotal => chaffCountL + chaffCountR;

	public event SeatOccupantDelegate OnOccupantEntered;

	public event SeatOccupantDelegate OnOccupantLeft;

	public event Action<ulong> OnSetControlOwnerID;

	public event Action<Vector3> OnSyncedInputPYR;

	public event Action<ulong> OnSetWeaponControllerId;

	public event Action<ulong> OnPilotTakeTGP;

	public event Action OnCMCountsUpdated;

	public event Action OnReleaseRateChanged;

	public ulong GetOccupantID(int seatIdx)
	{
		return seatOccupants[seatIdx];
	}

	public void SendRPCToCopilots(VTNetSync ns, string funcName, params object[] parameters)
	{
		if (seatOccupants == null)
		{
			return;
		}
		for (int i = 0; i < seatCount; i++)
		{
			ulong occupantID = GetOccupantID(i);
			if (occupantID != 0L && occupantID != BDSteamClient.mySteamID)
			{
				ns.SendDirectedRPC(occupantID, funcName, parameters);
			}
		}
	}

	public bool OwnerIsLocked()
	{
		return lockedOwner;
	}

	public void SetBatteryConnection(int c)
	{
		battery.SetConnection3Way(c);
		SendRPC("RPC_SetBatt", c);
	}

	[VTRPC]
	private void RPC_SetBatt(int c)
	{
		battery.SetConnection3Way(c);
	}

	public bool IsControlOwner()
	{
		return controlOwner == BDSteamClient.mySteamID;
	}

	public bool IsLocalPlayerSeated()
	{
		return localPlayerSeated;
	}

	public bool IsPlayerSeated(ulong id)
	{
		for (int i = 0; i < seatCount; i++)
		{
			if (seatOccupants[i] == id)
			{
				return true;
			}
		}
		return false;
	}

	public int UserSeatIdx(ulong id)
	{
		for (int i = 0; i < seatCount; i++)
		{
			if (seatOccupants[i] == id)
			{
				return i;
			}
		}
		return -1;
	}

	public int LocalPlayerSeatIdx()
	{
		for (int i = 0; i < seatOccupants.Length; i++)
		{
			if (seatOccupants[i] == BDSteamClient.mySteamID)
			{
				return i;
			}
		}
		return -1;
	}

	protected override void Awake()
	{
		Initialize();
		base.Awake();
		SetDoorAudio();
		seatOccupants = new ulong[msVehicleManager.slots.Length];
	}

	public void SetOccupant(int seatIdx, ulong userID)
	{
		if (seatOccupants == null)
		{
			Debug.LogError("MultiUserVehicleSync.SetOccupant : seatOccupants is null");
			return;
		}
		if (seatIdx >= 0 && seatIdx < seatOccupants.Length)
		{
			if (seatOccupants[seatIdx] != userID)
			{
				ulong num = seatOccupants[seatIdx];
				seatOccupants[seatIdx] = userID;
				if (userID != 0L)
				{
					if (userID == BDSteamClient.mySteamID)
					{
						localPlayerSeated = true;
					}
					Debug.Log(VTOLMPLobbyManager.GetPlayer(userID).pilotName + " has entered a multicrew seat in " + wm.actor.name);
					this.OnOccupantEntered?.Invoke(seatIdx, userID);
				}
				else
				{
					PlayerInfo player = VTOLMPLobbyManager.GetPlayer(num);
					Debug.Log(((player != null) ? player.pilotName : "A player") + " has left a multicrew seat in " + wm.actor.name);
					if (num == BDSteamClient.mySteamID)
					{
						localPlayerSeated = false;
					}
					this.OnOccupantLeft?.Invoke(seatIdx, num);
				}
			}
		}
		else
		{
			Debug.LogError("MultiUserVehicleSync.SetOccupant : seatIdx is out of bounds");
		}
		UpdateActorName();
	}

	private void UpdateActorName()
	{
		if (!vm.actor)
		{
			return;
		}
		if (seatOccupants == null)
		{
			vm.actor.actorName = vm.playerVehicle.vehicleName;
			return;
		}
		string text = string.Empty;
		for (int i = 0; i < seatOccupants.Length; i++)
		{
			if (seatOccupants[i] != 0L)
			{
				PlayerInfo player = VTOLMPLobbyManager.GetPlayer(seatOccupants[i]);
				if (player != null)
				{
					text = text + player.pilotName + ", ";
				}
			}
		}
		if (text.Length > 2)
		{
			text = text.TrimEnd(',', ' ');
		}
		vm.actor.actorName = vm.playerVehicle.vehicleName + " (" + text + ")";
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		RPC_SetControlOwner(base.netEntity.ownerID);
		VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
		Initialize();
		SetupVehiclePartEvents();
		if ((bool)wm)
		{
			if (base.isMine)
			{
				TakeWeaponControl();
			}
			else
			{
				wm.gpsSystem.remoteOnly = true;
			}
			wm.gpsSystem.muvs = this;
		}
		if ((bool)gpsPage)
		{
			if (!base.isMine)
			{
				gpsPage.remoteOnly = true;
			}
			gpsPage.muvs = this;
		}
		vm.OnPilotDied += Vm_OnPilotDied;
		if (base.isMine)
		{
			disableOnRemote.SetActive(active: true);
			collisionEffects.enabled = true;
			RPC_SetTGPOwner(BDSteamClient.mySteamID);
			RPC_SetControlOwner(BDSteamClient.mySteamID);
			SendRPC("RPC_SetControlOwner", BDSteamClient.mySteamID);
		}
		else
		{
			disableOnRemote.SetActive(active: false);
			base.gameObject.SetActive(value: true);
			vm.enabled = false;
		}
		passengerOnlyObjects.SetActive(base.isMine);
		nonPassengerObjects.SetActive(!base.isMine);
		OnOccupantEntered += MultiUserVehicleSync_OnOccupantEntered;
		OnOccupantLeft += MultiUserVehicleSync_OnOccupantLeft;
		VTOLMPLobbyManager.OnPlayerLeft += VTOLMPLobbyManager_OnPlayerLeft;
		UpdateActorName();
	}

	private void SetGPSDirty()
	{
		gpsDirty = true;
	}

	private void Vm_OnPilotDied()
	{
		SendRPCToCopilots(this, "RPC_KillPilot");
		MultiSlotVehicleManager.PlayerSlot[] slots = msVehicleManager.slots;
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].spawnTransform.gameObject.SetActive(value: false);
		}
	}

	[VTRPC]
	private void RPC_KillPilot()
	{
		if (IsLocalPlayerSeated())
		{
			vm.OnPilotDied -= Vm_OnPilotDied;
			vm.KillPilot();
		}
		MultiSlotVehicleManager.PlayerSlot[] slots = msVehicleManager.slots;
		for (int i = 0; i < slots.Length; i++)
		{
			slots[i].spawnTransform.gameObject.SetActive(value: false);
		}
	}

	private void VTOLMPLobbyManager_OnPlayerLeft(PlayerInfo obj)
	{
		if (base.isMine && (ulong)obj.steamUser.Id == controlOwner)
		{
			Debug.Log("MUVS.OnPlayerLeft: control owner has left.  returning control to entity owner");
			RPC_SetControlOwner(base.netEntity.ownerID);
			SendRPC("RPC_SetControlOwner", base.netEntity.ownerID);
		}
		for (int i = 0; i < seatOccupants.Length; i++)
		{
			if (seatOccupants[i] == (ulong)obj.steamUser.Id)
			{
				SetOccupant(i, 0uL);
			}
		}
	}

	private void Instance_OnWillReturnToBriefingRoom()
	{
		int num = LocalPlayerSeatIdx();
		if (num >= 0)
		{
			SetOccupant(num, 0uL);
			SendRPC("RPC_SetOccupant", num, 0);
		}
		returningToBriefingRoom = true;
		VTOLMPSceneManager.instance.OnWillReturnToBriefingRoom -= Instance_OnWillReturnToBriefingRoom;
		PlayerVehicleSetup component = GetComponent<PlayerVehicleSetup>();
		component.OnBeginRearming.RemoveListener(OnBeginRearming);
		component.OnEndRearming.RemoveListener(OnEndRearming);
		FlightSceneManager.instance.playerActor = null;
	}

	private void SetupOccupantListeners()
	{
		RemoveOccupantListeners();
		WaypointManager.instance.OnSetActorWaypoint += Instance_OnSetActorWaypoint;
		WaypointManager.instance.OnSetGPSWaypoint += Instance_OnSetGPSWaypoint;
		WaypointManager.instance.OnSetUnknownWaypoint += Instance_OnSetUnknownWaypoint;
		WaypointManager.instance.OnSetWaypoint += Instance_OnSetWaypoint;
		if ((bool)cmm)
		{
			cmm.OnSetReleaseMode += OnSetReleaseMode;
			cmm.OnReleaseRateChanged += Cmm_OnReleaseRateChanged;
			if (base.isMine)
			{
				cmm.OnFiredCM += OnOwnerCmCountsChanged;
			}
		}
		if ((bool)wm)
		{
			wm.OnWeaponChanged.AddListener(WeaponChanged);
			wm.OnRippleChanged += Wm_OnRippleChanged;
			wm.OnEquipArmingChanged += Wm_OnEquipArmingChanged;
			wm.OnEquipJettisonChanged += Wm_OnEquipJettisonChanged;
			if (base.isMine)
			{
				wm.gpsSystem.onGPSTargetsChanged.AddListener(SetGPSDirty);
			}
		}
		if ((bool)tgpPage)
		{
			tgpPage.mfdPage.OnSetSOI += TGPMfdPage_OnSetSOI;
			tgpPage.OnTGPPwrButton += TgpPage_OnTGPPwrButton;
			tgpPage.OnSetMode += TgpPage_OnSetMode;
			TargetingMFDPage targetingMFDPage = tgpPage;
			targetingMFDPage.OnSetFovIdx = (Action<int>)Delegate.Combine(targetingMFDPage.OnSetFovIdx, new Action<int>(TgpPage_OnSetFov));
			tgpPage.OnSetSensorMode += TgpPage_OnSetSensorMode;
		}
	}

	private void RemoveOccupantListeners()
	{
		if ((bool)WaypointManager.instance)
		{
			WaypointManager.instance.OnSetActorWaypoint -= Instance_OnSetActorWaypoint;
			WaypointManager.instance.OnSetGPSWaypoint -= Instance_OnSetGPSWaypoint;
			WaypointManager.instance.OnSetUnknownWaypoint -= Instance_OnSetUnknownWaypoint;
			WaypointManager.instance.OnSetWaypoint -= Instance_OnSetWaypoint;
		}
		if ((bool)cmm)
		{
			cmm.OnSetReleaseMode -= OnSetReleaseMode;
			cmm.OnReleaseRateChanged -= Cmm_OnReleaseRateChanged;
			cmm.OnFiredCM -= OnOwnerCmCountsChanged;
		}
		if ((bool)wm)
		{
			wm.OnWeaponChanged.RemoveListener(WeaponChanged);
			wm.OnRippleChanged -= Wm_OnRippleChanged;
			wm.OnEquipArmingChanged -= Wm_OnEquipArmingChanged;
			wm.OnEquipJettisonChanged -= Wm_OnEquipJettisonChanged;
			wm.gpsSystem.onGPSTargetsChanged.RemoveListener(SetGPSDirty);
		}
		if ((bool)tgpPage)
		{
			tgpPage.mfdPage.OnSetSOI -= TGPMfdPage_OnSetSOI;
			tgpPage.OnTGPPwrButton -= TgpPage_OnTGPPwrButton;
			tgpPage.OnSetMode -= TgpPage_OnSetMode;
			TargetingMFDPage targetingMFDPage = tgpPage;
			targetingMFDPage.OnSetFovIdx = (Action<int>)Delegate.Remove(targetingMFDPage.OnSetFovIdx, new Action<int>(TgpPage_OnSetFov));
			tgpPage.OnSetSensorMode -= TgpPage_OnSetSensorMode;
		}
	}

	private void MultiUserVehicleSync_OnOccupantEntered(int seatIdx, ulong userID)
	{
		bool flag = userID == BDSteamClient.mySteamID;
		if (base.isMine)
		{
			if (!flag)
			{
				SendDirectedRPC(userID, "RPC_SetArm", wm.isMasterArmed ? 1 : 0);
				SendDirectedRPC(userID, "RPC_SetTGPOwner", tgpControllerId);
				for (int i = 0; i < wm.equipCount; i++)
				{
					HPEquippable equip = wm.GetEquip(i);
					if ((bool)equip)
					{
						if (equip is IRippleWeapon)
						{
							IRippleWeapon rippleWeapon = (IRippleWeapon)equip;
							SendDirectedRPC(userID, "RPC_Ripple", i, rippleWeapon.GetRippleRateIdx());
						}
						SendDirectedRPC(userID, "RPC_SetEqArmed", equip.hardpointIdx, equip.armed ? 1 : 0);
					}
				}
				if ((bool)wm.currentEquip)
				{
					SendDirectedRPC(userID, "RPC_SetEq", wm.currentEquip.hardpointIdx);
				}
				SendDirectedRPC(userID, "RPC_SetWpnController", weaponControllerId);
				if (flightInfo.isLanded)
				{
					Debug.Log(" - vehicle was landed");
					VRDoor[] componentsInChildren = GetComponentsInChildren<VRDoor>();
					bool flag2 = false;
					VRDoor[] array = componentsInChildren;
					foreach (VRDoor vRDoor in array)
					{
						if (vRDoor.muvsSeatIdx == 0 && !vRDoor.isLatched)
						{
							flag2 = true;
							break;
						}
					}
					if (flag2)
					{
						array = componentsInChildren;
						foreach (VRDoor vRDoor2 in array)
						{
							if (vRDoor2.muvsSeatIdx == seatIdx && vRDoor2.openOnSpawn_mp)
							{
								vRDoor2.RemoteSetState(1f);
							}
						}
					}
				}
				else
				{
					Debug.Log(" - vehicle was airborne");
				}
				if ((bool)tgpPage)
				{
					TgpPage_OnSetFov(tgpPage.fovIdx);
					TgpPage_OnSetMode(tgpPage.tgpMode);
					TgpPage_OnTGPPwrButton(tgpPage.powered);
					TgpPage_OnSetSensorMode(tgpPage.sensorMode);
				}
				if ((bool)cmm)
				{
					SendRPCToCopilots(this, "RPC_CMMode", (int)cmm.releaseMode);
					OnOwnerCmCountsChanged();
					Cmm_OnReleaseRateChanged();
				}
			}
			PlayerVehicleSetup component = GetComponent<PlayerVehicleSetup>();
			component.OnBeginRearming.AddListener(OnBeginRearming);
			component.OnEndRearming.AddListener(OnEndRearming);
		}
		if (flag)
		{
			returningToBriefingRoom = false;
			VTOLMPSceneManager.instance.OnWillReturnToBriefingRoom -= Instance_OnWillReturnToBriefingRoom;
			VTOLMPSceneManager.instance.OnWillReturnToBriefingRoom += Instance_OnWillReturnToBriefingRoom;
			if (seatIdx != 0)
			{
				commsPage.forceDisallowRearm = true;
			}
			else
			{
				commsPage.OnRequestingRearming += CommsPage_OnRequestingRearming;
			}
			for (int k = 0; k < wm.equipCount; k++)
			{
				HPEquippable equip2 = wm.GetEquip(k);
				if ((bool)equip2)
				{
					AudioSource[] componentsInChildren2 = equip2.GetComponentsInChildren<AudioSource>(includeInactive: true);
					for (int j = 0; j < componentsInChildren2.Length; j++)
					{
						componentsInChildren2[j].mute = false;
					}
				}
			}
			VisualTargetFinder component2 = GetComponent<VisualTargetFinder>();
			component2.enabled = true;
			component2.fovReference = VRHead.instance.transform;
			localPlayerSeated = true;
			SetupOccupantListeners();
			vm.enabled = true;
		}
		if (IsLocalPlayerSeated())
		{
			wm.actor.DisableIcon();
		}
		else
		{
			wm.actor.EnableIcon();
		}
		passengerOnlyObjects.SetActive(IsLocalPlayerSeated());
		nonPassengerObjects.SetActive(!IsLocalPlayerSeated());
		if ((bool)vim)
		{
			vim.enabled = IsLocalPlayerSeated();
		}
		SetDoorAudio();
	}

	private void MultiUserVehicleSync_OnOccupantLeft(int seatIdx, ulong userID)
	{
		if (unity_isDestroyed)
		{
			return;
		}
		if (base.isMine)
		{
			if (userID == tgpControllerId && userID != BDSteamClient.mySteamID)
			{
				TakeTGPControl();
			}
			if (userID == controlOwner)
			{
				Debug.Log(" - the user was the control owner.  Returning control to the entity owner.");
				RPC_SetControlOwner(base.netEntity.ownerID);
				SendRPC("RPC_SetControlOwner", base.netEntity.ownerID);
				if (base.isMine)
				{
					cStickSync.ResetUngrabbedControls();
					cThrottleSync.ResetUngrabbedControls();
				}
			}
		}
		if (IsLocalPlayerSeated())
		{
			wm.actor.DisableIcon();
		}
		else
		{
			wm.actor.EnableIcon();
			GetComponent<VisualTargetFinder>().enabled = false;
			RemoveOccupantListeners();
			vm.enabled = false;
		}
		passengerOnlyObjects.SetActive(IsLocalPlayerSeated());
		nonPassengerObjects.SetActive(!IsLocalPlayerSeated());
		if ((bool)vim)
		{
			vim.enabled = IsLocalPlayerSeated();
		}
		SetDoorAudio();
	}

	public void Initialize()
	{
		if (hasInitialized)
		{
			return;
		}
		hasInitialized = true;
		Actor component = GetComponent<Actor>();
		if (VTOLMPUtils.IsMultiplayer())
		{
			GameObject[] array = destroyOnMP;
			foreach (GameObject gameObject in array)
			{
				if ((bool)gameObject)
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
			MultiSlotVehicleManager.PlayerSlot[] slots = msVehicleManager.slots;
			foreach (MultiSlotVehicleManager.PlayerSlot playerSlot in slots)
			{
				if ((bool)playerSlot.localNPCObj)
				{
					playerSlot.localNPCObj.SetActive(value: false);
				}
				if ((bool)playerSlot.localPlayerObj)
				{
					playerSlot.localPlayerObj.SetActive(value: false);
				}
			}
			foreach (VRInteractable controlInteractable in controlInteractables)
			{
				if ((bool)controlInteractable)
				{
					VRInteractable vrInt = controlInteractable;
					controlInteractable.OnInteract.AddListener(delegate
					{
						OnControlInteract(vrInt);
					});
					controlInteractable.OnStopInteract.AddListener(delegate
					{
						OnControlStopInteract(vrInt);
					});
					controlInteractable.OnInteracting.AddListener(OnControlInteracting);
				}
			}
			ShipSurviveObjective component2 = GetComponent<ShipSurviveObjective>();
			if ((bool)component2)
			{
				UnityEngine.Object.Destroy(component2);
			}
			Collider[] componentsInChildren = GetComponentsInChildren<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildren)
			{
				if (collider.gameObject.layer == 8)
				{
					collider.gameObject.layer = 13;
				}
			}
		}
		else
		{
			collisionEffects.enabled = true;
			disableOnRemote.SetActive(active: true);
			FlightSceneManager.instance.playerActor = component;
			GetComponent<FloatingOriginShifter>().enabled = true;
			GetComponent<FlightWarnings>().enabled = true;
			passengerOnlyObjects.SetActive(active: true);
			nonPassengerObjects.SetActive(active: false);
			TakeTGPControl();
		}
	}

	private void OnControlInteract(VRInteractable i)
	{
		localControlledInteractables.Add(i);
	}

	private void OnControlStopInteract(VRInteractable i)
	{
		localControlledInteractables.Remove(i);
		if (localControlledInteractables.Count == 0 && IsControlOwner())
		{
			SetLockedOwner(locked: false);
		}
	}

	private void OnControlInteracting()
	{
		if (!IsControlOwner())
		{
			TryTakeControl();
		}
		if (IsControlOwner())
		{
			SetLockedOwner(locked: true);
		}
	}

	public void SpawnLocalPlayerAvatar()
	{
		MultiSlotVehicleManager.PlayerSlot slot = GetSlot(VTOLMPLobbyManager.localPlayerInfo);
		StartCoroutine(SpawnPlayerAvatar(slot, VTOLMPSceneManager.instance.GetSlot(VTOLMPLobbyManager.localPlayerInfo).seatIdx));
	}

	private void OnEnable()
	{
		Initialize();
	}

	public MultiSlotVehicleManager.PlayerSlot GetSlot(PlayerInfo player)
	{
		int num = -1;
		PlayerInfo player2 = VTOLMPLobbyManager.GetPlayer(base.netEntity.ownerID);
		int unitID = GetComponentInParent<Actor>().unitSpawn.unitID;
		Debug.Log($"Checking vehicle slot for player {player.pilotName}.  mySpawnID={unitID}");
		List<VTOLMPSceneManager.VehicleSlot> list = ((player2.team == Teams.Allied) ? VTOLMPSceneManager.instance.alliedSlots : VTOLMPSceneManager.instance.enemySlots);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].spawnID == unitID)
			{
				Debug.Log($"Lobby slot #{i} is this vehicle.");
				num++;
				if (list[i].player == player)
				{
					Debug.Log($" - our vehicleSlot is #{num}");
					break;
				}
			}
		}
		if (num >= 0)
		{
			return msVehicleManager.slots[num];
		}
		return null;
	}

	private IEnumerator SpawnPlayerAvatar(MultiSlotVehicleManager.PlayerSlot slot, int seatIdx)
	{
		if (slot == null)
		{
			Debug.LogError("SpawnPlayerAvatar fail because slot is null");
		}
		VTNetworkManager.NetInstantiateRequest req = VTNetworkManager.NetInstantiate(slot.netEntResourcePath, slot.spawnTransform.position, slot.spawnTransform.rotation);
		while (!req.isReady)
		{
			yield return null;
		}
		Transform transform = req.obj.transform;
		VTOLMPLobbyManager.localPlayerInfo.multicrewAvatar = req.obj;
		transform.parent = slot.spawnTransform;
		transform.localPosition = Vector3.zero;
		transform.localRotation = Quaternion.identity;
		FlightSceneManager.instance.playerActor = GetComponent<Actor>();
		if (Application.isEditor)
		{
			Debug.LogError("TODO: make pilot receiver an interface");
		}
		slot.spawnTransform.GetComponent<AH94PilotReceiver>().ConnectLocal();
		SetOccupant(seatIdx, BDSteamClient.mySteamID);
		SendRPC("RPC_SetOccupant", seatIdx, BDSteamClient.mySteamID);
		PlayerVehicleSetup component = GetComponent<PlayerVehicleSetup>();
		if ((bool)component)
		{
			try
			{
				component.LoadPersistentVehicleData();
			}
			catch (Exception ex)
			{
				VTNetUtils.SendExceptionReport(ex.ToString());
			}
		}
		if ((bool)VTMapGenerator.fetch)
		{
			VTMapGenerator.fetch.StartLODRoutine(transform);
		}
	}

	[VTRPC]
	private void RPC_SetOccupant(int idx, ulong userID)
	{
		SetOccupant(idx, userID);
	}

	private void OnDestroy()
	{
		unity_isDestroyed = true;
		if (localPlayerSeated)
		{
			PlayerVehicleSetup component = GetComponent<PlayerVehicleSetup>();
			if ((bool)component)
			{
				component.SavePersistentData();
			}
		}
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
		if ((bool)VTOLMPSceneManager.instance)
		{
			if (!returningToBriefingRoom && IsLocalPlayerSeated())
			{
				VTOLMPSceneManager.instance.ReturnToBriefingRoom();
			}
			VTOLMPSceneManager.instance.OnWillReturnToBriefingRoom -= Instance_OnWillReturnToBriefingRoom;
		}
		VTOLMPLobbyManager.OnPlayerLeft -= VTOLMPLobbyManager_OnPlayerLeft;
		RemoveOccupantListeners();
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		if (base.isMine)
		{
			SendDirectedRPC(obj, "RPC_SetControlOwner", controlOwner);
			for (int i = 0; i < seatOccupants.Length; i++)
			{
				SendDirectedRPC(obj, "RPC_SetOccupant", i, seatOccupants[i]);
			}
		}
	}

	[VTRPC]
	private void RPC_SendData(Vector3 cubeVec, Vector3 posOffset, Vector3 vel, Vector3 accel, Quaternion rot, Vector3 pyr, Vector3 inputPyr, float brakes, float throttle, int states)
	{
		if (controlOwner != BDSteamClient.mySteamID)
		{
			syncedPos.point = FloatingOrigin.GlobalToWorldPoint(posOffset, Mathf.RoundToInt(cubeVec.x), Mathf.RoundToInt(cubeVec.y), Mathf.RoundToInt(cubeVec.z));
			syncedVel = vel;
			syncedAccel = accel;
			syncedRot = rot;
			syncedInputPyr = inputPyr;
			syncedPyr = pyr;
			syncedBrakes = brakes;
			syncedThrottle = throttle;
			flightInfo.RemoteSetIsLanded((states & 1) == 1);
		}
	}

	[VTRPC]
	private void RPC_SetControlOwner(ulong ctrlId)
	{
		controlOwner = ctrlId;
		if (controlOwner == BDSteamClient.mySteamID)
		{
			rb.isKinematic = false;
			flightInfo.SetRemote(r: false);
			flightInfo.UnpauseGCalculations();
			collisionEffects.enabled = true;
		}
		else
		{
			rb.isKinematic = true;
			flightInfo.SetRemote(r: true);
			collisionEffects.enabled = false;
		}
		this.OnSetControlOwnerID?.Invoke(ctrlId);
	}

	public bool TryTakeControl()
	{
		if (controlOwner == BDSteamClient.mySteamID)
		{
			return true;
		}
		timeLocalRequesting = Time.time;
		SendRPC("RPC_RequestControl", BDSteamClient.mySteamID);
		return false;
	}

	[VTRPC]
	private void RPC_RequestControl(ulong userId)
	{
		if (!lockedOwner && controlOwner == BDSteamClient.mySteamID)
		{
			RPC_SetControlOwner(userId);
			SendRPC("RPC_SetControlOwner", userId);
		}
		else
		{
			timeOtherRequested = Time.time;
		}
	}

	public void SetLockedOwner(bool locked)
	{
		if (controlOwner == BDSteamClient.mySteamID)
		{
			lockedOwner = locked;
		}
	}

	public void SetPitchYawRoll(Vector3 pyr)
	{
		if (IsControlOwner())
		{
			myPyr = pyr;
		}
	}

	public void SetInputPYR(Vector3 pyr)
	{
		if (IsControlOwner())
		{
			myInputPyr = pyr;
		}
	}

	public void SetBrakes(float brakes)
	{
		if (IsControlOwner())
		{
			myBrakes = brakes;
		}
	}

	public void SetThrottle(float t)
	{
		if (IsControlOwner())
		{
			myThrottle = t;
		}
	}

	private void FixedUpdate()
	{
		if (controlOwner == BDSteamClient.mySteamID)
		{
			float currentSendInterval = VTNetworkManager.CurrentSendInterval;
			if (Time.time - lastSendTime > currentSendInterval)
			{
				lastSendTime = Time.time;
				FloatingOrigin.instance.GetCubeShiftVector(out var x, out var y, out var z);
				Vector3 vector = new Vector3(x, y, z);
				int num = 0;
				num |= (flightInfo.isLanded ? 1 : 0);
				SendRPC("RPC_SendData", vector, rb.position, rb.velocity, flightInfo.acceleration, rb.rotation, myPyr, myInputPyr, myBrakes, myThrottle, num);
			}
			syncedPos.point = rb.position;
			syncedVel = rb.velocity;
			syncedAccel = flightInfo.acceleration;
			syncedRot = rb.rotation;
			syncedPyr = myPyr;
			syncedInputPyr = myInputPyr;
			syncedBrakes = myBrakes;
			syncedThrottle = myThrottle;
		}
		else
		{
			SyncPhysics(Time.fixedDeltaTime, rb.position, rb.rotation, out var pos, out var rot);
			rb.MovePosition(pos);
			rb.MoveRotation(rot);
			rb.velocity = syncedVel;
		}
	}

	private void Update()
	{
		if (controlOwner != BDSteamClient.mySteamID)
		{
			myInputPyr = Vector3.Lerp(myInputPyr, syncedInputPyr, 10f * Time.deltaTime);
			this.OnSyncedInputPYR?.Invoke(myInputPyr);
		}
		if (base.isMine && gpsDirty)
		{
			gpsDirty = false;
			SendEntireGPS();
		}
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedPos.point += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		flightInfo.PauseGCalculations();
		flightInfo.OverrideRecordedAcceleration(syncedAccel);
		if (!float.IsNaN(syncedVel.x))
		{
			rb.velocity = syncedVel;
		}
		else
		{
			Debug.LogError("syncedVel is NaN", base.gameObject);
			syncedVel = rb.velocity;
		}
		float magnitude = (syncedPos.point - vector).magnitude;
		float num = Mathf.Lerp(8f, 3f, syncedVel.sqrMagnitude / 6400f);
		Vector3 vector2 = Vector3.MoveTowards(vector, syncedPos.point, Mathf.Max(magnitude * num, magnitude * 3f) * deltaTime);
		currCorrectionDist = Mathf.MoveTowards(currCorrectionDist, targetCorrectionDist, 100f * deltaTime);
		if ((magnitude > 1f && flightInfo.airspeed < 4f) || magnitude > currCorrectionDist)
		{
			Debug.LogFormat("Resetting sync tf! Airspeed: {0}, dist: {1}", flightInfo.airspeed, magnitude);
			Vector3 vector3 = (pos = (rb.position = syncedPos.point));
			_ = Color.yellow;
		}
		else if (interpolatingPos)
		{
			pos = vector2;
			if (magnitude < interpThreshold * 0.33f)
			{
				interpolatingPos = false;
			}
			_ = Color.red;
		}
		else
		{
			pos = vector;
			if (magnitude > interpThreshold)
			{
				interpolatingPos = true;
			}
			_ = Color.green;
		}
		rot = Quaternion.Lerp(currRot, syncedRot, 10f * deltaTime);
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, flightInfo.airspeed / interpSpeedDiv);
	}

	public void TakeWeaponControl()
	{
		Debug.Log("Locally taking weapon controls");
		weaponControllerId = BDSteamClient.mySteamID;
		if (wm.isFiring)
		{
			wm.EndFire();
		}
		this.OnSetWeaponControllerId?.Invoke(weaponControllerId);
		SendRPCToCopilots(this, "RPC_SetWpnController", BDSteamClient.mySteamID);
	}

	[VTRPC]
	private void RPC_SetWpnController(ulong id)
	{
		Debug.Log($"RPC_SetWpnController({id})");
		if (IsLocalWeaponController() && wm.isFiring)
		{
			wm.EndFire();
		}
		weaponControllerId = id;
		this.OnSetWeaponControllerId?.Invoke(id);
	}

	public bool IsLocalWeaponController()
	{
		return weaponControllerId == BDSteamClient.mySteamID;
	}

	private void WeaponChanged()
	{
		if (wm.isMasterArmed != wasArmed)
		{
			wasArmed = wm.isMasterArmed;
			SendRPCToCopilots(this, "RPC_SetArm", wm.isMasterArmed ? 1 : 0);
		}
		if (IsLocalWeaponController() && wm.isMasterArmed && (bool)wm.currentEquip)
		{
			SendRPCToCopilots(this, "RPC_SetEq", wm.currentEquip.hardpointIdx);
		}
	}

	[VTRPC]
	private void RPC_SetEq(int idx)
	{
		wm.SetWeapon(idx);
		string text = string.Empty;
		if ((bool)wm.currentEquip)
		{
			text = wm.currentEquip.shortName;
		}
		if (text != lastEqShortname)
		{
			lastEqShortname = text;
			wm.OnUserCycledWeapon?.Invoke();
		}
	}

	[VTRPC]
	private void RPC_SetArm(int a)
	{
		bool flag = a > 0;
		if (flag != wm.isMasterArmed)
		{
			wasArmed = flag;
			wm.ui.UIToggleMasterArmed();
		}
	}

	private void Wm_OnEquipArmingChanged(HPEquippable eq, bool armed)
	{
		SendRPCToCopilots(this, "RPC_SetEqArmed", eq.hardpointIdx, armed ? 1 : 0);
	}

	[VTRPC]
	private void RPC_SetEqArmed(int eqIdx, int i_armed)
	{
		Debug.Log($"RPC_SetEqArmed({eqIdx}, {i_armed})");
		HPEquippable equip = wm.GetEquip(eqIdx);
		if ((bool)equip)
		{
			equip.armed = i_armed > 0;
			wm.RefreshWeapon();
			wm.ui.UpdateDisplay();
		}
	}

	private void Wm_OnEquipJettisonChanged(HPEquippable eq, bool state)
	{
		SendRPCToCopilots(this, "RPC_EqMarkJett", eq.hardpointIdx, state ? 1 : 0);
	}

	[VTRPC]
	private void RPC_EqMarkJett(int eqIdx, int j)
	{
		HPEquippable equip = wm.GetEquip(eqIdx);
		if ((bool)equip && equip.jettisonable)
		{
			equip.markedForJettison = j > 0;
			wm.RefreshWeapon();
			wm.ui.UpdateDisplay();
		}
	}

	public void RemoteJettisonItems()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_JettisonMarked");
	}

	[VTRPC]
	private void RPC_JettisonMarked()
	{
		if (base.isMine)
		{
			wm.ui.JettisonMarkedItems();
		}
	}

	private void Wm_OnRippleChanged(int equipIdx, int rippleRateIdx)
	{
		SendRPCToCopilots(this, "RPC_Ripple", equipIdx, rippleRateIdx);
	}

	[VTRPC]
	private void RPC_Ripple(int eqIdx, int rippleRateIdx)
	{
		HPEquippable equip = wm.GetEquip(eqIdx);
		if (!equip)
		{
			return;
		}
		IRippleWeapon rippleWeapon = (IRippleWeapon)equip;
		for (int i = 0; i < 100; i++)
		{
			if (rippleWeapon.GetRippleRateIdx() == rippleRateIdx)
			{
				return;
			}
			wm.CycleRippleRates(eqIdx, sendEvent: false);
		}
		wm.ui.UpdateDisplay();
	}

	private void TGPMfdPage_OnSetSOI(bool isSoi)
	{
		if (isSoi)
		{
			tgpPage.remoteOnly = false;
			if (tgpControllerId != BDSteamClient.mySteamID)
			{
				RPC_SetTGPOwner(BDSteamClient.mySteamID);
				SendRPCToCopilots(this, "RPC_SetTGPOwner", BDSteamClient.mySteamID);
				if (tgpPage.tgpMode == TargetingMFDPage.TGPModes.HEAD)
				{
					tgpPage.MFDHeadButton();
				}
			}
		}
		else if (tgpControllerId == BDSteamClient.mySteamID && base.netEntity.ownerID != BDSteamClient.mySteamID)
		{
			RPC_SetTGPOwner(base.netEntity.ownerID);
			SendRPCToCopilots(this, "RPC_SetTGPOwner", base.netEntity.ownerID);
			if (!base.isMine)
			{
				tgpPage.remoteOnly = true;
			}
		}
	}

	public bool IsLocalTGPControl()
	{
		return tgpControllerId == BDSteamClient.mySteamID;
	}

	public void TakeTGPControl()
	{
		tgpPage.remoteOnly = false;
		tgpControllerId = BDSteamClient.mySteamID;
		this.OnPilotTakeTGP?.Invoke(tgpControllerId);
		SendRPCToCopilots(this, "RPC_SetTGPOwner", BDSteamClient.mySteamID);
	}

	[VTRPC]
	private void RPC_SetTGPOwner(ulong id)
	{
		tgpControllerId = id;
		if (tgpControllerId == BDSteamClient.mySteamID)
		{
			tgpPage.remoteOnly = false;
		}
		else
		{
			tgpPage.remoteOnly = true;
			if (tgpPage.tgpMode == TargetingMFDPage.TGPModes.HEAD)
			{
				tgpPage.DisableHeadMode();
			}
			if ((bool)tgpPage.mfdPage.mfd && tgpPage.isSOI)
			{
				tgpPage.mfdPage.ToggleInput();
			}
		}
		this.OnPilotTakeTGP?.Invoke(tgpControllerId);
	}

	private void TgpPage_OnTGPPwrButton(bool p)
	{
		SendRPCToCopilots(this, "RPC_TGPPwr", p ? 1 : 0);
	}

	[VTRPC]
	private void RPC_TGPPwr(int _p)
	{
		bool p = _p > 0;
		tgpPage.RemoteSetPower(p);
	}

	private void TgpPage_OnSetMode(TargetingMFDPage.TGPModes tgpMode)
	{
		SendRPCToCopilots(this, "RPC_TGPMode", (int)tgpMode);
	}

	[VTRPC]
	private void RPC_TGPMode(int m)
	{
		tgpPage.RemoteSetMode((TargetingMFDPage.TGPModes)m);
	}

	private void TgpPage_OnSetFov(int fovIdx)
	{
		if (!tgpPage.remoteOnly)
		{
			SendRPCToCopilots(this, "RPC_TGPFov", fovIdx);
		}
	}

	[VTRPC]
	private void RPC_TGPFov(int fovIdx)
	{
		tgpPage.RemoteSetFovIdx(fovIdx);
	}

	private void TgpPage_OnSetSensorMode(TargetingMFDPage.SensorModes obj)
	{
		if (!tgpPage.remoteOnly)
		{
			SendRPCToCopilots(this, "RPC_TGPSens", (int)obj);
		}
	}

	[VTRPC]
	private void RPC_TGPSens(int i_mode)
	{
		tgpPage.RemoteSetSensorMode((TargetingMFDPage.SensorModes)i_mode);
	}

	private void CommsPage_OnRequestingRearming()
	{
		if (base.isMine)
		{
			SendRPCToCopilots(this, "RPC_OwnerRearming", 1);
			RPC_SetControlOwner(base.netEntity.ownerID);
			SendRPC("RPC_SetControlOwner", base.netEntity.ownerID);
			SetLockedOwner(locked: true);
		}
	}

	private void OnBeginRearming()
	{
	}

	private void OnEndRearming()
	{
		if (base.isMine)
		{
			SetLockedOwner(locked: false);
			SendRPCToCopilots(this, "RPC_OwnerRearming", 0);
		}
	}

	[VTRPC]
	private void RPC_OwnerRearming(int r)
	{
		bool flag = r > 0;
		if (LocalPlayerSeatIdx() <= 0)
		{
			return;
		}
		if (flag && IsControlOwner())
		{
			foreach (VRInteractable controlInteractable in controlInteractables)
			{
				if ((bool)controlInteractable.activeController)
				{
					controlInteractable.activeController.ReleaseFromInteractable();
				}
			}
		}
		VRInteractable[] array = disableInteractableOnRearming;
		foreach (VRInteractable vRInteractable in array)
		{
			if ((bool)vRInteractable)
			{
				vRInteractable.enabled = !flag;
			}
		}
		LeverLockInfo[] array2 = lockLeversOnRearming;
		for (int i = 0; i < array2.Length; i++)
		{
			LeverLockInfo leverLockInfo = array2[i];
			if ((bool)leverLockInfo.lever)
			{
				if (flag)
				{
					leverLockInfo.lever.LockTo(leverLockInfo.lockTo);
				}
				else
				{
					leverLockInfo.lever.Unlock();
				}
			}
		}
	}

	public void RemoteStartCM()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_CMStart");
	}

	public void RemoteStopCM()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_CMStop");
	}

	[VTRPC]
	private void RPC_CMStart()
	{
		cmm.FireCM();
	}

	[VTRPC]
	private void RPC_CMStop()
	{
		cmm.StopFireCM();
	}

	private void OnOwnerCmCountsChanged()
	{
		int leftCount = cmm.chaffCMs[0].leftCount;
		int rightCount = cmm.chaffCMs[0].rightCount;
		int num = (leftCount << 16) | rightCount;
		int leftCount2 = cmm.flareCMs[0].leftCount;
		int rightCount2 = cmm.flareCMs[0].rightCount;
		int num2 = (leftCount2 << 16) | rightCount2;
		SendRPCToCopilots(this, "RPC_CMCounts", num, num2);
	}

	[VTRPC]
	private void RPC_CMCounts(int chaff, int flare)
	{
		int num = chaffCountTotal;
		int num2 = flareCountTotal;
		int num3 = 65535;
		chaffCountL = (chaff & (num3 << 16)) >> 16;
		chaffCountR = chaff & num3;
		flareCountL = (flare & (num3 << 16)) >> 16;
		flareCountR = flare & num3;
		this.OnCMCountsUpdated?.Invoke();
		if (num > chaffCountTotal)
		{
			cmm.AnnounceChaff();
		}
		if (num2 > flareCountTotal)
		{
			cmm.AnnounceFlare();
		}
	}

	private void OnSetReleaseMode(CountermeasureManager.ReleaseModes mode)
	{
		if (ignoreNextCMReleaseMode)
		{
			ignoreNextCMReleaseMode = false;
			return;
		}
		SendRPCToCopilots(this, "RPC_CMMode", (int)mode);
	}

	[VTRPC]
	private void RPC_CMMode(int m)
	{
		if (cmm.releaseMode != (CountermeasureManager.ReleaseModes)m)
		{
			ignoreNextCMReleaseMode = true;
			cmm.SetReleaseMode((CountermeasureManager.ReleaseModes)m);
		}
	}

	private void Cmm_OnReleaseRateChanged()
	{
		if (ignoreNextRelRate)
		{
			ignoreNextRelRate = false;
			return;
		}
		SendRPCToCopilots(this, "RPC_CMRelIdx", cmm.releaseRateIdx);
	}

	[VTRPC]
	private void RPC_CMRelIdx(int r)
	{
		if (r != cmm.releaseRateIdx)
		{
			ignoreNextRelRate = true;
			cmm.SetReleaseRateIdx(r);
			this.OnReleaseRateChanged?.Invoke();
		}
	}

	private void Instance_OnSetWaypoint(Waypoint obj)
	{
		if (obj != null)
		{
			SendRPCToCopilots(this, "RPC_Wpt", obj.id);
		}
		else
		{
			SendRPCToCopilots(this, "RPC_Wpt", -1);
		}
	}

	[VTRPC]
	private void RPC_Wpt(int wpIdx)
	{
		Debug.Log($"RPC_Wpt({wpIdx})");
		Waypoint waypoint = VTScenario.current.waypoints.GetWaypoint(wpIdx);
		WaypointManager.instance.RemoteSetWaypoint(waypoint);
	}

	private void Instance_OnSetUnknownWaypoint(FixedPoint obj)
	{
		SendRPCToCopilots(this, "RPC_WptUnk", obj.globalPoint.toVector3);
	}

	[VTRPC]
	private void RPC_WptUnk(Vector3 gp)
	{
		Debug.Log($"RPC_WptUnk({gp})");
		FixedPoint fixedPoint = default(FixedPoint);
		fixedPoint.globalPoint = new Vector3D(gp);
		WaypointManager.instance.RemoteSetWaypoint(fixedPoint);
	}

	private void Instance_OnSetGPSWaypoint(GPSTargetGroup g, int index)
	{
		Debug.Log($"OnSetGPSWaypoint({g.groupName}, {index})");
		int num = wm.gpsSystem.groupNames.IndexOf(g.groupName);
		SendRPCToCopilots(this, "RPC_WptGPS", num, index);
	}

	[VTRPC]
	private void RPC_WptGPS(int gIdx, int tIdx)
	{
		Debug.Log($"RPC_WptGPS({gIdx}, {tIdx})");
		if (gIdx >= 0)
		{
			wm.gpsSystem.SetCurrentGroup(gIdx);
			wm.gpsSystem.currentGroup.currentTargetIdx = tIdx;
			WaypointManager.instance.SetWaypointGPS(wm.gpsSystem.currentGroup, sendEvt: false);
		}
	}

	private void Instance_OnSetActorWaypoint(Actor a)
	{
		int actorIdentifier = VTNetUtils.GetActorIdentifier(a);
		SendRPCToCopilots(this, "RPC_WptActor", actorIdentifier);
	}

	[VTRPC]
	private void RPC_WptActor(int actorId)
	{
		Actor actorFromIdentifier = VTNetUtils.GetActorFromIdentifier(actorId);
		WaypointManager.instance.RemoteSetWaypoint(actorFromIdentifier);
	}

	private void SendEntireGPS()
	{
		if (base.isMine)
		{
			SendRPCToCopilots(this, "RPC_ClearGPS");
		}
		if (wm.gpsSystem.noGroups)
		{
			return;
		}
		foreach (string groupName in wm.gpsSystem.groupNames)
		{
			GPSTargetGroup gPSTargetGroup = wm.gpsSystem.targetGroups[groupName];
			if (gPSTargetGroup.targets.Count > 0)
			{
				for (int i = 0; i < gPSTargetGroup.targets.Count; i++)
				{
					SendGPSTarget(gPSTargetGroup, i);
				}
			}
			else
			{
				int num = VTNetUtils.Encode3CharString(gPSTargetGroup.denom);
				SendRPCToCopilots(this, "RPC_GPSEmptyGrp", num, gPSTargetGroup.numeral);
			}
		}
		GPSTargetGroup currentGroup = wm.gpsSystem.currentGroup;
		SendRPCToCopilots(this, "RPC_SetGPSTgt", VTNetUtils.Encode3CharString(currentGroup.denom), currentGroup.numeral, currentGroup.currentTargetIdx);
	}

	private void SendGPSTarget(GPSTargetGroup g, int idx)
	{
		int num = VTNetUtils.Encode3CharString(g.denom);
		GPSTarget gPSTarget = g.targets[idx];
		int num2 = VTNetUtils.Encode3CharString(gPSTarget.denom);
		Vector3 toVector = VTMapManager.WorldToGlobalPoint(gPSTarget.worldPosition).toVector3;
		SendRPCToCopilots(this, "RPC_AddGPSTgt", num, g.numeral, toVector, num2, gPSTarget.numeral);
	}

	[VTRPC]
	private void RPC_ClearGPS()
	{
		Debug.Log("RPC_ClearGPS()");
		int num = 0;
		while (!wm.gpsSystem.noGroups)
		{
			wm.gpsSystem.RemoveCurrentGroup(sendIfRemote: false);
			num++;
			if (num > 1000)
			{
				Debug.LogError(" - It took way too long! There must have been an issue removing groups...");
				break;
			}
		}
	}

	[VTRPC]
	private void RPC_AddGPSTgt(int grpDenom, int grpNum, Vector3 globalPoint, int tgtDenom, int tgtNum)
	{
		string denom = VTNetUtils.Decode3CharString(grpDenom);
		GPSTargetGroup gPSTargetGroup = wm.gpsSystem.CreateGroup(denom, grpNum);
		Vector3 worldPosition = VTMapManager.GlobalToWorldPoint(new Vector3D(globalPoint));
		gPSTargetGroup.AddTarget(new GPSTarget(worldPosition, VTNetUtils.Decode3CharString(tgtDenom), tgtNum));
	}

	[VTRPC]
	private void RPC_SetGPSTgt(int grpDenom, int grpNum, int tgtIdx)
	{
		string arg = VTNetUtils.Decode3CharString(grpDenom);
		string text = $"{arg} {grpNum}";
		Debug.Log($"RPC_SetGPSTgt({text} #{tgtIdx}");
		if (wm.gpsSystem.SetCurrentGroup(text))
		{
			wm.gpsSystem.currentGroup.currentTargetIdx = tgtIdx;
			wm.gpsSystem.TargetsChanged();
		}
	}

	[VTRPC]
	private void RPC_GPSEmptyGrp(int grpDenom, int grpNum)
	{
		string text = VTNetUtils.Decode3CharString(grpDenom);
		string text2 = $"{text} {grpNum}";
		Debug.Log("RPC_GPSEmptyGrp(" + text2 + ")");
		wm.gpsSystem.CreateGroup(text, grpNum);
	}

	public void RemoteGPS_AddTarget(string tgtDenom, Vector3D globalPoint)
	{
		int num = VTNetUtils.Encode3CharString(tgtDenom);
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_AddTarget", num, globalPoint.toVector3);
	}

	[VTRPC]
	private void RPC_G_AddTarget(int tgtDenom, Vector3 globalPoint)
	{
		string prefix = VTNetUtils.Decode3CharString(tgtDenom);
		if (wm.gpsSystem.currentGroup == null)
		{
			wm.gpsSystem.CreateCustomGroup(sendIfRemote: false);
		}
		Vector3 worldPosition = VTMapManager.GlobalToWorldPoint(new Vector3D(globalPoint));
		wm.gpsSystem.AddTarget(worldPosition, prefix);
	}

	public void RemoteGPS_CreateGroup()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_CreateGrp");
	}

	[VTRPC]
	private void RPC_G_CreateGrp()
	{
		gpsPage.CreateCustomGroup();
	}

	public void RemoteGPS_DeleteGroup()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_DelGrp");
	}

	[VTRPC]
	private void RPC_G_DelGrp()
	{
		gpsPage.DeleteCurrentGroup();
	}

	public void RemoteGPS_DeleteTarget()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_DelTgt");
	}

	[VTRPC]
	private void RPC_G_DelTgt()
	{
		gpsPage.DeleteCurrentTarget();
	}

	public void RemoteGPS_MoveTgtUp()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_MovUp");
	}

	[VTRPC]
	private void RPC_G_MovUp()
	{
		gpsPage.MoveTargetUp();
	}

	public void RemoteGPS_MoveTgtDown()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_MovDn");
	}

	[VTRPC]
	private void RPC_G_MovDn()
	{
		gpsPage.MoveTargetDown();
	}

	public void RemoteGPS_SetWP()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_SetWP");
	}

	[VTRPC]
	private void RPC_G_SetWP()
	{
		gpsPage.SetWaypoint();
	}

	public void RemoteGPS_Share()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_Share");
	}

	[VTRPC]
	private void RPC_G_Share()
	{
		gpsPage.ShareCurrentGroup();
	}

	public void RemoteGPS_NextTgt()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_NextTgt");
	}

	[VTRPC]
	private void RPC_G_NextTgt()
	{
		gpsPage.NextTarget();
	}

	public void RemoteGPS_PrevTgt()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_PrevTgt");
	}

	[VTRPC]
	private void RPC_G_PrevTgt()
	{
		gpsPage.PreviousTarget();
	}

	public void RemoteGPS_NextGrp()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_NextGrp");
	}

	[VTRPC]
	private void RPC_G_NextGrp()
	{
		gpsPage.NextGroup();
	}

	public void RemoteGPS_PrevGrp()
	{
		SendDirectedRPC(base.netEntity.ownerID, "RPC_G_PrevGrp");
	}

	[VTRPC]
	private void RPC_G_PrevGrp()
	{
		gpsPage.PreviousGroup();
	}

	private void SetDoorAudio()
	{
		if (vrDoors == null)
		{
			vrDoors = GetComponentsInChildren<VRDoor>(includeInactive: true);
		}
		bool affectAudio = IsLocalPlayerSeated() || !VTOLMPUtils.IsMultiplayer();
		VRDoor[] array = vrDoors;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetAffectAudio(affectAudio);
		}
	}

	private void SetupVehiclePartEvents()
	{
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			int partIdx = i;
			if (base.isMine)
			{
				vehicleParts[i].OnPartDetach.AddListener(delegate
				{
					SendDetachRPC(partIdx, 0uL);
				});
				vehicleParts[i].health.OnDeath.AddListener(delegate
				{
					SendKillPartRPC(partIdx, 0uL);
				});
				vehicleParts[i].OnRepair.AddListener(delegate
				{
					SendPartRepairRPC(partIdx);
				});
			}
			else
			{
				vehicleParts[i].detachOnDeath = false;
			}
		}
	}

	private void SendPartRepairRPC(int partIdx)
	{
		SendRPC("RPC_PartRepair", partIdx);
	}

	[VTRPC]
	private void RPC_PartRepair(int idx)
	{
		vehicleParts[idx].Repair();
	}

	public void SendKillPartRPC(int partIdx, ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_PartKill", partIdx);
	}

	[VTRPC]
	private void RPC_PartKill(int idx)
	{
		vehicleParts[idx].RemoteKill(null);
	}

	public void SendDetachRPC(int partIdx, ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_PartDetach", partIdx);
	}

	[VTRPC]
	private void RPC_PartDetach(int idx)
	{
		vehicleParts[idx].RemoteDetachPart();
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			if (vehicleParts[i].health.normalizedHealth == 0f)
			{
				SendKillPartRPC(i, target);
			}
			if (vehicleParts[i].hasDetached)
			{
				SendDetachRPC(i, target);
			}
		}
	}
}

}