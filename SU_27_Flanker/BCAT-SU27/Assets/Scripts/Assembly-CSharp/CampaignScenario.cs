using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CampaignScenario
{
	[Serializable]
	public class BriefingNote
	{
		[TextArea(3, 100)]
		public string note;

		public Texture2D image;

		public AudioClip sound;
	}

	[Serializable]
	public class ForcedEquip
	{
		public int hardpointIdx;

		public string weaponName;
	}

	[Serializable]
	public class EnvironmentOption
	{
		public string envLabel;

		public string envName;

		public EnvironmentOption(string label, string id)
		{
			envLabel = label;
			envName = id;
		}
	}

	public VTScenarioInfo customScenarioInfo;

	[Header("Scenario")]
	public string scenarioName;

	public string scenarioID;

	public bool demoAvailable;

	public bool underConstruction;

	[HideInInspector]
	public bool isTraining;

	[Space]
	[TextArea]
	public string description;

	public float baseBudget;

	public float baseCompletedBudget;

	[HideInInspector]
	public float totalBudget;

	[HideInInspector]
	public float initialSpending;

	[HideInInspector]
	public float inFlightSpending;

	public string mapSceneName;

	public string environmentName = "morning";

	public EnvironmentOption[] envOptions;

	[HideInInspector]
	public int envIdx = -1;

	public Texture2D scenarioImage;

	[TextArea]
	public string recommendedTraining;

	public List<string> scenariosOnComplete;

	public List<string> equipmentOnComplete;

	[Header("Equips")]
	public bool equipConfigurable = true;

	public ForcedEquip[] forcedEquips;

	public float forcedFuel;

	[Header("Briefing")]
	public BriefingNote[] briefingNotes;
}
