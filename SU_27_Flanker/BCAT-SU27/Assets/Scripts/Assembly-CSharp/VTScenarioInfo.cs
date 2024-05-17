using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VTScenarioInfo
{
	public ConfigNode config;

	public GameVersion gameVersion;

	public Texture2D image;

	public string filePath;

	public string directoryPath;

	public VTCampaignInfo campaign;

	public string campaignID = string.Empty;

	public int campaignOrderIdx;

	public string mapID;

	public float baseBudget = 100000f;

	public bool isTraining;

	public bool forceEquips;

	public float normForcedFuel = 1f;

	public bool equipsConfigurable = true;

	public List<string> allowedEquips = new List<string>();

	public List<string> forcedEquips = new List<string>();

	public List<string> equipsOnComplete = new List<string>();

	public PlayerVehicle vehicle;

	public int mpPlayerCount;

	public bool separateBriefings;

	public string envName;

	public bool selectableEnv;

	public bool isWorkshop;

	private bool forceLocalize;

	private string _id;

	private string _name;

	private string _description;

	public bool isBuiltIn { get; private set; }

	public bool hasPackedMap { get; private set; }

	public string id
	{
		get
		{
			if (string.IsNullOrEmpty(_id))
			{
				_id = config.GetValue("scenarioID");
			}
			return _id;
		}
	}

	public string name
	{
		get
		{
			if (string.IsNullOrEmpty(_name))
			{
				_name = config.GetValue("scenarioName");
			}
			return _name;
		}
	}

	public string description
	{
		get
		{
			if (string.IsNullOrEmpty(_description))
			{
				_description = config.GetValue("scenarioDescription");
			}
			return _description;
		}
	}

	public static ConfigNode CreateEmptyScenarioConfig(string scenarioID, string vehicle, string mapID)
	{
		ConfigNode configNode = new ConfigNode("CustomScenario");
		configNode.SetValue("gameVersion", GameStartup.version);
		configNode.SetValue("scenarioName", "untitled");
		configNode.SetValue("scenarioID", scenarioID);
		configNode.SetValue("scenarioDescription", string.Empty);
		configNode.SetValue("mapID", mapID);
		configNode.SetValue("vehicle", vehicle);
		PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(vehicle);
		configNode.SetValue("allowedEquips", ConfigNodeUtils.WriteList(playerVehicle.GetEquipNamesList()));
		configNode.SetValue("forceEquips", value: false);
		configNode.SetValue("normForcedFuel", 1f);
		configNode.SetValue("equipsConfigurable", value: true);
		configNode.SetValue("baseBudget", 100000f);
		configNode.SetValue("isTraining", value: false);
		return configNode;
	}

	public string GetLocalizedName()
	{
		if (isBuiltIn || forceLocalize)
		{
			return VTLocalizationManager.GetString(campaignID + ":" + id + "_name", config.GetValue("scenarioName"), "Name of a mission.");
		}
		return name;
	}

	public string GetLocalizedDescription()
	{
		if (isBuiltIn || forceLocalize)
		{
			return VTLocalizationManager.GetString(campaignID + ":" + id + "_description", config.GetValue("scenarioDescription"), "Description of a mission.");
		}
		return description;
	}

	public VTScenarioInfo(SerializedScenario ss, SerializedCampaign sc, string campaignPath)
	{
		isBuiltIn = true;
		ConfigNode configNode = (config = ConfigNode.ParseNode(ss.scenarioConfig));
		directoryPath = campaignPath + "/" + ss.scenarioID.Trim();
		filePath = directoryPath + "/" + ss.scenarioID + ".vts";
		ConfigNodeUtils.TryParseValue(configNode, "campaignID", ref campaignID);
		ConfigNodeUtils.TryParseValue(configNode, "campaignOrderIdx", ref campaignOrderIdx);
		ConfigNodeUtils.TryParseValue(configNode, "gameVersion", ref gameVersion);
		if (VTLocalizationManager.writeLocalizationDict)
		{
			GetLocalizedName();
			GetLocalizedDescription();
		}
		mapID = configNode.GetValue("mapID");
		if (sc.GetMap(mapID) != null)
		{
			hasPackedMap = true;
		}
		if (configNode.HasValue("forcedEquips"))
		{
			forcedEquips = ConfigNodeUtils.ParseList(configNode.GetValue("forcedEquips"));
		}
		vehicle = VTResources.GetPlayerVehicle(configNode.GetValue("vehicle"));
		ConfigNodeUtils.TryParseValue(configNode, "baseBudget", ref baseBudget);
		ConfigNodeUtils.TryParseValue(configNode, "isTraining", ref isTraining);
		ConfigNodeUtils.TryParseValue(configNode, "forceEquips", ref forceEquips);
		ConfigNodeUtils.TryParseValue(configNode, "normForcedFuel", ref normForcedFuel);
		ConfigNodeUtils.TryParseValue(configNode, "equipsConfigurable", ref equipsConfigurable);
		ConfigNodeUtils.TryParseValue(configNode, "mpPlayerCount", ref mpPlayerCount);
		ConfigNodeUtils.TryParseValue(configNode, "separateBriefings", ref separateBriefings);
		if (configNode.HasValue("allowedEquips"))
		{
			allowedEquips = ConfigNodeUtils.ParseList(configNode.GetValue("allowedEquips"));
		}
		if (configNode.HasValue("equipsOnComplete"))
		{
			equipsOnComplete = ConfigNodeUtils.ParseList(configNode.GetValue("equipsOnComplete"));
		}
		ConfigNodeUtils.TryParseValue(configNode, "selectableEnv", ref selectableEnv);
		if (!ConfigNodeUtils.TryParseValue(configNode, "envName", ref envName))
		{
			envName = "day";
		}
		image = ss.image;
		if (Application.isPlaying || VTLocalizationManager.writeLocalizationDict)
		{
			ApplyLocalizedObjectives(config);
		}
	}

	private void ApplyLocalizedObjectives(ConfigNode config)
	{
		string arg = $"{vehicle.vehicleName}:{campaignID}:{id}:obj_";
		ConfigNode node = config.GetNode("OBJECTIVES");
		if (node == null)
		{
			return;
		}
		foreach (ConfigNode node2 in node.GetNodes("Objective"))
		{
			string value = node2.GetValue("objectiveID");
			string key = $"{arg}{value}:name";
			string key2 = $"{arg}{value}:info";
			node2.SetValue("objectiveName", VTLocalizationManager.GetString(key, node2.GetValue("objectiveName"), "A mission objective name"));
			node2.SetValue("objectiveInfo", VTLocalizationManager.GetString(key2, node2.GetValue("objectiveInfo"), "Mission objective info."));
		}
	}

	public void ApplyLocalizedObjectivesIfNecessary()
	{
		bool target = false;
		if (isBuiltIn || (ConfigNodeUtils.TryParseValue(config, "doLocalization", ref target) && target))
		{
			ApplyLocalizedObjectives(config);
		}
	}

	public VTScenarioInfo(ConfigNode scenarioNode, string filePath)
	{
		isBuiltIn = false;
		config = scenarioNode;
		this.filePath = filePath;
		directoryPath = Path.GetDirectoryName(filePath);
		vehicle = VTResources.GetPlayerVehicle(scenarioNode.GetValue("vehicle"));
		ConfigNodeUtils.TryParseValue(scenarioNode, "campaignID", ref campaignID);
		ConfigNodeUtils.TryParseValue(scenarioNode, "gameVersion", ref gameVersion);
		ConfigNodeUtils.TryParseValue(scenarioNode, "campaignOrderIdx", ref campaignOrderIdx);
		mapID = scenarioNode.GetValue("mapID");
		bool target = false;
		ConfigNodeUtils.TryParseValue(scenarioNode, "doLocalization", ref target);
		forceLocalize = target;
		if (VTLocalizationManager.writeLocalizationDict && target)
		{
			GetLocalizedName();
			GetLocalizedDescription();
		}
		if (Directory.Exists(Path.Combine(directoryPath, mapID)))
		{
			hasPackedMap = true;
		}
		else if (!string.IsNullOrEmpty(campaignID) && Directory.Exists(Path.Combine(Path.GetFullPath(Path.Combine(directoryPath, "..")), mapID)))
		{
			hasPackedMap = true;
		}
		if (scenarioNode.HasValue("forcedEquips"))
		{
			forcedEquips = ConfigNodeUtils.ParseList(scenarioNode.GetValue("forcedEquips"));
		}
		ConfigNodeUtils.TryParseValue(scenarioNode, "baseBudget", ref baseBudget);
		ConfigNodeUtils.TryParseValue(scenarioNode, "isTraining", ref isTraining);
		ConfigNodeUtils.TryParseValue(scenarioNode, "forceEquips", ref forceEquips);
		ConfigNodeUtils.TryParseValue(scenarioNode, "normForcedFuel", ref normForcedFuel);
		ConfigNodeUtils.TryParseValue(scenarioNode, "equipsConfigurable", ref equipsConfigurable);
		ConfigNodeUtils.TryParseValue(scenarioNode, "mpPlayerCount", ref mpPlayerCount);
		ConfigNodeUtils.TryParseValue(scenarioNode, "separateBriefings", ref separateBriefings);
		if (scenarioNode.HasValue("allowedEquips"))
		{
			allowedEquips = ConfigNodeUtils.ParseList(scenarioNode.GetValue("allowedEquips"));
		}
		if (scenarioNode.HasValue("equipsOnComplete"))
		{
			equipsOnComplete = ConfigNodeUtils.ParseList(scenarioNode.GetValue("equipsOnComplete"));
		}
		ConfigNodeUtils.TryParseValue(scenarioNode, "selectableEnv", ref selectableEnv);
		if (!ConfigNodeUtils.TryParseValue(scenarioNode, "envName", ref envName))
		{
			envName = "day";
		}
		string text = string.Empty;
		string[] files = Directory.GetFiles(Path.GetDirectoryName(filePath), "*", SearchOption.TopDirectoryOnly);
		foreach (string text2 in files)
		{
			if (text2.EndsWith(".jpg") || text2.EndsWith(".png"))
			{
				text = text2;
				if (text2.Contains(Path.DirectorySeparatorChar + "image."))
				{
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			image = VTResources.GetTexture(text);
		}
		if (target)
		{
			if (VTLocalizationManager.writeLocalizationDict)
			{
				ProtoBriefingNote.GetBriefingFromConfig(this);
			}
			ApplyLocalizedObjectives(config);
		}
	}

	public void SaveNewOrderIdx()
	{
		config.SetValue("campaignOrderIdx", campaignOrderIdx);
		config.SaveToFile(filePath);
	}

	public string GetFullResourcePath(string relativePath)
	{
		return Path.Combine(directoryPath, relativePath);
	}

	public CampaignScenario ToIngameScenario(VTCampaignInfo customCampaign)
	{
		CampaignScenario campaignScenario = new CampaignScenario();
		campaignScenario.customScenarioInfo = this;
		campaignScenario.baseBudget = baseBudget;
		campaignScenario.scenarioID = id;
		campaignScenario.scenarioName = GetLocalizedName();
		campaignScenario.description = GetLocalizedDescription();
		campaignScenario.equipConfigurable = equipsConfigurable;
		campaignScenario.environmentName = envName;
		campaignScenario.scenarioImage = image;
		if (selectableEnv)
		{
			campaignScenario.envOptions = EnvironmentManager.GetGlobalEnvOptions();
			for (int i = 0; i < campaignScenario.envOptions.Length; i++)
			{
				if (campaignScenario.envOptions[i].envName == envName)
				{
					campaignScenario.envIdx = i;
				}
			}
		}
		campaignScenario.isTraining = isTraining;
		campaignScenario.mapSceneName = VTResources.GetMapSceneNameForScenario(this);
		if (forceEquips)
		{
			List<CampaignScenario.ForcedEquip> list = new List<CampaignScenario.ForcedEquip>();
			for (int j = 0; j < forcedEquips.Count; j++)
			{
				if (!string.IsNullOrEmpty(forcedEquips[j]))
				{
					CampaignScenario.ForcedEquip forcedEquip = new CampaignScenario.ForcedEquip();
					forcedEquip.hardpointIdx = j;
					forcedEquip.weaponName = forcedEquips[j];
					list.Add(forcedEquip);
				}
			}
			campaignScenario.forcedEquips = list.ToArray();
		}
		if (!equipsConfigurable)
		{
			campaignScenario.forcedFuel = normForcedFuel;
		}
		campaignScenario.briefingNotes = new CampaignScenario.BriefingNote[0];
		if (customCampaign != null)
		{
			campaignScenario.equipmentOnComplete = equipsOnComplete;
			campaignScenario.scenariosOnComplete = new List<string>();
			List<VTScenarioInfo> list2 = (isTraining ? customCampaign.trainingScenarios : customCampaign.missionScenarios);
			if (campaignOrderIdx < list2.Count - 1)
			{
				campaignScenario.scenariosOnComplete.Add(list2[campaignOrderIdx + 1].id);
			}
		}
		return campaignScenario;
	}

	public static ConfigNode ReadConfigNode(string vtsFile)
	{
		ConfigNode result = null;
		if (vtsFile.EndsWith(".vts"))
		{
			result = ConfigNode.LoadFromFile(vtsFile);
		}
		else if (vtsFile.EndsWith(".vtsb"))
		{
			result = VTSteamWorkshopUtils.ReadWorkshopConfig(vtsFile);
		}
		else
		{
			Debug.LogError("Failed to read config node: " + vtsFile);
		}
		return result;
	}

	public static void WriteConfigFile(ConfigNode config, string outPath)
	{
		if (outPath.EndsWith("vtsb"))
		{
			VTSteamWorkshopUtils.WriteWorkshopConfig(config, outPath);
		}
		else
		{
			config.SaveToFile(outPath);
		}
	}

	public Actor.Designation GetPlayerDesignation()
	{
		ConfigNode node = config.GetNode("UNITS");
		int num = -1;
		foreach (ConfigNode node4 in node.GetNodes("UnitSpawner"))
		{
			if (node4.GetValue("unitID") == "PlayerSpawn")
			{
				num = node4.GetValue<int>("unitInstanceID");
			}
		}
		if (num >= 0)
		{
			ConfigNode node2 = config.GetNode("UNITGROUPS");
			if (node2 != null)
			{
				ConfigNode node3 = node2.GetNode("ALLIED");
				if (node3 != null)
				{
					foreach (ConfigNode.ConfigValue value in node3.GetValues())
					{
						List<int> list = ConfigNodeUtils.ParseList<int>(value.value);
						int num2 = -1;
						for (int i = 1; i < list.Count; i++)
						{
							if (list[i] == num)
							{
								num2 = i;
								break;
							}
						}
						if (num2 >= 1)
						{
							return new Actor.Designation(ConfigNodeUtils.ParseEnum<PhoneticLetters>(value.name), 1, num2);
						}
					}
					for (int j = 0; j < 26; j++)
					{
						PhoneticLetters phoneticLetters = (PhoneticLetters)j;
						string text = phoneticLetters.ToString();
						if (!node3.HasValue(text))
						{
							return new Actor.Designation((PhoneticLetters)j, 1, 1);
						}
					}
				}
			}
		}
		return new Actor.Designation(PhoneticLetters.Alpha, 1, 1);
	}
}
