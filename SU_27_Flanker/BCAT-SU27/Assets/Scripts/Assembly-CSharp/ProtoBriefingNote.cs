using System.Collections.Generic;
using UnityEngine;

public class ProtoBriefingNote : IScenarioResourceUser
{
	public string text;

	public string imagePath;

	public bool imageDirty;

	public string audioClipPath;

	public bool audioDirty;

	public const string PARENT_NODE_NAME = "Briefing";

	public const string PARENT_NODE_NAME_B = "Briefing_B";

	public const string NODE_NAME = "BRIEFING_NOTE";

	public Texture2D cachedImage;

	public AudioClip cachedAudio;

	public void SaveToParentNode(ConfigNode parentNode)
	{
		ConfigNode configNode = new ConfigNode("BRIEFING_NOTE");
		configNode.SetValue("text", text);
		configNode.SetValue("imagePath", imagePath);
		configNode.SetValue("audioClipPath", audioClipPath);
		parentNode.AddNode(configNode);
	}

	public void LoadFromBriefingNode(ConfigNode bNode)
	{
		text = bNode.GetValue("text");
		imagePath = bNode.GetValue("imagePath");
		audioClipPath = bNode.GetValue("audioClipPath");
	}

	public string[] GetDirtyResources()
	{
		if (imageDirty || audioDirty)
		{
			string[] array = new string[2];
			if (imageDirty)
			{
				array[0] = imagePath;
			}
			if (audioDirty)
			{
				array[1] = audioClipPath;
			}
			return array;
		}
		return null;
	}

	public void SetCleanedResources(string[] resources)
	{
		if (imageDirty)
		{
			imagePath = resources[0];
			imageDirty = false;
		}
		if (audioDirty)
		{
			audioClipPath = resources[1];
			audioDirty = false;
		}
	}

	public List<string> GetAllUsedResources()
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrEmpty(imagePath))
		{
			list.Add(imagePath);
		}
		if (!string.IsNullOrEmpty(audioClipPath))
		{
			list.Add(audioClipPath);
		}
		return list;
	}

	public CampaignScenario.BriefingNote ToBriefingNote(VTScenarioInfo scenarioInfo)
	{
		CampaignScenario.BriefingNote note = new CampaignScenario.BriefingNote();
		if (!string.IsNullOrEmpty(imagePath))
		{
			note.image = VTResources.GetTexture(scenarioInfo.GetFullResourcePath(imagePath));
		}
		if (!string.IsNullOrEmpty(audioClipPath))
		{
			if (audioClipPath.ToLower().EndsWith("mp3"))
			{
				VTResources.LoadMP3Clip(scenarioInfo.GetFullResourcePath(audioClipPath), delegate(AudioClip clip)
				{
					if (note != null)
					{
						note.sound = clip;
					}
				});
			}
			else
			{
				note.sound = VTResources.GetAudioClip(scenarioInfo.GetFullResourcePath(audioClipPath));
			}
		}
		note.note = text;
		return note;
	}

	public static List<ProtoBriefingNote> GetProtoBriefingsFromConfig(ConfigNode scenarioConfig, bool teamB)
	{
		List<ProtoBriefingNote> list = new List<ProtoBriefingNote>();
		string name = (teamB ? "Briefing_B" : "Briefing");
		if (scenarioConfig.HasNode(name))
		{
			foreach (ConfigNode node in scenarioConfig.GetNode(name).GetNodes("BRIEFING_NOTE"))
			{
				ProtoBriefingNote protoBriefingNote = new ProtoBriefingNote();
				protoBriefingNote.LoadFromBriefingNode(node);
				list.Add(protoBriefingNote);
			}
			return list;
		}
		return list;
	}

	public static CampaignScenario.BriefingNote[] GetBriefingFromConfig(VTScenarioInfo scenario, bool teamB = false)
	{
		if (!Application.isPlaying)
		{
			Debug.LogFormat("Getting breifing notes from {0} : {1}", scenario.campaignID, scenario.id);
		}
		ConfigNode config = scenario.config;
		bool isBuiltIn = scenario.isBuiltIn;
		List<CampaignScenario.BriefingNote> list = new List<CampaignScenario.BriefingNote>();
		string name = (teamB ? "Briefing_B" : "Briefing");
		if (config.HasNode(name))
		{
			ConfigNode node = config.GetNode(name);
			int num = 0;
			foreach (ConfigNode node2 in node.GetNodes("BRIEFING_NOTE"))
			{
				ProtoBriefingNote protoBriefingNote = new ProtoBriefingNote();
				protoBriefingNote.LoadFromBriefingNode(node2);
				if (isBuiltIn)
				{
					string text = scenario.campaignID;
					if (string.IsNullOrEmpty(text))
					{
						text = $"custom_{scenario.vehicle.vehicleName}";
					}
					string key = $"{text}_{scenario.id}_b{num}";
					protoBriefingNote.text = VTLocalizationManager.GetString(key, protoBriefingNote.text, "Text from a mission briefing.");
				}
				list.Add(protoBriefingNote.ToBriefingNote(scenario));
				num++;
			}
		}
		return list.ToArray();
	}
}
