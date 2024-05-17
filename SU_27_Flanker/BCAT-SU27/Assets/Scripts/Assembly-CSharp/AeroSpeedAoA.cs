using UnityEngine;

public class AeroSpeedAoA : MonoBehaviour
{
	public FlightInfo flightInfo;

	public AeroController aeroController;

	public int[] indices;

	public AnimationCurve aoaSpeedCurve;

	private void Update()
	{
		float aoAFactor = aoaSpeedCurve.Evaluate(flightInfo.airspeed);
		for (int i = 0; i < indices.Length; i++)
		{
			int num = indices[i];
			aeroController.controlSurfaces[num].AoAFactor = aoAFactor;
		}
	}
}
