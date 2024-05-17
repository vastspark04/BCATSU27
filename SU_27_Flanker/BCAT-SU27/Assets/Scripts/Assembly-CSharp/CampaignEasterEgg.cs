using System;
using UnityEngine;

public class CampaignEasterEgg : MonoBehaviour
{
	public GameObject displayObj;

	public PlayerVehicle vehicle;

	public bool useDate;

	public int year;

	public int month;

	public int day;

	public int range;

	private void OnEnable()
	{
		DateTime dateTime = new DateTime(year, month, day);
		if (PilotSaveManager.currentVehicle != null && PilotSaveManager.currentVehicle.vehicleName == vehicle.vehicleName && (!useDate || (DateTime.Now.Date >= dateTime && DateTime.Now.Date < dateTime.AddDays(range))))
		{
			displayObj.SetActive(value: true);
		}
		else
		{
			displayObj.SetActive(value: false);
		}
	}
}
