public class OverGWarning : GenericCommonFlightWarning
{
	public float maxG = 8f;

	private void Awake()
	{
		warning = FlightWarnings.CommonWarnings.OverG;
	}

	protected override void Update()
	{
		base.Update();
		doWarning = flightInfo.playerGs > maxG;
	}
}
