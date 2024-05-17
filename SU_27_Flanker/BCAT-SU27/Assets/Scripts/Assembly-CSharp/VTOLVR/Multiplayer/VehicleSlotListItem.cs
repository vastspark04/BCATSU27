using Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class VehicleSlotListItem : MonoBehaviour
{
	public Text titleText;

	public Text vehicleText;

	public Text usernameText;

	public UIMaskedTextScroller usernameMask;

	public RawImage userImg;

	public GameObject emptyObj;

	public GameObject rdyBg;

	public GameObject readyCheck;

	public GameObject noDlcObject;

	public VTOLMPSceneManager.VehicleSlot slot;

	private void OnEnable()
	{
		VTOLMPSceneManager.instance.OnDeclaredReady += Instance_OnDeclaredReady;
		VTOLMPSceneManager.instance.OnMPScenarioStart += Instance_OnMPScenarioStart;
	}

	private void Start()
	{
		rdyBg.SetActive(!VTMapManager.fetch.mpScenarioStart && slot != null && slot.player != null);
	}

	private void Instance_OnMPScenarioStart()
	{
		rdyBg.SetActive(value: false);
	}

	public void Setup(VTOLMPSceneManager.VehicleSlot slot)
	{
		this.slot = slot;
		titleText.text = slot.slotTitle;
		vehicleText.text = slot.vehicleName;
		userImg.gameObject.SetActive(value: false);
		if ((bool)noDlcObject)
		{
			PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(slot.vehicleName);
			if (!playerVehicle || (playerVehicle.dlc && !SteamApps.IsDlcInstalled(playerVehicle.dlcID)))
			{
				noDlcObject.SetActive(value: true);
			}
			else
			{
				noDlcObject.SetActive(value: false);
			}
		}
		if (slot.player != null)
		{
			usernameText.text = slot.player.pilotName;
			usernameMask.gameObject.SetActive(value: true);
			emptyObj.SetActive(value: false);
			readyCheck.SetActive(slot.player.isReady || (slot.player.steamUser.IsMe && VTOLMPLobbyManager.isLobbyHost));
			rdyBg.SetActive(!VTMapManager.fetch.mpScenarioStart);
			VTOLMPLobbyManager.GetUserImageForCallback(slot.player.steamUser.Id, OnLoadedImage);
		}
		else
		{
			emptyObj.SetActive(value: true);
			usernameMask.gameObject.SetActive(value: false);
			readyCheck.SetActive(value: false);
			rdyBg.SetActive(value: false);
		}
	}

	private void OnLoadedImage(SteamId steamId, Texture2D tex)
	{
		if (slot.player != null && (ulong)steamId == (ulong)slot.player.steamUser.Id)
		{
			userImg.texture = tex;
			userImg.gameObject.SetActive(value: true);
		}
	}

	private void Instance_OnDeclaredReady(ulong steamId, bool isReady)
	{
		if (slot != null && slot.player != null && (ulong)slot.player.steamUser.Id == steamId)
		{
			readyCheck.SetActive(isReady);
		}
	}

	private void OnDisable()
	{
		if (VTOLMPSceneManager.instance != null)
		{
			VTOLMPSceneManager.instance.OnDeclaredReady -= Instance_OnDeclaredReady;
			VTOLMPSceneManager.instance.OnMPScenarioStart -= Instance_OnMPScenarioStart;
		}
	}

	public void SelectButton()
	{
		if (slot.player == null)
		{
			PlayerVehicle playerVehicle = VTResources.GetPlayerVehicle(slot.vehicleName);
			if (!playerVehicle || (playerVehicle.dlc && !SteamApps.IsDlcInstalled(playerVehicle.dlcID)))
			{
				if (!playerVehicle)
				{
					Debug.LogError("VehicleSlotListItem.SelectButton -- !v -- vehicleName='" + slot.vehicleName + "'");
				}
				else if (playerVehicle.dlc && !SteamApps.IsDlcInstalled(playerVehicle.dlcID))
				{
					Debug.LogError("VehicleSlotListItem.SelectButton -- DLC not installed");
				}
			}
			else
			{
				VTOLMPSceneManager.instance.RequestSlot(slot);
			}
		}
		else if (slot.player == VTOLMPLobbyManager.localPlayerInfo)
		{
			VTOLMPSceneManager.instance.VacateSlot();
		}
	}
}

}