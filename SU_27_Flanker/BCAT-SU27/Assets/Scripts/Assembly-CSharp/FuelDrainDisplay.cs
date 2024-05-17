using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;
using VTOLVR.Multiplayer;

public class FuelDrainDisplay : VTNetSyncRPCOnly
{
	public MMFDFuelDrainPage master;

	public MultiUserVehicleSync muvs;

	public MeasurementManager measurements;

	public Text fuelDrainText;

	public Text estTimeText;

	public Text estDistanceText;

	private bool getLocally = true;

	private bool sendUpdates;

	private float drainPerMinute;

	private float timeSeconds;

	private float meters;

	protected override void Awake()
	{
	}

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			while (!wasRegistered)
			{
				yield return null;
			}
			getLocally = base.isMine;
			sendUpdates = base.isMine;
		}
		else
		{
			getLocally = true;
		}
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (base.enabled)
		{
			if (getLocally)
			{
				drainPerMinute = master.drainPerMinute;
				timeSeconds = master.estTimeSeconds;
				meters = master.estDistMeters;
			}
			if (drainPerMinute > 0f)
			{
				string text = string.Format("{0} L/min", drainPerMinute.ToString("0.00"));
				fuelDrainText.text = text;
				TimeSpan timeSpan = new TimeSpan(0, 0, 0, Mathf.RoundToInt(timeSeconds));
				string text2 = string.Format("{0}:{1}:{2}", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00"));
				estTimeText.text = text2;
				if (meters > 0f)
				{
					string text3 = measurements.FormattedDistance(meters);
					estDistanceText.text = text3;
				}
				else
				{
					estDistanceText.text = $"-.- {measurements.SpeedLabel()}";
				}
			}
			else
			{
				fuelDrainText.text = "-.-- L/min";
				estTimeText.text = "--:--:--";
				estDistanceText.text = $"-.- {measurements.SpeedLabel()}";
			}
			if (sendUpdates)
			{
				muvs.SendRPCToCopilots(this, "RPC_S", drainPerMinute, timeSeconds, meters);
			}
			yield return wait;
		}
	}

	[VTRPC]
	private void RPC_S(float drain, float time, float dist)
	{
		drainPerMinute = drain;
		timeSeconds = time;
		meters = dist;
	}
}
