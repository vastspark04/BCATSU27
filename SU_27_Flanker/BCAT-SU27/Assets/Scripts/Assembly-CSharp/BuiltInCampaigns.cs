using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BuiltInCampaigns : ScriptableObject
{
	public List<SerializedCampaign> campaigns;

	public List<SerializedCampaign> tutorials;

	public List<SerializedCampaign> multiplayerCampaigns;

	private Dictionary<string, SerializedCampaign> campaignDictionary;

	public SerializedCampaign GetCampaign(string campaignID)
	{
		if (campaignDictionary == null || campaignDictionary.Count == 0)
		{
			CreateDictionary();
		}
		return campaignDictionary[campaignID];
	}

	private void CreateDictionary()
	{
		campaignDictionary = new Dictionary<string, SerializedCampaign>();
		foreach (SerializedCampaign campaign in campaigns)
		{
			campaignDictionary.Add(campaign.campaignID, campaign);
		}
		foreach (SerializedCampaign tutorial in tutorials)
		{
			campaignDictionary.Add(tutorial.campaignID, tutorial);
		}
		foreach (SerializedCampaign multiplayerCampaign in multiplayerCampaigns)
		{
			campaignDictionary.Add(multiplayerCampaign.campaignID, multiplayerCampaign);
		}
	}
}
