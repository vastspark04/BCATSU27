using System.IO;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public static class VTOLMPUtils
{
	public static bool IsMultiplayer()
	{
		if (!VTOLMPLobbyManager.isInLobby)
		{
			if ((bool)VTNetworkManager.instance)
			{
				return VTNetworkManager.instance.netState != VTNetworkManager.NetStates.None;
			}
			return false;
		}
		return true;
	}

	public static int GetMaxPlayerCount(VTScenarioInfo s)
	{
		return Mathf.Min(16, s.mpPlayerCount);
	}

	public static bool IsMine(GameObject o)
	{
		VTNetEntity componentInParent = o.GetComponentInParent<VTNetEntity>(includeInactive: true);
		if ((bool)componentInParent && componentInParent.isMine)
		{
			return true;
		}
		return false;
	}

	public static VTCampaignInfo ConvertSingleToMultiplayerCampaign(VTCampaignInfo spCampaign)
	{
		Debug.Log("Converting singleplayer campaign '" + spCampaign.campaignName + "' to multiplayer...");
		string text = spCampaign.campaignID + "_mp";
		string text2 = Path.Combine(VTResources.customCampaignsDir, text);
		_ = spCampaign.config;
		VTResources.CopyDirectory(spCampaign.directoryPath, text2, new string[1] { ".meta" });
		string sourceFileName = Path.Combine(text2, spCampaign.campaignID + ".vtc");
		string text3 = Path.Combine(text2, text + ".vtc");
		File.Move(sourceFileName, text3);
		ConfigNode configNode = ConfigNode.LoadFromFile(text3);
		configNode.SetValue("multiplayer", value: true);
		configNode.SetValue("campaignID", text);
		configNode.SaveToFile(text3);
		string[] files = Directory.GetFiles(text2, "*.vts", SearchOption.AllDirectories);
		foreach (string text4 in files)
		{
			ConfigNode configNode2 = ConfigNode.LoadFromFile(text4);
			Debug.Log(" - Converting scenario: " + Path.GetFileName(text4));
			configNode2.SetValue("multiplayer", value: true);
			configNode2.SetValue("campaignID", text);
			configNode2.SetValue("mpPlayerCount", 1);
			ConfigNode node = configNode2.GetNode("UNITS");
			if (node != null)
			{
				foreach (ConfigNode node2 in node.GetNodes("UnitSpawner"))
				{
					if (node2.GetValue("unitID").Equals("PlayerSpawn"))
					{
						Debug.Log("Found player spawn.  Converting it to MultiplayerSpawn");
						node2.SetValue("unitID", "MultiplayerSpawn");
						node2.SetValue("vehicle", MultiplayerSpawn.GetVehicleEnum(spCampaign.vehicle));
						break;
					}
				}
			}
			configNode2.SaveToFile(text4);
		}
		VTResources.LoadCustomScenarios();
		return VTResources.GetCustomCampaign(text);
	}
}

}