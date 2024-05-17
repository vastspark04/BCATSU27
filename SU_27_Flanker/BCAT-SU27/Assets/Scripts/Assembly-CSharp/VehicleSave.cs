using System.Collections.Generic;
using UnityEngine;

public class VehicleSave
{
	public string vehicleName;

	public float seatHeight;

	public Vector3 joystickPosition = new Vector3(0.5f, 0.5f, 0.5f);

	public Vector3 throttlePosition = Vector3.zero;

	public MeasurementManager.AltitudeModes altitudeMode = MeasurementManager.AltitudeModes.Feet;

	public MeasurementManager.DistanceModes distanceMode = MeasurementManager.DistanceModes.NautMiles;

	public MeasurementManager.SpeedModes airspeedMode = MeasurementManager.SpeedModes.Knots;

	public Dictionary<string, ConfigNode> savedLoadouts = new Dictionary<string, ConfigNode>();

	public List<CampaignSave> campaignSaves;

	public ConfigNode vehicleDataNode;

	public CampaignSave GetCampaignSave(string campaignID)
	{
		foreach (CampaignSave campaignSafe in campaignSaves)
		{
			if (campaignSafe.campaignID == campaignID)
			{
				return campaignSafe;
			}
		}
		return null;
	}

	public static ConfigNode SaveToConfigNode(VehicleSave vs)
	{
		ConfigNode configNode = new ConfigNode("VEHICLE");
		configNode.SetValue("vehicleName", vs.vehicleName);
		configNode.SetValue("seatHeight", vs.seatHeight);
		configNode.SetValue("joystickPosition", ConfigNodeUtils.WriteVector3(vs.joystickPosition));
		configNode.SetValue("throttlePosition", ConfigNodeUtils.WriteVector3(vs.throttlePosition));
		configNode.SetValue("altitudeMode", ConfigNodeUtils.WriteObject(vs.altitudeMode));
		configNode.SetValue("distanceMode", ConfigNodeUtils.WriteObject(vs.distanceMode));
		configNode.SetValue("airspeedMode", ConfigNodeUtils.WriteObject(vs.airspeedMode));
		if (vs.vehicleDataNode != null)
		{
			configNode.AddNode(vs.vehicleDataNode);
		}
		foreach (CampaignSave campaignSafe in vs.campaignSaves)
		{
			ConfigNode node = CampaignSave.SaveToConfigNode(campaignSafe);
			configNode.AddNode(node);
		}
		ConfigNode configNode2 = configNode.AddNode("SavedLoadouts");
		foreach (ConfigNode value in vs.savedLoadouts.Values)
		{
			configNode2.AddNode(value);
		}
		return configNode;
	}

	public static VehicleSave LoadFromConfigNode(ConfigNode vNode)
	{
		VehicleSave vehicleSave = new VehicleSave();
		vehicleSave.vehicleName = vNode.GetValue("vehicleName");
		if (vNode.HasValue("seatHeight"))
		{
			vehicleSave.seatHeight = ConfigNodeUtils.ParseFloat(vNode.GetValue("seatHeight"));
		}
		if (vNode.HasValue("joystickPosition"))
		{
			vehicleSave.joystickPosition = ConfigNodeUtils.ParseVector3(vNode.GetValue("joystickPosition"));
		}
		if (vNode.HasValue("throttlePosition"))
		{
			vehicleSave.throttlePosition = ConfigNodeUtils.ParseVector3(vNode.GetValue("throttlePosition"));
		}
		if (vNode.HasValue("altitudeMode"))
		{
			vehicleSave.altitudeMode = ConfigNodeUtils.ParseEnum<MeasurementManager.AltitudeModes>(vNode.GetValue("altitudeMode"));
		}
		if (vNode.HasValue("distanceMode"))
		{
			vehicleSave.distanceMode = ConfigNodeUtils.ParseEnum<MeasurementManager.DistanceModes>(vNode.GetValue("distanceMode"));
		}
		if (vNode.HasValue("airspeedMode"))
		{
			vehicleSave.airspeedMode = ConfigNodeUtils.ParseEnum<MeasurementManager.SpeedModes>(vNode.GetValue("airspeedMode"));
		}
		vehicleSave.campaignSaves = new List<CampaignSave>();
		foreach (ConfigNode node in vNode.GetNodes("CAMPAIGN"))
		{
			CampaignSave item = CampaignSave.LoadFromConfigNode(node);
			vehicleSave.campaignSaves.Add(item);
		}
		if (vNode.HasNode("VDATA"))
		{
			vehicleSave.vehicleDataNode = vNode.GetNode("VDATA");
		}
		else
		{
			vehicleSave.vehicleDataNode = new ConfigNode("VDATA");
		}
		vehicleSave.savedLoadouts = new Dictionary<string, ConfigNode>();
		if (vNode.HasNode("SavedLoadouts"))
		{
			foreach (ConfigNode node2 in vNode.GetNode("SavedLoadouts").GetNodes("SavedLoadout"))
			{
				string value = node2.GetValue("name");
				vehicleSave.savedLoadouts.Add(value, node2);
			}
			return vehicleSave;
		}
		return vehicleSave;
	}
}
