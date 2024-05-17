public class DashFuelFlowGauge : DashGauge
{
	public FuelTank fuelTank;

	protected override float GetMeteredValue()
	{
		return fuelTank.fuelDrain;
	}
}
