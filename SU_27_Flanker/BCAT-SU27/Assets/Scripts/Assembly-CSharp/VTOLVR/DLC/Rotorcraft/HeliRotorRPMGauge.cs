namespace VTOLVR.DLC.Rotorcraft{

public class HeliRotorRPMGauge : DashGauge
{
	public HelicopterRotor rotor;

	protected override float GetMeteredValue()
	{
		return rotor.inputShaft.outputRPM;
	}
}

}