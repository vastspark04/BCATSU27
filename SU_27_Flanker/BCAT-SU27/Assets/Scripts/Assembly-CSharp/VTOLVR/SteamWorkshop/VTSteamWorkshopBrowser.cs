using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace VTOLVR.SteamWorkshop{

public class VTSteamWorkshopBrowser : MonoBehaviour
{
	private enum SortModes
	{
		Subs,
		Votes,
		Newest
	}

	public GameObject mainDisplayObj;

	public VTWorkshopItemInfoPage infoPage;

	public GameObject itemEntryTemplate;

	public ScrollRect scrollRect;

	public bool showSubscribedOnly;

	public GameObject subbedOnlyIndicator;

	public VTWorkshopPagingControl pagingControl;

	public Text tagsText;

	public List<string> tags;

	public bool matchAllTags;

	public GameObject loadingBlockerObj;

	private List<VTWorkshopItemListEntry> itemObjs = new List<VTWorkshopItemListEntry>();

	private int displayedPage = -1;

	private const int MAX_IMG_REQUESTS = 6;

	private int imageRequestActive;

	private SortModes sortMode;

	protected virtual void Awake()
	{
		if ((bool)subbedOnlyIndicator)
		{
			subbedOnlyIndicator.SetActive(showSubscribedOnly);
		}
	}

	protected virtual void OnItemDownloaded(Item item)
	{
	}

	private void OnEnable()
	{
		if (displayedPage > 0)
		{
			DisplayPage(displayedPage);
		}
	}

	public void ClearTags()
	{
		if (tags != null)
		{
			tags.Clear();
		}
		else
		{
			tags = new List<string>();
		}
	}

	public void AddTag(string s)
	{
		tags.Add(s);
	}

	public void DisplayPage(int page)
	{
		StartCoroutine(DisplayPageRoutine(page));
	}

	private IEnumerator DisplayPageRoutine(int page)
	{
		displayedPage = page;
		loadingBlockerObj.SetActive(value: true);
		foreach (VTWorkshopItemListEntry itemObj2 in itemObjs)
		{
			itemObj2.DisposeItem();
			Object.Destroy(itemObj2.gameObject);
		}
		itemObjs.Clear();
		Query query = Query.Items;
		if (showSubscribedOnly)
		{
			query = query.WhereUserSubscribed(SteamClient.SteamId);
		}
		switch (sortMode)
		{
		case SortModes.Subs:
			query.RankedByTotalUniqueSubscriptions();
			break;
		case SortModes.Votes:
			query = query.SortByVoteScore();
			break;
		case SortModes.Newest:
			query.RankedByPublicationDate();
			break;
		}
		string text = string.Empty;
		if (tags != null)
		{
			for (int j = 0; j < tags.Count; j++)
			{
				text += tags[j];
				if (j + 1 < tags.Count)
				{
					text += "|";
				}
				query = query.WithTag(tags[j]);
			}
			if (matchAllTags)
			{
				query = query.MatchAllTags();
			}
		}
		PlayerVehicle[] playerVehicles = VTResources.GetPlayerVehicles();
		foreach (PlayerVehicle playerVehicle in playerVehicles)
		{
			if (playerVehicle.dlc && !playerVehicle.IsDLCOwned())
			{
				query = query.WithoutTag(playerVehicle.vehicleName);
			}
		}
		tagsText.text = text;
		Task<ResultPage?> task = query.GetPageAsync(page);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.Result.HasValue)
		{
			float height = ((RectTransform)itemEntryTemplate.transform).rect.height * itemEntryTemplate.transform.localScale.y;
			int i = 0;
			foreach (Item entry in task.Result.Value.Entries)
			{
				GameObject itemObj = Object.Instantiate(itemEntryTemplate, scrollRect.content);
				itemObj.SetActive(value: false);
				itemObj.transform.localPosition = new Vector3(0f, (float)(-i) * height, 0f);
				VTWorkshopItemListEntry s = itemObj.GetComponent<VTWorkshopItemListEntry>();
				s.onItemDownloaded += OnItemDownloaded;
				VTSWorkshopItemInfo vtItem = new VTSWorkshopItemInfo(entry);
				StartCoroutine(GetItemImageRoutine(entry, vtItem));
				yield return null;
				itemObj.SetActive(value: true);
				
				itemObjs.Add(s);
				i++;
				scrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)i * height);
			}
			scrollRect.verticalNormalizedPosition = 1f;
			int numPages = task.Result.Value.TotalCount / 50 + 1;
			pagingControl.SetPage(page, numPages);
		}
		loadingBlockerObj.SetActive(value: false);
		VRPointInteractableCanvas componentInParent = GetComponentInParent<VRPointInteractableCanvas>();
		if ((bool)componentInParent)
		{
			componentInParent.RefreshInteractables();
		}
	}

	private IEnumerator GetItemImageRoutine(Item item, VTSWorkshopItemInfo vtItem)
	{
		while (imageRequestActive >= 6)
		{
			yield return null;
		}
		imageRequestActive++;
		UnityWebRequest req = UnityWebRequestTexture.GetTexture(item.PreviewImageUrl);
		req.SendWebRequest();
		float t = Time.realtimeSinceStartup;
		bool timeout = false;
		while (!req.isDone && !timeout)
		{
			yield return null;
			if (Time.realtimeSinceStartup - t > 5f || vtItem.disposed)
			{
				timeout = true;
			}
		}
		if (req.result == UnityWebRequest.Result.Success && !timeout)
		{
			Texture2D texture2D = (vtItem.previewImage = ((DownloadHandlerTexture)req.downloadHandler).texture);
		}
		req.Dispose();
		imageRequestActive--;
	}

	public void ToggleInstalledOnly()
	{
		showSubscribedOnly = !showSubscribedOnly;
		subbedOnlyIndicator.SetActive(showSubscribedOnly);
		DisplayPage(1);
	}

	public void SortBySubs()
	{
		sortMode = SortModes.Subs;
		DisplayPage(1);
	}

	public void SortByVotes()
	{
		sortMode = SortModes.Votes;
		DisplayPage(1);
	}

	public void SortByNewest()
	{
		sortMode = SortModes.Newest;
		DisplayPage(1);
	}

	private void OnDisable()
	{
		foreach (VTWorkshopItemListEntry itemObj in itemObjs)
		{
			itemObj.DisposeItem();
			Object.Destroy(itemObj.gameObject);
		}
		itemObjs.Clear();
		imageRequestActive = 0;
	}
}

}