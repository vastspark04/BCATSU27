public class DashRPMGauge : DashGauge
{
	public ModuleEngine engine;

	protected override float GetMeteredValue()
	{
		if (engine.useTorquePhysics)
		{
			return engine.outputRPM / engine.maxRPM;
		}
		return engine.displayedRPM;
	}
}
