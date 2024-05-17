using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Campaign : ScriptableObject
{
	[Serializable]
	public class PerVehicleLiveries
	{
		public string vehicleName;

		public Texture2D alliedLivery;

		public Texture2D enemyLivery;
	}

	public string campaignID;

	public string campaignName;

	public bool readyToPlay = true;

	public bool demoAvailable;

	public bool isCustomScenarios;

	public bool isStandaloneScenarios;

	public bool isBuiltIn;

	public bool isSteamworksStandalone;

	[TextArea]
	public string description;

	public Texture2D campaignImage;

	public List<string> weaponsOnStart;

	public List<string> scenariosOnStart;

	public List<CampaignScenario> missions;

	public List<CampaignScenario> trainingMissions;

	[Header("Special")]
	public Texture2D campaignLivery;

	public Texture2D campaignLiveryOpFor;

	public PerVehicleLiveries[] perVehicleLiveries;
}
