using UnityEngine;
using UnityEngine.UI;
using VTOLVR.DLC.Rotorcraft;

public class HMDAltitude : MonoBehaviour, IPilotReceiverHandler
{
	public Text text;

	public bool noComma;

	private MeasurementManager measurements;

	private FlightInfo flightInfo;

	private VehicleMaster vm;

	public bool forceRadar;

	public bool forceBaro;

	private bool radarMode => vm.useRadarAlt;

	private void Awake()
	{
		vm = GetComponentInParent<VehicleMaster>();
	}

	private void Start()
	{
		flightInfo = GetComponentInParent<FlightInfo>();
		measurements = flightInfo.GetComponentInChildren<MeasurementManager>();
	}

	private void Update()
	{
		if ((bool)flightInfo)
		{
			float altitude = (forceBaro ? flightInfo.altitudeASL : ((!forceRadar) ? (radarMode ? flightInfo.radarAltitude : flightInfo.altitudeASL) : flightInfo.radarAltitude));
			if (noComma)
			{
				text.text = Mathf.RoundToInt(measurements.ConvertedAltitude(altitude)).ToString();
			}
			else
			{
				text.text = measurements.FormattedAltitude(altitude);
			}
		}
	}

	public void ToggleAltitudeMode()
	{
		vm.ToggleRadarAltMode();
	}

	public void OnPilotReceiver(AH94PilotReceiver receiver)
	{
		flightInfo = receiver.flightInfo;
		vm = receiver.flightInfo.GetComponent<VehicleMaster>();
		measurements = receiver.flightInfo.GetComponent<MeasurementManager>();
	}
}
