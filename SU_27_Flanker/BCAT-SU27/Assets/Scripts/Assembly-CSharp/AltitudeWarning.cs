public class AltitudeWarning : GenericCommonFlightWarning, IPersistentVehicleData
{
	public GearAnimator landingGear;

	public float minSpeed;

	public bool useRadarAlt = true;

	public float minAltitude = 100f;

	public float maxAltitude = 8000f;

	private bool armed;

	private const string NODE_NAME = "AltitudeWarningSettings";

	private void Awake()
	{
		warning = FlightWarnings.CommonWarnings.Altitude;
	}

	protected override void Update()
	{
		base.Update();
		if (flightInfo.airspeed < minSpeed || landingGear.state == GearAnimator.GearStates.Extended)
		{
			doWarning = false;
			armed = false;
			return;
		}
		float num = (useRadarAlt ? flightInfo.radarAltitude : flightInfo.altitudeASL);
		if (armed && (num < minAltitude || num > maxAltitude))
		{
			doWarning = true;
		}
		if (num > minAltitude + 5f && num < maxAltitude - 5f)
		{
			doWarning = false;
			armed = true;
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		ConfigNode configNode = vDataNode.AddOrGetNode("AltitudeWarningSettings");
		configNode.SetValue("minAltitude", minAltitude);
		configNode.SetValue("maxAltitude", maxAltitude);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode("AltitudeWarningSettings");
		if (node != null)
		{
			ConfigNodeUtils.TryParseValue(node, "minAltitude", ref minAltitude);
			ConfigNodeUtils.TryParseValue(node, "maxAltitude", ref maxAltitude);
		}
	}
}
