using System;
using System.Collections;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.SteamWorkshop;

public class VTWorkshopItemListEntry : MonoBehaviour
{
	public VTSteamWorkshopBrowser browser;

	private VTSWorkshopItemInfo item;

	public Text titleText;

	public Text ownerText;

	public Text descriptionText;

	public Text progressText;

	public Text votesText;

	public Text faveCountText;

	public Text subCountText;

	public RawImage previewImage;

	public int maxDescCharCount = 140;

	public GameObject playButtonObj;

	public GameObject addButtonObj;

	public GameObject removeButtonObj;

	public GameObject upvoteButton;

	public GameObject upvoteActiveButton;

	public GameObject downvoteButton;

	public GameObject downvoteActiveButton;

	public GameObject faveButton;

	public GameObject faveButtonActive;

	private Texture origTex;

	private bool updatingVotes;

	public event Action<Item> onItemDownloaded;

	private void Awake()
	{
		origTex = previewImage.texture;
		upvoteButton.SetActive(value: true);
		upvoteActiveButton.SetActive(value: false);
		downvoteButton.SetActive(value: true);
		downvoteActiveButton.SetActive(value: false);
	}

	public void SetItem(VTSWorkshopItemInfo item, VTSteamWorkshopBrowser browser)
	{
		this.item = item;
		this.browser = browser;
		titleText.text = item.title;
		ownerText.text = (string.IsNullOrEmpty(item.ownerName) ? string.Empty : ("by " + item.ownerName));
		progressText.text = "0%";
		string text = item.description.Replace("\r\n", " ").Replace("\n", " ");
		if (text.Length > maxDescCharCount)
		{
			text = TruncateDescription(text);
		}
		descriptionText.text = text;
		if ((bool)item.previewImage)
		{
			previewImage.texture = item.previewImage;
		}
		else
		{
			item.onImageUpdated = (Action<Texture2D>)Delegate.Combine(item.onImageUpdated, new Action<Texture2D>(OnImageUpdated));
		}
		UpdateItem();
		if (item.item.IsDownloading)
		{
			BeginUpdatingDownload();
		}
	}

	private void OnDestroy()
	{
		if (item != null)
		{
			VTSWorkshopItemInfo vTSWorkshopItemInfo = item;
			vTSWorkshopItemInfo.onImageUpdated = (Action<Texture2D>)Delegate.Remove(vTSWorkshopItemInfo.onImageUpdated, new Action<Texture2D>(OnImageUpdated));
		}
	}

	private void OnImageUpdated(Texture2D img)
	{
		if (img != null)
		{
			previewImage.texture = img;
		}
	}

	private string TruncateDescription(string dTxt)
	{
		if (dTxt.Length > maxDescCharCount)
		{
			dTxt = dTxt.Substring(0, maxDescCharCount);
			bool flag = true;
			while (flag)
			{
				flag = false;
				if (dTxt.EndsWith("\r\n") && dTxt.Length > 2)
				{
					dTxt = dTxt.Substring(0, dTxt.Length - 2);
				}
				else if ((dTxt.EndsWith("\n") || dTxt.EndsWith(" ")) && dTxt.Length > 1)
				{
					dTxt = dTxt.Substring(0, dTxt.Length - 1);
				}
				else
				{
					flag = false;
				}
			}
			dTxt = dTxt.Replace('\n', ' ') + "...";
		}
		return dTxt;
	}

	private void UpdateItem()
	{
		votesText.text = $"{item.votesUp} / {item.votesDown}";
		faveCountText.text = item.numFavorites.ToString();
		subCountText.text = item.numSubscriptions.ToString();
		bool isSubscribed = item.isSubscribed;
		addButtonObj.SetActive(!isSubscribed);
		removeButtonObj.SetActive(isSubscribed && item.item.IsInstalled);
		bool flag = item.item.Tags.Contains("multiplayer campaigns");
		if ((bool)playButtonObj)
		{
			playButtonObj.SetActive(!item.item.IsDownloading && item.item.IsInstalled && isSubscribed && !flag);
		}
		if (item.item.IsDownloading)
		{
			progressText.gameObject.SetActive(value: true);
		}
		else
		{
			progressText.gameObject.SetActive(value: false);
		}
		UpdateVotes();
	}

	private void UpdateVotes()
	{
		if (!updatingVotes)
		{
			StartCoroutine(UpdateVotesRoutine());
		}
	}

	private IEnumerator UpdateVotesRoutine()
	{
		updatingVotes = true;
		Task<UserItemVote?> task = item.item.GetUserVote();
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (task.Result.HasValue)
		{
			if (task.Result.Value.VotedDown)
			{
				upvoteButton.SetActive(value: true);
				upvoteActiveButton.SetActive(value: false);
				downvoteButton.SetActive(value: false);
				downvoteActiveButton.SetActive(value: true);
			}
			else if (task.Result.Value.VotedUp)
			{
				upvoteButton.SetActive(value: false);
				upvoteActiveButton.SetActive(value: true);
				downvoteButton.SetActive(value: true);
				downvoteActiveButton.SetActive(value: false);
			}
			else
			{
				upvoteButton.SetActive(value: true);
				upvoteActiveButton.SetActive(value: false);
				downvoteButton.SetActive(value: true);
				downvoteActiveButton.SetActive(value: false);
			}
		}
		updatingVotes = false;
	}

	private void BeginUpdatingDownload()
	{
		progressText.gameObject.SetActive(value: true);
		StartCoroutine(DownloadProgressRoutine());
	}

	private IEnumerator DownloadProgressRoutine()
	{
		while (!item.item.IsInstalled)
		{
			progressText.text = $"{Mathf.Round(item.item.DownloadAmount * 100f)}%";
			UpdateItem();
			yield return null;
		}
		progressText.gameObject.SetActive(value: false);
		playButtonObj.SetActive(value: true);
		removeButtonObj.SetActive(value: true);
		UpdateItem();
	}

	public void InfoButton()
	{
		
		{
			
		}
	}

	public void UpvoteButton()
	{
		StartCoroutine(VoteRoutine(up: true));
	}

	public void DownvoteButton()
	{
		StartCoroutine(VoteRoutine(up: false));
	}

	private IEnumerator VoteRoutine(bool up)
	{
		Task<Result?> task = item.item.Vote(up);
		while (!task.IsCompleted)
		{
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		UpdateItem();
	}

	public void AddButton()
	{
		StartCoroutine(AddRoutine());
	}

	private IEnumerator AddRoutine()
	{
		addButtonObj.SetActive(value: false);
		VTSteamWorkshopUtils.ItemDownload task = VTSteamWorkshopUtils.AsyncSubscribeItem(item.item, OnItemDownloaded);
		while (!task.isDone)
		{
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		UpdateItem();
		BeginUpdatingDownload();
	}

	private void OnItemDownloaded(Item item)
	{
		this.onItemDownloaded?.Invoke(item);
	}

	public void RemoveButton()
	{
		StartCoroutine(UnSubRoutine());
	}

	private IEnumerator UnSubRoutine()
	{
		removeButtonObj.SetActive(value: false);
		Task<bool> task = item.item.Unsubscribe();
		while (!task.IsCompleted)
		{
			yield return null;
		}
		yield return new WaitForSeconds(0.5f);
		UpdateItem();
	}

	public void PlayButton()
	{
		string text = item.item.Id.Value.ToString();
		if (item.item.Tags.Contains("campaigns"))
		{
			
		}
		VTResources.LoadWorkshopSingleScenario(item.item);
	}

	public void FavoriteButton()
	{
	}

	public void DisposeItem()
	{
		item.Dispose();
	}
}
