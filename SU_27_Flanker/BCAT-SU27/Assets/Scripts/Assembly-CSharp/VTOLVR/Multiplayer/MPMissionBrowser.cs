using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPMissionBrowser : MonoBehaviour
{
	public ScrollRect scrollRect;

	public GameObject campaignTemplate;

	public GameObject missionTemplate;

	public Texture2D noImageTex;

	[Header("Selected Item")]
	public Text campaignNameLabel;

	public Text campaignDescription;

	public RawImage campaignImage;

	public Text missionNameLabel;

	public Text missionDescription;

	public RawImage missionImage;

	public RawImage mapImage;

	public GameObject workshopSeparator;

	private Action<VTScenarioInfo> onSelectedScenario;

	private Dictionary<string, bool> expanded = new Dictionary<string, bool>();

	private Dictionary<string, GameObject> campaignObjs = new Dictionary<string, GameObject>();

	private Dictionary<string, GameObject> missionObjs = new Dictionary<string, GameObject>();

	private MPMissionBrowserItem selectedMissionItem;

	private VTScenarioInfo selectedScenario;

	private List<VTCampaignInfo> workshopCampaigns = new List<VTCampaignInfo>();

	public void Open(VTScenarioInfo currentScenario, Action<VTScenarioInfo> onSelectedScenario)
	{
		base.gameObject.SetActive(value: true);
		campaignTemplate.SetActive(value: false);
		missionTemplate.SetActive(value: false);
		UpdateList();
		if (currentScenario != null)
		{
			expanded[currentScenario.campaignID] = true;
			UpdateList();
			SelectMission(currentScenario.campaignID, currentScenario.id);
		}
		else
		{
			campaignNameLabel.text = "--";
			campaignDescription.text = string.Empty;
			campaignImage.texture = noImageTex;
			missionNameLabel.text = "--";
			missionDescription.text = "--";
			missionImage.texture = noImageTex;
			mapImage.texture = noImageTex;
		}
		this.onSelectedScenario = onSelectedScenario;
		if (GameStartup.version.releaseType == GameVersion.ReleaseTypes.Testing)
		{
			StartCoroutine(LoadWorkshopItems());
		}
	}

	private void SelectCampaign(string campaignId)
	{
		bool flag = expanded[campaignId];
		expanded[campaignId] = !flag;
		UpdateList();
	}

	private void SelectMission(string campaignId, string missionId)
	{
		if (selectedMissionItem != null)
		{
			selectedMissionItem.selectedObj.SetActive(value: false);
		}
		string key = campaignId + ":" + missionId;
		if (!missionObjs.ContainsKey(key))
		{
			using Dictionary<string, GameObject>.Enumerator enumerator = missionObjs.GetEnumerator();
			if (enumerator.MoveNext())
			{
				key = enumerator.Current.Key;
			}
		}
		MPMissionBrowserItem component = missionObjs[key].GetComponent<MPMissionBrowserItem>();
		component.selectedObj.SetActive(value: true);
		selectedMissionItem = component;
		campaignNameLabel.text = component.campaign.campaignName;
		campaignDescription.text = component.campaign.description;
		campaignImage.texture = component.campaign.image;
		if (component.campaign.image == null)
		{
			campaignImage.texture = noImageTex;
		}
		missionNameLabel.text = component.mission.name;
		missionDescription.text = component.mission.description;
		missionImage.texture = component.mission.image;
		if (component.mission.image == null)
		{
			missionImage.texture = noImageTex;
		}
		mapImage.texture = VTResources.GetMapForScenario(component.mission, out var _).previewImage;
		if (mapImage.texture == null)
		{
			mapImage.texture = noImageTex;
		}
		selectedScenario = component.mission;
	}

	private void UpdateList()
	{
		List<VTCampaignInfo> list = new List<VTCampaignInfo>();
		foreach (VTCampaignInfo value4 in VTResources.builtInMultiplayerCampaigns.Values)
		{
			list.Add(value4);
		}
		int count = list.Count;
		foreach (VTCampaignInfo workshopCampaign in workshopCampaigns)
		{
			list.Add(workshopCampaign);
		}
		float num = ((RectTransform)campaignTemplate.transform).rect.height * campaignTemplate.transform.localScale.y;
		float num2 = ((RectTransform)missionTemplate.transform).rect.height * missionTemplate.transform.localScale.y;
		float num3 = 0f;
		if ((bool)workshopSeparator)
		{
			workshopSeparator.SetActive(value: false);
		}
		bool flag = false;
		for (int j = 0; j < list.Count; j++)
		{
			if (j == count && (bool)workshopSeparator)
			{
				flag = true;
				workshopSeparator.SetActive(value: true);
				workshopSeparator.transform.localPosition = new Vector3(0f, 0f - num3, 0f);
				num3 += ((RectTransform)workshopSeparator.transform).rect.height * workshopSeparator.transform.localScale.y;
			}
			VTCampaignInfo vTCampaignInfo = list[j];
			if (vTCampaignInfo.hideFromMenu && !Application.isEditor)
			{
				continue;
			}
			if (!campaignObjs.TryGetValue(vTCampaignInfo.campaignID, out var value))
			{
				value = UnityEngine.Object.Instantiate(campaignTemplate, scrollRect.content);
				campaignObjs.Add(vTCampaignInfo.campaignID, value);
				MPMissionBrowserItem item = value.GetComponent<MPMissionBrowserItem>();
				item.nameText.text = vTCampaignInfo.campaignName;
				if (flag)
				{
					item.authorText.text = vTCampaignInfo.workshopAuthor;
				}
				else
				{
					item.authorText.text = "BahamutoD";
				}
				item.countText.text = string.Format("{0} mission{1}", vTCampaignInfo.missionScenarios.Count, (vTCampaignInfo.missionScenarios.Count > 1) ? "s" : "");
				item.thumbnail.texture = vTCampaignInfo.image;
				if (!vTCampaignInfo.image)
				{
					item.thumbnail.texture = noImageTex;
				}
				item.campaign = vTCampaignInfo;
				item.OnSelect = delegate
				{
					SelectCampaign(item.campaign.campaignID);
				};
				value.SetActive(value: true);
			}
			value.transform.localPosition = new Vector3(0f, 0f - num3, 0f);
			num3 += num;
			if (!expanded.TryGetValue(vTCampaignInfo.campaignID, out var value2))
			{
				expanded.Add(vTCampaignInfo.campaignID, value: false);
				value2 = false;
			}
			foreach (VTScenarioInfo i in vTCampaignInfo.missionScenarios)
			{
				string key = vTCampaignInfo.campaignID + ":" + i.id;
				MPMissionBrowserItem component;
				if (!missionObjs.TryGetValue(key, out var value3))
				{
					value3 = UnityEngine.Object.Instantiate(missionTemplate, scrollRect.content);
					missionObjs.Add(key, value3);
					component = value3.GetComponent<MPMissionBrowserItem>();
					component.nameText.text = i.name;
					component.countText.text = $"{VTOLMPUtils.GetMaxPlayerCount(i)} players";
					component.thumbnail.texture = i.image;
					if (!i.image)
					{
						component.thumbnail.texture = noImageTex;
					}
					component.campaign = vTCampaignInfo;
					component.mission = i;
					_ = i;
					component.OnSelect = delegate
					{
						SelectMission(i.campaignID, i.id);
					};
				}
				else
				{
					component = value3.GetComponent<MPMissionBrowserItem>();
				}
				if (value2)
				{
					value3.SetActive(value: true);
					value3.transform.localPosition = new Vector3(0f, 0f - num3, 0f);
					num3 += num2;
					component.selectedObj.SetActive(i == selectedScenario);
				}
				else
				{
					value3.SetActive(value: false);
				}
			}
		}
		scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3);
		scrollRect.ClampVertical();
		VRPointInteractableCanvas componentInParent = GetComponentInParent<VRPointInteractableCanvas>();
		if ((bool)componentInParent)
		{
			componentInParent.RefreshInteractables();
		}
	}

	private IEnumerator LoadWorkshopItems()
	{
		workshopCampaigns.Clear();
		Task<ResultPage?> task = Query.Items.WhereUserSubscribed(SteamClient.SteamId).WithTag("Multiplayer Campaigns").GetPageAsync(1);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.Result.HasValue)
		{
			foreach (Item entry in task.Result.Value.Entries)
			{
				if (entry.IsInstalled)
				{
					VTCampaignInfo item = VTResources.LoadWorkshopCampaign(entry);
					workshopCampaigns.Add(item);
				}
			}
		}
		UpdateList();
	}

	public void Close()
	{
		base.gameObject.SetActive(value: false);
		onSelectedScenario?.Invoke(selectedScenario);
	}
}

}