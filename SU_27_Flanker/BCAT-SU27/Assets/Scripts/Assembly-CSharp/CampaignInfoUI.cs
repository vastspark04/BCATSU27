using UnityEngine;
using UnityEngine.UI;

public class CampaignInfoUI : MonoBehaviour, ILocalizationUser
{
	public Text campaignName;

	public Text campaignDescription;

	public RawImage campaignImage;

	public Text progressText;

	public Color hundredPercentColor;

	private string s_cInfo_completed = "Completed";

	public void ApplyLocalization()
	{
		s_cInfo_completed = VTLocalizationManager.GetString("s_cInfo_completed", "Completed", "'x% [Completed]' text in campaign selector");
	}

	public void UpdateDisplay(Campaign c, string vehicleID)
	{
		ApplyLocalization();
		campaignName.text = c.campaignName;
		campaignDescription.text = c.description;
		if ((bool)c.campaignImage)
		{
			campaignImage.texture = c.campaignImage;
		}
		if (c.isCustomScenarios && c.isStandaloneScenarios)
		{
			progressText.gameObject.SetActive(value: false);
			return;
		}
		float value = 0f;
		CampaignSave campaignSave = PilotSaveManager.current.GetVehicleSave(vehicleID).GetCampaignSave(c.campaignID);
		if (campaignSave != null && c.missions.Count > 0)
		{
			float num = 0f;
			foreach (CampaignSave.CompletedScenarioInfo completedScenario in campaignSave.completedScenarios)
			{
				foreach (CampaignScenario mission in c.missions)
				{
					if (completedScenario.scenarioID == mission.scenarioID)
					{
						num += 1f;
						break;
					}
				}
			}
			value = num / (float)c.missions.Count;
		}
		int num2 = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
		progressText.text = $"{num2.ToString()}% {s_cInfo_completed}";
		if (num2 == 100)
		{
			progressText.color = hundredPercentColor;
		}
	}
}
