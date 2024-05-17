using UnityEngine;
using UnityEngine.UI;
using LapinerTools.uMyGUI;
using LapinerTools.Steam.Data;

public class VTSteamWorkshopBrowser : MonoBehaviour
{
	public GameObject displayObj;
	public VTSteamWorkshopUIBrowse uiBrowse;
	public CampaignSelectorUI campaignSelector;
	public VTUIErrorWindow errorWindow;
	public GameObject mainMenuDisplayObj;
	public GameObject vehicleSelectDisplayObj;
	public GameObject loadingObj;
	public Text tagsText;
	public Text missionTypeText;
	public uMyGUI_PageBox pageBox;
	public VTWorkshopItemInfoPage itemInfoPage;
	public EWorkshopSource source;
}
