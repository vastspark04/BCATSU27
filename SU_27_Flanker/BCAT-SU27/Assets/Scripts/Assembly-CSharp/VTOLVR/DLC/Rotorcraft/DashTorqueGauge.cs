namespace VTOLVR.DLC.Rotorcraft{

public class DashTorqueGauge : DashGauge
{
	public TurbineTransmission transmission;

	public float maxTorque;

	protected override float GetMeteredValue()
	{
		return transmission.inputTorque / maxTorque;
	}
}

}