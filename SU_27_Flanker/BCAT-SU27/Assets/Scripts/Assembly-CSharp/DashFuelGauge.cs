public class DashFuelGauge : DashGauge
{
	public FuelTank fuelTank;

	protected override float GetMeteredValue()
	{
		return fuelTank.fuelFraction;
	}
}
