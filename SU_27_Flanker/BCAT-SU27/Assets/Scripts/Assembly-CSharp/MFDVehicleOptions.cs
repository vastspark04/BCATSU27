using System;
using UnityEngine;

public class MFDVehicleOptions : MonoBehaviour
{
	private MeasurementManager measurements;

	private MFDPage mfdPage;

	private MFDPortalPage portalPage;

	private int altCount;

	private int distCount;

	private int spdCount;

	private bool wasSetUp;

	private void Awake()
	{
		Setup();
	}

	private void Setup()
	{
		if (!wasSetUp)
		{
			wasSetUp = true;
			mfdPage = GetComponent<MFDPage>();
			portalPage = GetComponent<MFDPortalPage>();
			measurements = GetComponentInParent<MeasurementManager>();
			altCount = Enum.GetNames(typeof(MeasurementManager.AltitudeModes)).Length;
			distCount = Enum.GetNames(typeof(MeasurementManager.DistanceModes)).Length;
			spdCount = Enum.GetNames(typeof(MeasurementManager.SpeedModes)).Length;
		}
	}

	private void Start()
	{
		UpdateDisplay();
	}

	public void ToggleAltMode()
	{
		int altitudeMode = (int)measurements.altitudeMode;
		altitudeMode = (altitudeMode + 1) % altCount;
		measurements.altitudeMode = (MeasurementManager.AltitudeModes)altitudeMode;
		UpdateDisplay();
	}

	public void ToggleDistMode()
	{
		int distanceMode = (int)measurements.distanceMode;
		distanceMode = (distanceMode + 1) % distCount;
		measurements.distanceMode = (MeasurementManager.DistanceModes)distanceMode;
		UpdateDisplay();
	}

	public void ToggleSpeedMode()
	{
		int airspeedMode = (int)measurements.airspeedMode;
		airspeedMode = (airspeedMode + 1) % spdCount;
		measurements.airspeedMode = (MeasurementManager.SpeedModes)airspeedMode;
		if (measurements.airspeedMode == MeasurementManager.SpeedModes.Mach)
		{
			ToggleSpeedMode();
		}
		else
		{
			UpdateDisplay();
		}
	}

	private void UpdateDisplay()
	{
		string text = string.Empty;
		switch (measurements.altitudeMode)
		{
		case MeasurementManager.AltitudeModes.Meters:
			text = "meters";
			break;
		case MeasurementManager.AltitudeModes.Feet:
			text = "feet";
			break;
		}
		string text2 = string.Empty;
		switch (measurements.distanceMode)
		{
		case MeasurementManager.DistanceModes.Meters:
			text2 = "meters";
			break;
		case MeasurementManager.DistanceModes.Feet:
			text2 = "feet";
			break;
		case MeasurementManager.DistanceModes.Miles:
			text2 = "miles";
			break;
		case MeasurementManager.DistanceModes.NautMiles:
			text2 = "naut miles";
			break;
		}
		string text3 = string.Empty;
		switch (measurements.airspeedMode)
		{
		case MeasurementManager.SpeedModes.FeetPerSecond:
			text3 = "ft/s";
			break;
		case MeasurementManager.SpeedModes.KilometersPerHour:
			text3 = "KPH";
			break;
		case MeasurementManager.SpeedModes.Knots:
			text3 = "knots";
			break;
		case MeasurementManager.SpeedModes.MetersPerSecond:
			text3 = "m/s";
			break;
		case MeasurementManager.SpeedModes.MilesPerHour:
			text3 = "MPH";
			break;
		case MeasurementManager.SpeedModes.Mach:
			text3 = "Mach";
			break;
		}
		if ((bool)mfdPage)
		{
			mfdPage.SetText("AltModeText", text);
			mfdPage.SetText("DistModeText", text2);
			mfdPage.SetText("SpeedModeText", text3);
		}
		else if ((bool)portalPage)
		{
			portalPage.SetText("AltModeText", text);
			portalPage.SetText("DistModeText", text2);
			portalPage.SetText("SpeedModeText", text3);
		}
	}
}
