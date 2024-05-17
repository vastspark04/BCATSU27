using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTOLVR.Multiplayer;

public class ReArmingPoint : MonoBehaviour
{
	public Teams team;

	public static List<ReArmingPoint> reArmingPoints = new List<ReArmingPoint>();

	public float radius;

	public bool canArm = true;

	public bool canRefuel = true;

	private static List<GroundCrewVoiceProfile> voiceProfiles = null;

	private static int nextProfileIdx = 0;

	private LoadoutConfigurator config;

	private Transform camRigParent;

	private GameObject configObject;

	private PlayerVehicleSetup vehicleSetup;

	private static List<Actor> checkBuffer = new List<Actor>();

	private HPEquippable[] mp_OrigEquips;

	public static ReArmingPoint active { get; private set; }

	public AirportManager.ParkingSpace parkingSpace { get; set; }

	public GroundCrewVoiceProfile voiceProfile { get; private set; }

	public event Action OnEndRearm;

	private static GroundCrewVoiceProfile GetNextVoice()
	{
		if (voiceProfiles == null)
		{
			voiceProfiles = VTResources.GetAllGroundCrewVoices();
		}
		GroundCrewVoiceProfile result = voiceProfiles[nextProfileIdx];
		nextProfileIdx = (nextProfileIdx + 1) % voiceProfiles.Count;
		return result;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}

	private void OnEnable()
	{
		reArmingPoints.Add(this);
	}

	private void OnDisable()
	{
		if (active == this)
		{
			active = null;
		}
		reArmingPoints.Remove(this);
	}

	private void Start()
	{
		voiceProfile = GetNextVoice();
	}

