using Steamworks.Ugc;
using UnityEngine;

namespace VTOLVR.SteamWorkshop{

public class VTSteamWorkshopBrowserVTEdit : VTSteamWorkshopBrowser
{
	public GameObject mainMenuObj;

	protected override void OnItemDownloaded(Item item)
	{
		base.OnItemDownloaded(item);
		VTResources.SetWorkshopMapsDirty();
	}

	public void MapsButton()
	{
		ClearTags();
		tags.Add("Maps");
		mainMenuObj.SetActive(value: false);
		mainDisplayObj.SetActive(value: true);
		DisplayPage(1);
	}

	public void BackToMenuButton()
	{
		mainMenuObj.SetActive(value: true);
		mainDisplayObj.SetActive(value: false);
	}
}

}