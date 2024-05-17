using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;

public class VTCampaignInfo
{
	public enum AvailabilityModes
	{
		Sequential,
		All_Available
	}

	public class AsyncCtorRequest
	{
		public VTCampaignInfo result;
	}

	private class ScenarioOrderSorter : IComparer<VTScenarioInfo>
	{
		public int Compare(VTScenarioInfo x, VTScenarioInfo y)
		{
			if (x.campaignOrderIdx != y.campaignOrderIdx)
			{
				return x.campaignOrderIdx.CompareTo(y.campaignOrderIdx);
			}
			return x.id.CompareTo(y.id);
		}
	}

	public string campaignID;

	public string campaignName;

	public string description;

	public string vehicle;

	public Texture2D image;

	public List<string> startingEquips;

	public bool isWorkshop;

	public Item wsItem;

	public bool multiplayer;

	public AvailabilityModes availability;

	public List<VTScenarioInfo> allScenarios;

	public List<VTScenarioInfo> missionScenarios;

	public List<VTScenarioInfo> trainingScenarios;

	public ConfigNode config;

	public bool hideFromMenu;

	public SerializedCampaign serializedCampaign;

	public string workshopAuthor;

	public string filePath { get; private set; }

	public string directoryPath { get; private set; }

	public bool isBuiltIn { get; private set; }

	public bool indicesWereRepaired { get; private set; }

	public VTScenarioInfo GetScenario(string scenarioID)
	{
		foreach (VTScenarioInfo allScenario in allScenarios)
		{
			if (allScenario.id == scenarioID)
			{
				return allScenario;
			}
		}
		Debug.Log("Campaign scenario '" + scenarioID + "' not found in campaign '" + campaignID + "'");
		return null;
	}

	public string GetLocalizedCampaignName()
	{
		return VTLocalizationManager.GetString("campaign_" + campaignID + "_name", config.GetValue("campaignName"), "Name of a campaign.");
	}

	public string GetLocalizedDescription()
	{
		return VTLocalizationManager.GetString("campaign_" + campaignID + "_description", config.GetValue("description"), "Description of a campaign.");
	}

	public VTCampaignInfo(SerializedCampaign sc)
	{
		Debug.Log("VTCampaignInfo(" + sc.campaignID + ")");
		isBuiltIn = true;
		serializedCampaign = sc;
		config = ConfigNode.ParseNode(sc.campaignConfig);
		campaignID = sc.campaignID;
		campaignName = config.GetValue("campaignName");
		description = config.GetValue("description");
		if (VTLocalizationManager.writeLocalizationDict)
		{
			GetLocalizedCampaignName();
			GetLocalizedDescription();
		}
		vehicle = config.GetValue("vehicle");
		startingEquips = ConfigNodeUtils.ParseList(config.GetValue("startingEquips"));
		ConfigNodeUtils.TryParseValue(config, "availability", ref availability);
		hideFromMenu = sc.hideFromMenu;
		if (Application.isPlaying && sc.requireDLCs != null)
		{
			uint[] requireDLCs = sc.requireDLCs;
			for (int i = 0; i < requireDLCs.Length; i++)
			{
				if (!SteamApps.IsDlcInstalled(requireDLCs[i]))
				{
					hideFromMenu = true;
					break;
				}
			}
		}
		string text = "%BuiltIn/" + campaignID;
		filePath = text + "/" + campaignID + ".vtc";
		image = sc.image;
		allScenarios = new List<VTScenarioInfo>();
		missionScenarios = new List<VTScenarioInfo>();
		trainingScenarios = new List<VTScenarioInfo>();
		SerializedScenario[] scenarios = sc.scenarios;
		for (int i = 0; i < scenarios.Length; i++)
		{
			VTScenarioInfo vTScenarioInfo = new VTScenarioInfo(scenarios[i], sc, text)
			{
				campaign = this
			};
			allScenarios.Add(vTScenarioInfo);
			if (vTScenarioInfo.isTraining)
			{
				trainingScenarios.Add(vTScenarioInfo);
			}
			else
			{
				missionScenarios.Add(vTScenarioInfo);
			}
		}
		if (VTLocalizationManager.writeLocalizationDict)
		{
			foreach (VTScenarioInfo allScenario in allScenarios)
			{
				ProtoBriefingNote.GetBriefingFromConfig(allScenario);
			}
		}
		SortScenariosByOrder();
	}

