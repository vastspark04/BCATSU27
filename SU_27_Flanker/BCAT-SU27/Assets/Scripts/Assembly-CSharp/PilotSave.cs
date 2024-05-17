using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PilotSave
{
	public string pilotName;

	public string lastVehicleUsed = string.Empty;

	public float totalFlightTime;

	public Color skinColor = ColorUtils.From255(163f, 123f, 64f, 255f);

	public Color suitColor = ColorUtils.From255(100f, 103f, 89f, 255f);

	public Color vestColor = ColorUtils.From255(72f, 81f, 65f, 255f);

	public Color gSuitColor = ColorUtils.From255(84f, 92f, 72f, 255f);

	public Color strapsColor = ColorUtils.From255(146f, 146f, 146f, 255f);

	private Dictionary<string, VehicleSave> vehicleSaves = new Dictionary<string, VehicleSave>();

	private static StringBuilder sanitizedNameSB = new StringBuilder();

	private const string allowedNameChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ 1234567890";

	public VehicleSave lastVehicleSave
	{
		get
		{
			if (vehicleSaves.TryGetValue(lastVehicleUsed, out var value))
			{
				return value;
			}
			return null;
		}
	}

	public VehicleSave GetVehicleSave(string vehicleID)
	{
		if (vehicleSaves.TryGetValue(vehicleID, out var value))
		{
			return value;
		}
		VehicleSave vehicleSave = new VehicleSave();
		vehicleSave.vehicleName = vehicleID;
		vehicleSave.campaignSaves = new List<CampaignSave>();
		vehicleSave.joystickPosition = 0.5f * Vector3.one;
		vehicleSaves.Add(vehicleID, vehicleSave);
		return vehicleSave;
	}

	public string GetInfoString()
	{
		float num = totalFlightTime;
		StringBuilder stringBuilder = new StringBuilder();
		TimeSpan timeSpan = TimeSpan.FromSeconds(num);
		string value = $"{Math.Floor(timeSpan.TotalHours)}h:{timeSpan.Minutes:D2}m:{timeSpan.Seconds:D2}s";
		string ps_timeLabel = VTLStaticStrings.ps_timeLabel;
		stringBuilder.Append(ps_timeLabel).Append(":\n");
		stringBuilder.Append("  ").Append(value).Append("\n");
		if (!string.IsNullOrEmpty(lastVehicleUsed))
		{
			PlayerVehicle vehicle = PilotSaveManager.GetVehicle(lastVehicleUsed);
			if (vehicle != null)
			{
				string vehicleName = vehicle.vehicleName;
				string ps_lvLabel = VTLStaticStrings.ps_lvLabel;
				stringBuilder.Append(ps_lvLabel).Append("\n");
				stringBuilder.Append("  ").Append(vehicleName).Append("\n");
			}
		}
		return stringBuilder.ToString();
	}

	public static string SanitizeName(string n)
	{
		if (n.Length > 30)
		{
			n = n.Substring(0, 30);
		}
		sanitizedNameSB.Clear();
		bool flag = false;
		for (int i = 0; i < n.Length; i++)
		{
			if (!"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ 1234567890".Contains(n[i].ToString()))
			{
				continue;
			}
			if (n[i] == ' ')
			{
				if (flag)
				{
					continue;
				}
				flag = true;
			}
			else
			{
				flag = false;
			}
			sanitizedNameSB.Append(n[i]);
		}
		n = sanitizedNameSB.ToString();
		n = n.Trim();
		return n;
	}

	public static PilotSave LoadFromConfigNode(ConfigNode node)
	{
		PilotSave pilotSave = new PilotSave();
		pilotSave.pilotName = node.GetValue("pilotName");
		pilotSave.pilotName = SanitizeName(pilotSave.pilotName);
		if (string.IsNullOrEmpty(pilotSave.pilotName))
		{
			pilotSave.pilotName = "noob";
		}
		pilotSave.vehicleSaves = new Dictionary<string, VehicleSave>();
		foreach (ConfigNode node2 in node.GetNodes("VEHICLE"))
		{
			if ((bool)VTResources.GetPlayerVehicle(node2.GetValue("vehicleName")))
			{
				VehicleSave vehicleSave = VehicleSave.LoadFromConfigNode(node2);
				pilotSave.vehicleSaves.Add(vehicleSave.vehicleName, vehicleSave);
			}
		}
		pilotSave.lastVehicleUsed = node.GetValue("lastVehicleUsed");
		if (node.HasValue("totalFlightTime"))
		{
			pilotSave.totalFlightTime = ConfigNodeUtils.ParseFloat(node.GetValue("totalFlightTime"));
		}
		bool clamp = GameStartup.version.releaseType == GameVersion.ReleaseTypes.Public;
		if (node.HasValue("skinColor"))
		{
			pilotSave.skinColor = ColorUtils.FromVector3(ConfigNodeUtils.ParseVector3(node.GetValue("skinColor")), clamp);
		}
		if (node.HasValue("suitColor"))
		{
			pilotSave.suitColor = ColorUtils.FromVector3(ConfigNodeUtils.ParseVector3(node.GetValue("suitColor")), clamp);
		}
		if (node.HasValue("vestColor"))
		{
			pilotSave.vestColor = ColorUtils.FromVector3(ConfigNodeUtils.ParseVector3(node.GetValue("vestColor")), clamp);
		}
		if (node.HasValue("gSuitColor"))
		{
			pilotSave.gSuitColor = ColorUtils.FromVector3(ConfigNodeUtils.ParseVector3(node.GetValue("gSuitColor")), clamp);
		}
		if (node.HasValue("strapsColor"))
		{
			pilotSave.strapsColor = ColorUtils.FromVector3(ConfigNodeUtils.ParseVector3(node.GetValue("strapsColor")), clamp);
		}
		return pilotSave;
	}

	public static ConfigNode SaveToConfigNode(PilotSave ps)
	{
		ConfigNode configNode = new ConfigNode("PILOTSAVE");
		configNode.SetValue("pilotName", ps.pilotName);
		foreach (VehicleSave value in ps.vehicleSaves.Values)
		{
			ConfigNode node = VehicleSave.SaveToConfigNode(value);
			configNode.AddNode(node);
		}
		configNode.SetValue("lastVehicleUsed", ps.lastVehicleUsed);
		configNode.SetValue("totalFlightTime", ConfigNodeUtils.WriteObject(ps.totalFlightTime));
		configNode.SetValue("skinColor", ConfigNodeUtils.WriteVector3(ColorUtils.ToVector3(ps.skinColor)));
		configNode.SetValue("suitColor", ConfigNodeUtils.WriteVector3(ColorUtils.ToVector3(ps.suitColor)));
		configNode.SetValue("vestColor", ConfigNodeUtils.WriteVector3(ColorUtils.ToVector3(ps.vestColor)));
		configNode.SetValue("gSuitColor", ConfigNodeUtils.WriteVector3(ColorUtils.ToVector3(ps.gSuitColor)));
		configNode.SetValue("strapsColor", ConfigNodeUtils.WriteVector3(ColorUtils.ToVector3(ps.strapsColor)));
		return configNode;
	}
}
