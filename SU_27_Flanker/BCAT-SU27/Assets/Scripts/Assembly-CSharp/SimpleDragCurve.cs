using UnityEngine;

public class SimpleDragCurve : SimpleDrag
{
	public AnimationCurve dragMultCurve;

	protected override float CalculateDragForceMagnitude(float spd)
	{
		return dragMultCurve.Evaluate(spd) * 0.5f * AerodynamicsController.fetch.AtmosDensityAtPosition(base.transform.position) * area * spd;
	}
}