	public VTCampaignInfo(ConfigNode config, string filePath)
	{
		isBuiltIn = false;
		this.config = config;
		campaignID = config.GetValue("campaignID");
		campaignName = config.GetValue("campaignName");
		description = config.GetValue("description");
		vehicle = config.GetValue("vehicle");
		startingEquips = ConfigNodeUtils.ParseList(config.GetValue("startingEquips"));
		ConfigNodeUtils.TryParseValue(config, "availability", ref availability);
		ConfigNodeUtils.TryParseValue(config, "multiplayer", ref multiplayer);
		this.filePath = filePath;
		string text = string.Empty;
		string text2 = (directoryPath = Path.GetDirectoryName(filePath));
		string[] files = Directory.GetFiles(text2, "*", SearchOption.TopDirectoryOnly);
		foreach (string text3 in files)
		{
			if (text3.EndsWith(".jpg") || text3.EndsWith(".png"))
			{
				text = text3;
				if (text3.Contains(Path.DirectorySeparatorChar + "image."))
				{
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			image = VTResources.GetTexture(text);
		}
		allScenarios = VTResources.LoadScenariosFromDir(text2, checkModified: false, enforceDirectoryName: false, decodeWSScenarios: true);
		missionScenarios = new List<VTScenarioInfo>();
		trainingScenarios = new List<VTScenarioInfo>();
		Debug.Log(" - loaded scenarios:");
		foreach (VTScenarioInfo allScenario in allScenarios)
		{
			Debug.Log(" - - " + allScenario.id);
			allScenario.config.SetValue("campaignID", campaignID);
			allScenario.campaignID = campaignID;
			allScenario.campaign = this;
			if (allScenario.isTraining)
			{
				trainingScenarios.Add(allScenario);
			}
			else
			{
				missionScenarios.Add(allScenario);
			}
		}
		SortScenariosByOrder();
	}

	private VTCampaignInfo()
	{
	}

	public static AsyncCtorRequest ConstructAsync(MonoBehaviour host, ConfigNode config, string filePath)
	{
		AsyncCtorRequest asyncCtorRequest = new AsyncCtorRequest();
		host.StartCoroutine(CtorAsyncRoutine(host, asyncCtorRequest, config, filePath));
		return asyncCtorRequest;
	}

	private static IEnumerator CtorAsyncRoutine(MonoBehaviour host, AsyncCtorRequest r, ConfigNode config, string filePath)
	{
		VTCampaignInfo info = new VTCampaignInfo
		{
			isBuiltIn = false,
			config = config,
			campaignID = config.GetValue("campaignID"),
			campaignName = config.GetValue("campaignName"),
			description = config.GetValue("description"),
			vehicle = config.GetValue("vehicle"),
			startingEquips = ConfigNodeUtils.ParseList(config.GetValue("startingEquips"))
		};
		ConfigNodeUtils.TryParseValue(config, "availability", ref info.availability);
		info.filePath = filePath;
		string text = string.Empty;
		string text2 = (info.directoryPath = Path.GetDirectoryName(filePath));
		string text3 = text2;
		string[] files = Directory.GetFiles(text3, "*", SearchOption.TopDirectoryOnly);
		foreach (string text4 in files)
		{
			if (text4.EndsWith(".jpg") || text4.EndsWith(".png"))
			{
				text = text4;
				if (text4.Contains(Path.DirectorySeparatorChar + "image."))
				{
					break;
				}
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			info.image = VTResources.GetTexture(text);
		}
		info.allScenarios = new List<VTScenarioInfo>();
		yield return VTResources.LoadScenariosFromDirAsync(host, info.allScenarios, text3, checkModified: false, enforceDirectoryName: false, decodeWSScenarios: true);
		info.missionScenarios = new List<VTScenarioInfo>();
		info.trainingScenarios = new List<VTScenarioInfo>();
		foreach (VTScenarioInfo allScenario in info.allScenarios)
		{
			allScenario.config.SetValue("campaignID", info.campaignID);
			allScenario.campaignID = info.campaignID;
			allScenario.campaign = info;
			if (allScenario.isTraining)
			{
				info.trainingScenarios.Add(allScenario);
			}
			else
			{
				info.missionScenarios.Add(allScenario);
			}
		}
		info.SortScenariosByOrder();
		r.result = info;
	}

	public void SortScenariosByOrder()
	{
		ScenarioOrderSorter comparer = new ScenarioOrderSorter();
		missionScenarios.Sort(comparer);
		trainingScenarios.Sort(comparer);
		FixCampaignOrderIndices(missionScenarios);
		FixCampaignOrderIndices(trainingScenarios);
	}

	private void FixCampaignOrderIndices(List<VTScenarioInfo> sortedList)
	{
		for (int i = 0; i < sortedList.Count; i++)
		{
			if (sortedList[i].campaignOrderIdx != i)
			{
				indicesWereRepaired = true;
				sortedList[i].campaignOrderIdx = i;
			}
		}
	}

	public ConfigNode SaveToConfigNode()
	{
		ConfigNode configNode = new ConfigNode("CAMPAIGN");
		configNode.SetValue("campaignID", campaignID);
		configNode.SetValue("campaignName", campaignName);
		configNode.SetValue("description", description);
		configNode.SetValue("vehicle", vehicle);
		configNode.SetValue("startingEquips", ConfigNodeUtils.WriteList(startingEquips));
		configNode.SetValue("availability", availability);
		configNode.SetValue("multiplayer", multiplayer);
		return configNode;
	}

	public Campaign ToIngameCampaign()
	{
		Campaign campaign = ScriptableObject.CreateInstance<Campaign>();
		campaign.campaignID = campaignID;
		campaign.campaignImage = image;
		string text = (campaign.campaignName = (campaign.name = GetLocalizedCampaignName()));
		campaign.demoAvailable = false;
		campaign.description = GetLocalizedDescription();
		campaign.isCustomScenarios = true;
		campaign.readyToPlay = true;
		campaign.isBuiltIn = isBuiltIn;
		campaign.isStandaloneScenarios = false;
		campaign.weaponsOnStart = startingEquips;
		campaign.missions = new List<CampaignScenario>();
		campaign.trainingMissions = new List<CampaignScenario>();
		campaign.scenariosOnStart = new List<string>();
		if (availability == AvailabilityModes.Sequential)
		{
			if (missionScenarios.Count > 0)
			{
				campaign.scenariosOnStart.Add(missionScenarios[0].id);
			}
			foreach (VTScenarioInfo trainingScenario in trainingScenarios)
			{
				campaign.scenariosOnStart.Add(trainingScenario.id);
			}
		}
		else if (availability == AvailabilityModes.All_Available)
		{
			foreach (VTScenarioInfo allScenario in allScenarios)
			{
				campaign.scenariosOnStart.Add(allScenario.id);
			}
		}
		foreach (VTScenarioInfo missionScenario in missionScenarios)
		{
			campaign.missions.Add(missionScenario.ToIngameScenario(this));
		}
		foreach (VTScenarioInfo trainingScenario2 in trainingScenarios)
		{
			campaign.trainingMissions.Add(trainingScenario2.ToIngameScenario(this));
		}
		if ((bool)serializedCampaign)
		{
			campaign.campaignLivery = serializedCampaign.campaignLivery;
			campaign.campaignLiveryOpFor = serializedCampaign.campaignLiveryOpFor;
			campaign.perVehicleLiveries = serializedCampaign.perVehicleLiveries;
		}
		return campaign;
	}

	public Campaign ToIngameCampaignAsync(MonoBehaviour script, out BDCoroutine coroutine)
	{
		Campaign campaign = ScriptableObject.CreateInstance<Campaign>();
		coroutine = new BDCoroutine(ToIngameCampaignAsyncRoutine(campaign), script);
		return campaign;
	}

	private IEnumerator ToIngameCampaignAsyncRoutine(Campaign c)
	{
		c.campaignID = campaignID;
		c.campaignImage = image;
		string text = (c.campaignName = (c.name = GetLocalizedCampaignName()));
		c.demoAvailable = false;
		c.description = GetLocalizedDescription();
		c.isCustomScenarios = true;
		c.readyToPlay = true;
		c.isBuiltIn = isBuiltIn;
		c.isStandaloneScenarios = false;
		c.weaponsOnStart = startingEquips;
		c.missions = new List<CampaignScenario>();
		c.trainingMissions = new List<CampaignScenario>();
		c.scenariosOnStart = new List<string>();
		if (availability == AvailabilityModes.Sequential)
		{
			if (missionScenarios.Count > 0)
			{
				c.scenariosOnStart.Add(missionScenarios[0].id);
			}
			foreach (VTScenarioInfo trainingScenario in trainingScenarios)
			{
				c.scenariosOnStart.Add(trainingScenario.id);
			}
		}
		else if (availability == AvailabilityModes.All_Available)
		{
			foreach (VTScenarioInfo allScenario in allScenarios)
			{
				c.scenariosOnStart.Add(allScenario.id);
			}
		}
		foreach (VTScenarioInfo missionScenario in missionScenarios)
		{
			c.missions.Add(missionScenario.ToIngameScenario(this));
			yield return null;
		}
		foreach (VTScenarioInfo trainingScenario2 in trainingScenarios)
		{
			c.trainingMissions.Add(trainingScenario2.ToIngameScenario(this));
			yield return null;
		}
		if ((bool)serializedCampaign)
		{
			c.campaignLivery = serializedCampaign.campaignLivery;
			c.campaignLiveryOpFor = serializedCampaign.campaignLiveryOpFor;
			c.perVehicleLiveries = serializedCampaign.perVehicleLiveries;
		}
	}

	public static ConfigNode CreateEmptyCampaignConfig(string id, string vehicle)
	{
		ConfigNode configNode = new ConfigNode("CAMPAIGN");
		configNode.SetValue("campaignID", id);
		configNode.SetValue("campaignName", "New Campaign");
		configNode.SetValue("description", "Enter description...");
		configNode.SetValue("vehicle", vehicle);
		List<string> equipNamesList = VTResources.GetPlayerVehicle(vehicle).GetEquipNamesList();
		configNode.SetValue("startingEquips", ConfigNodeUtils.WriteList(equipNamesList));
		return configNode;
	}

	public static bool RepairCampaignFileStructure(VTCampaignInfo cInfo, string campaignDir, bool doRepair = true)
	{
		if (!doRepair)
		{
			Debug.Log("Campaign Repair: Checking if repair is required for " + cInfo.campaignID);
		}
		bool result = false;
		string[] directories = Directory.GetDirectories(campaignDir, "*", SearchOption.TopDirectoryOnly);
		foreach (string text in directories)
		{
			Path.GetFileName(text);
			string[] files = Directory.GetFiles(text, "*", SearchOption.TopDirectoryOnly);
			foreach (string text2 in files)
			{
				string extension = Path.GetExtension(text2);
				if (!(extension == ".vtm") && !(extension == ".vtmb"))
				{
					continue;
				}
				string[] files2 = Directory.GetFiles(text, "height*", SearchOption.TopDirectoryOnly);
				if (files2.Length == 0)
				{
					continue;
				}
				string[] files3 = Directory.GetFiles(text, "*.vts*", SearchOption.TopDirectoryOnly);
				if (files3.Length == 0)
				{
					continue;
				}
				Debug.LogFormat("Campaign Repair: scenario file found in map folder: {0}", files3[0]);
				result = true;
				if (!doRepair)
				{
					continue;
				}
				ConfigNode configNode = ((!(extension == ".vtm")) ? VTSteamWorkshopUtils.ReadWorkshopConfig(text2) : ConfigNode.LoadFromFile(text2));
				string value = configNode.GetValue("mapID");
				string newMapID = GetNewMapID(campaignDir, value);
				configNode.SetValue("mapID", newMapID);
				if (extension == ".vtm")
				{
					configNode.SaveToFile(text2);
				}
				else
				{
					VTSteamWorkshopUtils.WriteWorkshopConfig(configNode, text2);
				}
				Debug.Log("Campaign Repair: Map shares directory with scenario files.  Moving map to new ID: " + newMapID);
				string text3 = Path.Combine(campaignDir, newMapID);
				Directory.CreateDirectory(text3);
				string text4 = Path.Combine(text3, newMapID + extension);
				Debug.Log("Moving " + text2 + " to " + text4);
				File.Move(text2, text4);
				string[] array = files2;
				foreach (string text5 in array)
				{
					string text6 = Path.Combine(text3, Path.GetFileName(text5));
					Debug.Log("Moving " + text5 + " to " + text6);
					File.Move(text5, text6);
				}
				string text7 = Path.Combine(text, "preview.jpg");
				if (File.Exists(text7))
				{
					string text8 = Path.Combine(text3, "preview.jpg");
					Debug.Log("Moving " + text7 + " to " + text8);
					File.Move(text7, text8);
				}
				array = Directory.GetFiles(campaignDir, "*.vts*", SearchOption.AllDirectories);
				foreach (string text9 in array)
				{
					if (!text9.EndsWith(".vts") && !text9.EndsWith(".vtsb"))
					{
						continue;
					}
					ConfigNode configNode2 = VTScenarioInfo.ReadConfigNode(text9);
					try
					{
						if (configNode2.GetValue("mapID") == value)
						{
							Debug.Log("ConfigFile: " + text9 + " used the map.  Updating the ID reference.");
							configNode2.SetValue("mapID", newMapID);
							VTScenarioInfo.WriteConfigFile(configNode2, text9);
						}
					}
					catch (Exception)
					{
						Debug.LogError("Caught exception while trying to read mapID from vts file: " + text9 + "\nConfig:\n" + ConfigNode.WriteNode(configNode2, 0));
					}
				}
			}
		}
		directories = Directory.GetDirectories(campaignDir, "*", SearchOption.TopDirectoryOnly);
		foreach (string text10 in directories)
		{
			string fileName = Path.GetFileName(text10);
			string[] files = Directory.GetFiles(text10, "*.vts*");
			foreach (string text11 in files)
			{
				string extension2 = Path.GetExtension(text11);
				if (!(extension2 == ".vts") && !(extension2 == ".vtsb"))
				{
					continue;
				}
				string value2 = VTScenarioInfo.ReadConfigNode(text11).GetValue("scenarioID");
				if (value2 != Path.GetFileNameWithoutExtension(text11))
				{
					Debug.LogFormat("Campaign Repair: A scenarioID does not match its filename.  Renaming the file. ({0})", text11);
					result = true;
					if (doRepair)
					{
						string text12 = Path.Combine(text10, value2 + extension2);
						bool flag = true;
						if (File.Exists(text12))
						{
							if (File.GetLastWriteTimeUtc(text11) < File.GetLastWriteTimeUtc(text12))
							{
								Debug.Log("File found at " + text12 + " is older.  Deleting it.");
								File.Delete(text12);
							}
							else
							{
								flag = false;
								Debug.Log("File found at " + text12 + " is older.  Deleting it.");
								File.Delete(text11);
							}
						}
						if (flag)
						{
							Debug.Log("Renaming " + text11 + " to " + text12);
							File.Move(text11, text12);
						}
					}
				}
				if (!(value2 != fileName))
				{
					continue;
				}
				Debug.LogFormat("Campaign Repair: A scenarioID does not match its folder name: {0} vs {1}", value2, fileName);
				result = true;
				if (!doRepair)
				{
					continue;
				}
				string text13 = Path.Combine(campaignDir, value2);
				Debug.Log("Trying to rename " + fileName + " to " + value2);
				bool flag2 = true;
				if (Directory.Exists(text13))
				{
					bool flag3 = false;
					string text14 = Path.Combine(text13, value2 + ".vts");
					if (!File.Exists(text14))
					{
						text14 += "b";
					}
					if (File.Exists(text14))
					{
						flag2 = false;
						string[] array = Directory.GetFiles(text10);
						foreach (string text15 in array)
						{
							string text16 = Path.Combine(text13, Path.GetFileName(text15));
							if (File.Exists(text16))
							{
								if (File.GetLastWriteTimeUtc(text16) < File.GetLastWriteTimeUtc(text15))
								{
									File.Delete(text15);
									continue;
								}
								File.Delete(text16);
								File.Move(text15, text16);
							}
							else
							{
								Debug.Log("Moving mission file " + text15 + " to " + text16);
								File.Move(text15, text16);
							}
						}
						Debug.Log("A mission folder with the incorrect name and old data is no longer needed. (" + text10 + ")");
						Directory.Delete(text10, recursive: true);
					}
					else
					{
						flag3 = true;
					}
					if (flag3)
					{
						Debug.Log("Deleting " + text13);
						Directory.Delete(text13, recursive: true);
					}
				}
				if (flag2)
				{
					Debug.Log("Renaming " + text10 + " to " + text13);
					Directory.Move(text10, text13);
				}
			}
		}
		if (cInfo != null && cInfo.indicesWereRepaired)
		{
			Debug.LogFormat("Campaign Repair: A campaign had its order indices fixed. Configs need to be saved to file. ({0})", cInfo.campaignID);
			result = true;
			if (doRepair)
			{
				foreach (VTScenarioInfo allScenario in cInfo.allScenarios)
				{
					allScenario.SaveNewOrderIdx();
				}
				return result;
			}
		}
		return result;
	}

	private static string GetNewMapID(string campaignDir, string oldID)
	{
		if (oldID.StartsWith("map") && oldID.IndexOf('_') == 6 && oldID.Length > 7)
		{
			oldID = oldID.Substring(7);
		}
		Directory.GetDirectories(campaignDir, "*", SearchOption.TopDirectoryOnly);
		string text = "map";
		int num = 0;
		string text2 = "_";
		string text3 = text + num.ToString("000") + text2 + oldID;
		while (Directory.Exists(Path.Combine(campaignDir, text3)))
		{
			num++;
			text3 = text + num.ToString("000") + text2 + oldID;
		}
		return text3;
	}
}
