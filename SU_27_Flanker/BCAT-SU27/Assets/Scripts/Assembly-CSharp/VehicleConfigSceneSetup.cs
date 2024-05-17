using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleConfigSceneSetup : MonoBehaviour
{
	public Transform loadoutSpawnTransform;

	private LoadoutConfigurator config;

	public VehicleConfigScenarioUI configScenarioUI;

	private IEnumerator Start()
	{
		if (PilotSaveManager.currentVehicle == null)
		{
			LoadingSceneController.LoadSceneImmediate("ReadyRoom");
			yield break;
		}
		yield return null;
		PilotSaveManager.LoadPilotsFromFile();
		PlayerVehicle currentVehicle = PilotSaveManager.currentVehicle;
		GameObject gameObject = Object.Instantiate(currentVehicle.vehiclePrefab);
		gameObject.transform.position = loadoutSpawnTransform.TransformPoint(currentVehicle.loadoutSpawnOffset);
		gameObject.transform.rotation = Quaternion.AngleAxis(currentVehicle.spawnPitch, loadoutSpawnTransform.right) * loadoutSpawnTransform.rotation;
		PlayerVehicleSetup component = gameObject.GetComponent<PlayerVehicleSetup>();
		component.SetToConfigurationState();
		WheelsController component2 = gameObject.GetComponent<WheelsController>();
		if ((bool)component2)
		{
			component2.SetBrakeLock(1);
		}
		gameObject.SetActive(value: true);
		GameObject gameObject2 = Object.Instantiate(currentVehicle.loadoutConfiguratorPrefab);
		gameObject2.transform.position = loadoutSpawnTransform.position;
		gameObject2.transform.rotation = loadoutSpawnTransform.rotation;
		gameObject2.SetActive(value: true);
		config = gameObject2.GetComponent<LoadoutConfigurator>();
		config.wm = gameObject.GetComponent<WeaponManager>();
		RaySpringDamper[] componentsInChildren = gameObject.GetComponentsInChildren<RaySpringDamper>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].SetRearmAnchor();
		}
		component.StartUsingConfigurator(config);
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(PilotSaveManager.currentVehicle.vehicleName).GetCampaignSave(PilotSaveManager.currentCampaign.campaignID);
		config.availableEquipStrings = new List<string>();
		if (PilotSaveManager.currentCampaign.isCustomScenarios && PilotSaveManager.currentCampaign.isStandaloneScenarios)
		{
			List<string> allowedEquips = VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign).allowedEquips;
			foreach (string item in allowedEquips)
			{
				config.availableEquipStrings.Add(item);
			}
			if (campaignSave.currentWeapons != null)
			{
				for (int j = 0; j < campaignSave.currentWeapons.Length; j++)
				{
					if (!allowedEquips.Contains(campaignSave.currentWeapons[j]))
					{
						campaignSave.currentWeapons[j] = string.Empty;
					}
				}
			}
		}
		else
		{
			foreach (string availableWeapon in campaignSave.availableWeapons)
			{
				if (!(VTScenario.currentScenarioInfo.gameVersion > new GameVersion(1, 3, 0, 30, GameVersion.ReleaseTypes.Testing)) || VTScenario.currentScenarioInfo.allowedEquips.Contains(availableWeapon))
				{
					config.availableEquipStrings.Add(availableWeapon);
				}
			}
		}
		PerBiomeLivery component3 = gameObject.GetComponent<PerBiomeLivery>();
		if ((bool)component3 && PilotSaveManager.currentScenario.customScenarioInfo != null)
		{
			string sceneName;
			VTMap mapForScenario = VTResources.GetMapForScenario(PilotSaveManager.currentScenario.customScenarioInfo, out sceneName);
			if (mapForScenario is VTMapCustom)
			{
				component3.SetBiome(((VTMapCustom)mapForScenario).biome);
			}
		}
		PilotSaveManager.currentScenario.initialSpending = 0f;
		config.Initialize(campaignSave);
		if (PilotSaveManager.currentScenario.forcedEquips != null)
		{
			CampaignScenario.ForcedEquip[] forcedEquips = PilotSaveManager.currentScenario.forcedEquips;
			foreach (CampaignScenario.ForcedEquip forcedEquip in forcedEquips)
			{
				config.AttachImmediate(forcedEquip.weaponName, forcedEquip.hardpointIdx);
				config.lockedHardpoints.Add(forcedEquip.hardpointIdx);
			}
		}
		if (campaignSave.currentWeapons != null)
		{
			for (int k = 0; k < campaignSave.currentWeapons.Length; k++)
			{
				if (!config.lockedHardpoints.Contains(k) && !string.IsNullOrEmpty(campaignSave.currentWeapons[k]))
				{
					config.AttachImmediate(campaignSave.currentWeapons[k], k);
				}
			}
		}
		ScreenFader.FadeIn();
	}

	public void LaunchMission()
	{
		float totalFlightCost = config.GetTotalFlightCost();
		if (!PilotSaveManager.currentScenario.isTraining && totalFlightCost > PilotSaveManager.currentScenario.totalBudget)
		{
			configScenarioUI.DenyLaunch(VTLStaticStrings.vehicleConfig_denyOverBudget);
			return;
		}
		PilotSaveManager.currentScenario.initialSpending = totalFlightCost;
		config.SaveConfig();
		PilotSaveManager.SavePilotsToFile();
		StartCoroutine(LaunchMissionSceneRoutine());
	}

	private IEnumerator LaunchMissionSceneRoutine()
	{
		ScreenFader.FadeOut(Color.black, 0.85f);
		ControllerEventHandler.PauseEvents();
		yield return new WaitForSeconds(1f);
		if (PilotSaveManager.currentCampaign.isCustomScenarios)
		{
			VTScenario.LaunchScenario(VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign));
		}
		else
		{
			LoadingSceneController.LoadScene(PilotSaveManager.currentScenario.mapSceneName);
		}
		ControllerEventHandler.UnpauseEvents();
	}

	public void ReturnToCampaign()
	{
		StartCoroutine(ReturnToCampaignRoutine());
	}

	private IEnumerator ReturnToCampaignRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 1.5f);
		yield return new WaitForSeconds(2f);
		ControllerEventHandler.UnpauseEvents();
		config.SaveConfig();
		PilotSaveManager.SavePilotsToFile();
		if (VTScenarioEditor.returnToEditor)
		{
			VTMapManager.nextLaunchMode = VTMapManager.MapLaunchModes.Editor;
			VTResources.LaunchMapForScenario(VTResources.GetCustomScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign.campaignID), skipLoading: false);
		}
		else
		{
			LoadingSceneController.LoadSceneImmediate("ReadyRoom");
		}
	}
}
