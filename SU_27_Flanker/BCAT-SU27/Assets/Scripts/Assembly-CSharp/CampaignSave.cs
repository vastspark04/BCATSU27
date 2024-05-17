using System.Collections.Generic;

public class CampaignSave
{
	public class CompletedScenarioInfo
	{
		public string scenarioID;

		public float earnedBudget;
	}

	public string campaignID;

	public string campaignName;

	public string vehicleName;

	public List<string> availableWeapons;

	public string[] currentWeapons;

	public float currentFuel;

	public List<string> availableScenarios;

	public List<CompletedScenarioInfo> completedScenarios;

	public int lastScenarioIdx;

	public bool lastScenarioWasTraining;

	public static ConfigNode SaveToConfigNode(CampaignSave cs)
	{
		ConfigNode configNode = new ConfigNode("CAMPAIGN");
		configNode.SetValue("campaignName", cs.campaignName);
		configNode.SetValue("campaignID", cs.campaignID);
		configNode.SetValue("vehicleName", cs.vehicleName);
		if (cs.availableWeapons == null)
		{
			cs.availableWeapons = new List<string>();
		}
		configNode.SetValue("availableWeapons", ConfigNodeUtils.WriteList(cs.availableWeapons));
		configNode.SetValue("currentFuel", cs.currentFuel);
		ConfigNode configNode2 = new ConfigNode("currentWeapons");
		for (int i = 0; i < cs.currentWeapons.Length; i++)
		{
			ConfigNode configNode3 = new ConfigNode("weapon");
			configNode3.SetValue("idx", i);
			configNode3.SetValue("weapon", cs.currentWeapons[i]);
			configNode2.AddNode(configNode3);
		}
		configNode.AddNode(configNode2);
		configNode.SetValue("availableScenarios", ConfigNodeUtils.WriteList(cs.availableScenarios));
		foreach (CompletedScenarioInfo completedScenario in cs.completedScenarios)
		{
			ConfigNode configNode4 = new ConfigNode("completedScenario");
			configNode4.SetValue("scenarioID", completedScenario.scenarioID);
			configNode4.SetValue("earnedBudget", completedScenario.earnedBudget);
			configNode.AddNode(configNode4);
		}
		configNode.SetValue("lastScenarioIdx", cs.lastScenarioIdx);
		configNode.SetValue("lastScenarioWasTraining", cs.lastScenarioWasTraining);
		return configNode;
	}

	public static CampaignSave LoadFromConfigNode(ConfigNode cNode)
	{
		CampaignSave campaignSave = new CampaignSave();
		campaignSave.campaignName = cNode.GetValue("campaignName");
		if (cNode.HasValue("campaignID"))
		{
			campaignSave.campaignID = cNode.GetValue("campaignID");
		}
		else
		{
			campaignSave.campaignID = campaignSave.campaignName;
		}
		campaignSave.vehicleName = cNode.GetValue("vehicleName");
		campaignSave.availableScenarios = ConfigNodeUtils.ParseList(cNode.GetValue("availableScenarios"));
		if (cNode.HasValue("currentFuel"))
		{
			campaignSave.currentFuel = ConfigNodeUtils.ParseFloat(cNode.GetValue("currentFuel"));
		}
		else
		{
			campaignSave.currentFuel = 1f;
		}
		campaignSave.availableWeapons = ConfigNodeUtils.ParseList(cNode.GetValue("availableWeapons"));
		int hardpointCount = PilotSaveManager.GetVehicle(campaignSave.vehicleName).hardpointCount;
		campaignSave.currentWeapons = new string[hardpointCount];
		foreach (ConfigNode node in cNode.GetNode("currentWeapons").GetNodes("weapon"))
		{
			int num = ConfigNodeUtils.ParseInt(node.GetValue("idx"));
			if (num < hardpointCount)
			{
				string value = node.GetValue("weapon");
				campaignSave.currentWeapons[num] = value;
			}
		}
		campaignSave.completedScenarios = new List<CompletedScenarioInfo>();
		foreach (ConfigNode node2 in cNode.GetNodes("completedScenario"))
		{
			CompletedScenarioInfo completedScenarioInfo = new CompletedScenarioInfo();
			completedScenarioInfo.scenarioID = node2.GetValue("scenarioID");
			completedScenarioInfo.earnedBudget = ConfigNodeUtils.ParseFloat(node2.GetValue("earnedBudget"));
			campaignSave.completedScenarios.Add(completedScenarioInfo);
		}
		ConfigNodeUtils.TryParseValue(cNode, "lastScenarioWasTraining", ref campaignSave.lastScenarioWasTraining);
		ConfigNodeUtils.TryParseValue(cNode, "lastScenarioIdx", ref campaignSave.lastScenarioIdx);
		return campaignSave;
	}
}
