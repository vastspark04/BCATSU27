public class TutObjTakeoffLand : CustomTutorialObjective
{
	public FlightInfo flightInfo;

	public bool landing;

	public override bool GetIsCompleted()
	{
		return flightInfo.isLanded == landing;
	}
}