	private IEnumerator BeginRearmRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 0.5f, fadeoutVolume: false);
		yield return new WaitForSeconds(0.7f);
		FinalBeginReArm();
		ScreenFader.FadeIn(0.5f);
		ControllerEventHandler.UnpauseEvents();
	}

	public void BeginReArm()
	{
		if ((bool)active)
		{
			Debug.Log("Attempted to begin rearming but a station is already active.", active ? active.gameObject : null);
			return;
		}
		active = this;
		FlightSceneManager.instance.OnExitScene += Instance_OnExitScene;
		StartCoroutine(BeginRearmRoutine());
	}

	public bool CheckIsClear(Actor requestingActor)
	{
		if (parkingSpace != null && parkingSpace.occupiedBy != null && parkingSpace.occupiedBy != requestingActor)
		{
			return false;
		}
		Actor.GetActorsInRadius(base.transform.position, radius, Teams.Allied, TeamOptions.BothTeams, checkBuffer);
		for (int i = 0; i < checkBuffer.Count; i++)
		{
			if ((bool)checkBuffer[i] && checkBuffer[i] != requestingActor && checkBuffer[i].finalCombatRole == Actor.Roles.Air)
			{
				return false;
			}
		}
		return true;
	}

	private void Instance_OnExitScene()
	{
		active = null;
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= Instance_OnExitScene;
		}
	}

	private void FinalBeginReArm()
	{
		AudioController.instance.SetExteriorOpening("rearming", 1f);
		PlayerVehicle currentVehicle = PilotSaveManager.currentVehicle;
		Transform transform = base.transform;
		GameObject gameObject = FlightSceneManager.instance.playerActor.gameObject;
		Vector3 vector = Vector3.zero;
		if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out var hitInfo, 100f, 1))
		{
			vector = transform.InverseTransformPoint(hitInfo.point);
		}
		FlightSceneManager.instance.playerActor.flightInfo.PauseGCalculations();
		gameObject.transform.position = transform.TransformPoint(currentVehicle.playerSpawnOffset + vector);
		gameObject.transform.rotation = Quaternion.AngleAxis(currentVehicle.spawnPitch, transform.right) * transform.rotation;
		gameObject.GetComponent<PlayerVehicleSetup>().LandVehicle(transform);
		WeaponManager component = gameObject.GetComponent<WeaponManager>();
		if (VTOLMPUtils.IsMultiplayer())
		{
			if (mp_OrigEquips == null || mp_OrigEquips.Length != component.equipCount)
			{
				mp_OrigEquips = new HPEquippable[component.equipCount];
			}
			for (int i = 0; i < component.equipCount; i++)
			{
				mp_OrigEquips[i] = component.GetEquip(i);
			}
		}
		camRigParent = VRHead.instance.transform.parent.parent;
		gameObject.SetActive(value: true);
		EjectionSeat componentInChildren = gameObject.GetComponentInChildren<EjectionSeat>();
		if ((bool)componentInChildren)
		{
			componentInChildren.pilotModel.SetActive(value: false);
		}
		VTOLQuickStart componentInChildren2 = gameObject.GetComponentInChildren<VTOLQuickStart>();
		if ((bool)componentInChildren2.throttle)
		{
			componentInChildren2.throttle.RemoteSetThrottle(0f);
		}
		componentInChildren2.quickStopComponents.ApplySettings();
		vehicleSetup = gameObject.GetComponentInChildren<PlayerVehicleSetup>();
		if ((bool)vehicleSetup && vehicleSetup.OnBeginRearming != null)
		{
			vehicleSetup.OnBeginRearming.Invoke();
		}
		configObject = UnityEngine.Object.Instantiate(currentVehicle.loadoutConfiguratorPrefab);
		configObject.transform.parent = transform;
		configObject.transform.position = transform.position;
		configObject.transform.rotation = transform.rotation;
		configObject.SetActive(value: true);
		config = configObject.GetComponent<LoadoutConfigurator>();
		config.wm = component;
		config.canRefuel = canRefuel;
		config.canArm = canArm;
		if ((bool)config.equipRigTf)
		{
			float z = currentVehicle.playerSpawnOffset.z - currentVehicle.loadoutSpawnOffset.z;
			Vector3 localPosition = config.equipRigTf.localPosition;
			localPosition += new Vector3(0f, 0f, z);
			localPosition.y = 0f;
			config.equipRigTf.localPosition = localPosition;
		}
		if ((bool)vehicleSetup)
		{
			vehicleSetup.StartUsingConfigurator(config);
		}
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller.activeInteractable)
			{
				controller.ReleaseFromInteractable();
			}
		}
		VRHead.instance.transform.parent.parent = config.seatTransform;
		VRHead.instance.transform.parent.localPosition = VRHead.playAreaPosition;
		VRHead.instance.transform.parent.localRotation = VRHead.playAreaRotation;
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
		config.availableEquipStrings = new List<string>();
		_ = campaignSave.availableWeapons;
		if (VTOLMPUtils.IsMultiplayer())
		{
			PlayerInfo localPlayer = VTOLMPSceneManager.instance.localPlayer;
			List<string> equipment = VTOLMPSceneManager.instance.GetMPSpawn(localPlayer.team, localPlayer.selectedSlot).equipment.equipment;
			foreach (GameObject allEquipPrefab in PilotSaveManager.currentVehicle.allEquipPrefabs)
			{
				if (!equipment.Contains(allEquipPrefab.gameObject.name))
				{
					config.availableEquipStrings.Add(allEquipPrefab.gameObject.name);
				}
			}
		}
		else
		{
			foreach (string availableWeapon in campaignSave.availableWeapons)
			{
				if (!(VTScenario.current.gameVersion > new GameVersion(1, 3, 0, 30, GameVersion.ReleaseTypes.Testing)) || VTScenario.current.allowedEquips.Contains(availableWeapon))
				{
					config.availableEquipStrings.Add(availableWeapon);
				}
			}
		}
		config.Initialize(campaignSave, useMidflightEquips: true);
		if (PilotSaveManager.currentScenario.forcedEquips != null)
		{
			CampaignScenario.ForcedEquip[] forcedEquips = PilotSaveManager.currentScenario.forcedEquips;
			foreach (CampaignScenario.ForcedEquip forcedEquip in forcedEquips)
			{
				config.Attach(forcedEquip.weaponName, forcedEquip.hardpointIdx);
				config.lockedHardpoints.Add(forcedEquip.hardpointIdx);
			}
		}
		config.UpdateNodes();
		StartCoroutine(SetRearmAnchorDelayed());
	}

	private IEnumerator SetRearmAnchorDelayed()
	{
		yield return new WaitForSeconds(1f);
		if (active == this)
		{
			RaySpringDamper[] suspensions = FlightSceneManager.instance.playerActor.flightInfo.wheelsController.suspensions;
			for (int i = 0; i < suspensions.Length; i++)
			{
				suspensions[i].SetRearmAnchor();
			}
		}
	}

	public bool EndReArm()
	{
		float totalFlightCost = config.GetTotalFlightCost();
		float num = PilotSaveManager.currentScenario.totalBudget - PilotSaveManager.currentScenario.initialSpending - PilotSaveManager.currentScenario.inFlightSpending;
		if (!PilotSaveManager.currentScenario.isTraining && totalFlightCost > num)
		{
			Debug.Log("Over budget midflight arming");
			config.DenyLaunch(VTLStaticStrings.vehicleConfig_denyOverBudget);
			return false;
		}
		RaySpringDamper[] suspensions = FlightSceneManager.instance.playerActor.flightInfo.wheelsController.suspensions;
		for (int i = 0; i < suspensions.Length; i++)
		{
			suspensions[i].ReleaseRearmAnchor();
		}
		StartCoroutine(EndRearmRoutine());
		return true;
	}

	private IEnumerator EndRearmRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 0.5f, fadeoutVolume: false);
		yield return new WaitForSeconds(0.7f);
		FinalEndReArm();
		ControllerEventHandler.UnpauseEvents();
		ScreenFader.FadeIn(0.5f);
	}

	private void FinalEndReArm()
	{
		float totalFlightCost = config.GetTotalFlightCost();
		AudioController.instance.SetExteriorOpening("rearming", 0f);
		PilotSaveManager.currentScenario.inFlightSpending += totalFlightCost;
		VRHead.instance.transform.parent.parent = camRigParent;
		VRHead.instance.transform.parent.localPosition = VRHead.playAreaPosition;
		VRHead.instance.transform.parent.localRotation = VRHead.playAreaRotation;
		GameObject gameObject = FlightSceneManager.instance.playerActor.gameObject;
		EjectionSeat componentInChildren = gameObject.GetComponentInChildren<EjectionSeat>();
		if ((bool)componentInChildren)
		{
			componentInChildren.pilotModel.SetActive(value: true);
		}
		CommRadioSource componentInChildren2 = gameObject.GetComponentInChildren<CommRadioSource>();
		if ((bool)componentInChildren2)
		{
			componentInChildren2.SetAsRadioSource();
		}
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if ((bool)controller.activeInteractable)
			{
				controller.ReleaseFromInteractable();
			}
		}
		UnityEngine.Object.Destroy(configObject);
		active = null;
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= Instance_OnExitScene;
		}
		if ((bool)vehicleSetup)
		{
			if (vehicleSetup.OnEndRearming != null)
			{
				vehicleSetup.OnEndRearming.Invoke();
			}
			vehicleSetup.EndUsingConfigurator(config);
		}
		if (this.OnEndRearm != null)
		{
			this.OnEndRearm();
		}
		voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.ReturnedToVehicle);
		FlightSceneManager.instance.playerActor.flightInfo.UnpauseGCalculations();
		if (!VTOLMPUtils.IsMultiplayer())
		{
			return;
		}
		WeaponManagerSync componentInChildren3 = gameObject.GetComponentInChildren<WeaponManagerSync>();
		Loadout loadout = new Loadout();
		loadout.cmLoadout = new int[2] { 9999, 9999 };
		loadout.normalizedFuel = gameObject.GetComponent<FuelTank>().fuelFraction;
		loadout.hpLoadout = new string[componentInChildren3.wm.equipCount];
		for (int i = 0; i < componentInChildren3.wm.equipCount; i++)
		{
			if (componentInChildren3.wm.GetEquip(i) != mp_OrigEquips[i])
			{
				loadout.hpLoadout[i] = componentInChildren3.wm.GetEquip(i).gameObject.name;
			}
		}
		componentInChildren3.NetEquipWeapons(loadout, additive: true);
	}
}
