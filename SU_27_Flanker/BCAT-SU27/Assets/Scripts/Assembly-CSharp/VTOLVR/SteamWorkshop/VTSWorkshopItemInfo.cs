using System;
using Steamworks.Ugc;
using UnityEngine;

namespace VTOLVR.SteamWorkshop{

public class VTSWorkshopItemInfo
{
	public string title;

	public string description;

	public string ownerName;

	public string directory;

	private bool _disposed;

	private Texture2D _pImage;

	public Action<Texture2D> onImageUpdated;

	public Item item;

	public bool disposed { get; }

	public Texture2D previewImage
	{
		get
		{
			return _pImage;
		}
		set
		{
			_pImage = value;
			onImageUpdated?.Invoke(value);
		}
	}

	public int votesUp => (int)item.VotesUp;

	public int votesDown => (int)item.VotesDown;

	public int numFavorites => (int)item.NumFavorites;

	public int numSubscriptions => (int)item.NumUniqueSubscriptions;

	public bool isSubscribed => item.IsSubscribed;

	public VTSWorkshopItemInfo(Item item)
	{
		this.item = item;
		title = item.Title;
		description = item.Description;
		if (TrySetByLine("\n\nby ") || TrySetByLine("\r\n\r\nby"))
		{
			description = description.Replace("by " + ownerName, "");
		}
		directory = item.Directory;
	}

	private bool TrySetByLine(string splitter)
	{
		if (item.Description.Contains(splitter))
		{
			string[] array = item.Description.Split(new string[1] { splitter }, StringSplitOptions.RemoveEmptyEntries);
			ownerName = array[array.Length - 1];
			ownerName = ownerName.Trim('\n', '\r', ' ');
			if (ownerName.Contains("\n"))
			{
				ownerName = ownerName.Substring(0, ownerName.IndexOf('\n'));
			}
			return true;
		}
		return false;
	}

	public void Dispose()
	{
		_disposed = true;
		if ((bool)_pImage)
		{
			UnityEngine.Object.Destroy(_pImage);
		}
	}
}

}