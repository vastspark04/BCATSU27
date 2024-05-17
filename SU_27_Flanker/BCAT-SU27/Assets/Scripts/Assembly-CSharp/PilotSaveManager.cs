using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.CrashReportHandler;

public static class PilotSaveManager
{
	private static PlayerVehicle _currVehicle;

	private static Campaign _currentCampaign;

	private static CampaignScenario _currentScenario;

	private static Dictionary<string, PlayerVehicle> vehicles = null;

	private static List<PlayerVehicle> vehicleList = null;

	public static Dictionary<string, PilotSave> pilots = new Dictionary<string, PilotSave>();

	public static PilotSave current { get; set; }

	public static PlayerVehicle currentVehicle
	{
		get
		{
			return _currVehicle;
		}
		set
		{
			_currVehicle = value;
			string text = "none";
			if (_currVehicle != null)
			{
				text = _currVehicle.vehicleName;
			}
			Debug.Log("Set current vehicle to " + text);
			CrashReportHandler.SetUserMetadata("currentVehicle", text);
		}
	}

	public static Campaign currentCampaign
	{
		get
		{
			return _currentCampaign;
		}
		set
		{
			if (value != null)
			{
				Debug.Log("Setting current campaign: " + value.campaignID);
				CrashReportHandler.SetUserMetadata("campaignID", value.campaignID);
			}
			else
			{
				Debug.Log("Setting current campaign to null.");
				CrashReportHandler.SetUserMetadata("campaignID", "none");
			}
			_currentCampaign = value;
		}
	}

	public static CampaignScenario currentScenario
	{
		get
		{
			return _currentScenario;
		}
		set
		{
			if (value != null)
			{
				Debug.Log("Setting current scenario: " + value.scenarioID);
				CrashReportHandler.SetUserMetadata("scenarioID", value.scenarioID);
			}
			else
			{
				Debug.Log("Setting current scenario to null.");
				CrashReportHandler.SetUserMetadata("scenarioID", "none");
			}
			_currentScenario = value;
		}
	}

	public static string newSaveDataPath => Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Boundless Dynamics, LLC"), "VTOLVR"), "SaveData");

	public static string saveDataPath => Path.Combine(VTResources.gameRootDirectory, "SaveData");

	public static string pilotConfigPath => Path.Combine(saveDataPath, "pilots.cfg");

	public static string pilotConfigPathNew => Path.Combine(newSaveDataPath, "pilots.cfg");

	public static PlayerVehicle GetVehicle(string vehicleName)
	{
		EnsureVehicleCollections();
		if (vehicles.TryGetValue(vehicleName, out var value))
		{
			return value;
		}
		return null;
	}

	public static List<PlayerVehicle> GetVehicleList()
	{
		EnsureVehicleCollections();
		return vehicleList;
	}

	private static void EnsureVehicleCollections()
	{
		if (vehicles != null)
		{
			return;
		}
		vehicles = new Dictionary<string, PlayerVehicle>();
		PlayerVehicleList obj = (PlayerVehicleList)Resources.Load("PlayerVehicles");
		vehicleList = new List<PlayerVehicle>();
		foreach (PlayerVehicle playerVehicle in obj.playerVehicles)
		{
			vehicles.Add(playerVehicle.vehicleName, playerVehicle);
			vehicleList.Add(playerVehicle);
		}
	}

	public static void LoadPilotsFromFile()
	{
		string text = ((current == null) ? string.Empty : current.pilotName);
		pilots = new Dictionary<string, PilotSave>();
		ConfigNode configNode = ConfigNode.LoadFromFile(pilotConfigPathNew, logErrors: false);
		if (configNode == null)
		{
			configNode = ConfigNode.LoadFromFile(pilotConfigPath, logErrors: false);
		}
		if (configNode != null)
		{
			foreach (ConfigNode node in configNode.GetNodes("PILOTSAVE"))
			{
				PilotSave pilotSave = PilotSave.LoadFromConfigNode(node);
				pilots.Add(pilotSave.pilotName, pilotSave);
				if (text == pilotSave.pilotName)
				{
					current = pilotSave;
				}
			}
		}
		else
		{
			if (!Directory.Exists(newSaveDataPath))
			{
				Directory.CreateDirectory(newSaveDataPath);
			}
			if (!File.Exists(pilotConfigPathNew))
			{
				File.Create(pilotConfigPathNew).Dispose();
			}
		}
		Debug.Log("Loaded pilots from file.");
	}

	public static void EnsureSaveDirectory()
	{
		if (!Directory.Exists(newSaveDataPath))
		{
			Directory.CreateDirectory(newSaveDataPath);
		}
	}

	public static void SavePilotsToFile()
	{
		EnsureSaveDirectory();
		if (!File.Exists(pilotConfigPathNew))
		{
			File.Create(pilotConfigPathNew).Dispose();
		}
		ConfigNode configNode = new ConfigNode("PILOTS");
		foreach (PilotSave value in pilots.Values)
		{
			ConfigNode node = PilotSave.SaveToConfigNode(value);
			configNode.AddNode(node);
		}
		configNode.SaveToFile(pilotConfigPathNew);
		Debug.Log("Saved pilots to file.");
	}

	public static PilotSave CreateNewPilot(string pilotName)
	{
		PilotSave pilotSave = new PilotSave();
		pilotSave.pilotName = pilotName;
		pilots.Add(pilotName, pilotSave);
		return pilotSave;
	}

	public static void ResetCampaignSave(PilotSave pilot, string vehicleID, string campaignID)
	{
		VehicleSave vehicleSave = pilot.GetVehicleSave(vehicleID);
		CampaignSave campaignSave = vehicleSave.GetCampaignSave(campaignID);
		if (campaignSave != null)
		{
			vehicleSave.campaignSaves.Remove(campaignSave);
			SavePilotsToFile();
		}
	}
}
