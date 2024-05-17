public class DashAPUGauge : DashGauge
{
	public AuxilliaryPowerUnit apu;

	protected override float GetMeteredValue()
	{
		return apu.rpm;
	}
}
