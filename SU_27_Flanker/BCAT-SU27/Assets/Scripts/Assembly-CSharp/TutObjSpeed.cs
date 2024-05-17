public class TutObjSpeed : CustomTutorialObjective
{
	public enum Comparisons
	{
		GreaterThan,
		LessThan
	}

	public FlightInfo flightInfo;

	public float targetSpeed;

	public Comparisons comparison;

	public override bool GetIsCompleted()
	{
		return comparison switch
		{
			Comparisons.GreaterThan => flightInfo.airspeed > targetSpeed, 
			Comparisons.LessThan => flightInfo.airspeed < targetSpeed, 
			_ => false, 
		};
	}
}
