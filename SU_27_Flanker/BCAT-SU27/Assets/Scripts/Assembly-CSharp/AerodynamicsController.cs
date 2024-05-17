using UnityEngine;

public class AerodynamicsController : MonoBehaviour
{
	public Aerodynamics wingAero;

	[Tooltip("This is actually pressure in Atmospheres")]
	public AnimationCurve atmosDensityCurve;

	public SOCurve defaultDragMachCurve;

	public SOCurve wingDragMachCurveZero;

	public float sweepDragModifierX;

	public float sweepDragModifierY;

	public static AerodynamicsController fetch { get; private set; }

	public float AtmosPressureAtPosition(Vector3 worldPos)
	{
		return atmosDensityCurve.Evaluate(WaterPhysics.GetAltitude(worldPos));
	}

	public float AtmosDensityAtPosition(Vector3 worldPos)
	{
		return 0.021f * AtmosPressureAtPosition(worldPos);
	}

	public float AtmosDensityAtPositionMetric(Vector3 worldPos)
	{
		return 1.225f * AtmosPressureAtPosition(worldPos);
	}

	public float DragMultiplierAtSpeed(float speed, float altitude)
	{
		float t = MeasurementManager.SpeedToMach(speed, altitude);
		return defaultDragMachCurve.Evaluate(t);
	}

	public float DragMultiplierAtSpeed(float speed, Vector3 position)
	{
		return DragMultiplierAtSpeed(speed, WaterPhysics.GetAltitude(position));
	}

	public float IndicatedAirspeed(float trueAirspeed, Vector3 position)
	{
		float altitude = WaterPhysics.GetAltitude(position);
		float num = AtmosPressureAtPosition(position);
		float num2 = num * 1.225f;
		num *= 101325f;
		float num3 = num2 * trueAirspeed * trueAirspeed / 2f + num;
		float num4 = Mathf.Sqrt(2f * (num3 - num) / 1.225f);
		float num5 = 1f + 0.06792f * (altitude / 3048f);
		return num4 * num5;
	}

	public float DragMultiplierAtSpeedAndSweep(float speed, float altitude, float sweep)
	{
		float num = Mathf.Lerp(1f, sweepDragModifierX, sweep / 90f);
		float num2 = Mathf.Lerp(1f, sweepDragModifierY, sweep / 90f);
		float num3 = MeasurementManager.SpeedToMach(speed, altitude);
		return 1f + num2 * (wingDragMachCurveZero.Evaluate(num3 * num) - 1f);
	}

	private void Awake()
	{
		if ((bool)fetch)
		{
			Object.Destroy(fetch);
		}
		fetch = this;
	}
}
