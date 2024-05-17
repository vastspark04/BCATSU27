using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class VehicleSelectUI : MonoBehaviour
{
	public Text vehicleName;

	public Text vehicleDescription;

	public RawImage vehicleImage;

	public GameObject dlcNotOwnedObj;

	public void UpdateUI(PlayerVehicle v)
	{
		vehicleName.text = v.vehicleName;
		vehicleDescription.text = v.GetLocalizedDescription();
		if ((bool)v.vehicleImage)
		{
			vehicleImage.texture = v.vehicleImage;
		}
		if (v.dlc)
		{
			dlcNotOwnedObj.SetActive(!SteamApps.IsDlcInstalled(v.dlcID) || !v.vehiclePrefab);
		}
		else
		{
			dlcNotOwnedObj.SetActive(value: false);
		}
	}
}
