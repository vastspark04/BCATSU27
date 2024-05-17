using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FlightDataUI : MonoBehaviour
{
	private struct STRTDataPoint
	{
		public float kias;

		public float turnRate;

		public float alpha;

		public float g;
	}

	public FlightInfo flightInfo;

	public MeasurementManager measurements;

	public Text turnRateText;

	public Text aoaText;

	public Text ktasText;

	public Text kiasText;

	public Text gText;

	public Text altText;

	public Text massText;

	private float turnRate;

	private Vector3 lastVel;

	private List<STRTDataPoint> turnRateData = new List<STRTDataPoint>();

	private Coroutine trtRoutine;

	private bool plotTrt;

	private void Update()
	{
		if ((bool)turnRateText)
		{
			turnRateText.text = ToNearestTenthsString(turnRate);
		}
		if ((bool)aoaText)
		{
			aoaText.text = ToNearestTenthsString(flightInfo.aoa);
		}
		if ((bool)ktasText)
		{
			ktasText.text = ToNearestTenthsString(MeasurementManager.SpeedToKnot(flightInfo.airspeed));
		}
		if ((bool)kiasText)
		{
			kiasText.text = ToNearestTenthsString(MeasurementManager.SpeedToKnot(IndicatedAirspeed(flightInfo.airspeed, flightInfo.rb.position)));
		}
		if ((bool)gText)
		{
			gText.text = ToNearestTenthsString(flightInfo.playerGs);
		}
		if ((bool)altText)
		{
			altText.text = ToNearestTenthsString(measurements.ConvertedAltitude(flightInfo.altitudeASL));
		}
		if ((bool)massText)
		{
			massText.text = ToNearestTenthsString(flightInfo.rb.mass * 1000f);
		}
	}

	private string ToNearestTenthsString(float num)
	{
		return (Mathf.Round(num * 10f) / 10f).ToString("0.0");
	}

	private void FixedUpdate()
	{
		turnRate = Vector3.Angle(flightInfo.rb.velocity, lastVel) / Time.fixedDeltaTime;
		lastVel = flightInfo.rb.velocity;
	}

	private static float IndicatedAirspeed(float trueAirspeed, Vector3 position)
	{
		return AerodynamicsController.fetch.IndicatedAirspeed(trueAirspeed, position);
	}

	public void BeginRecordTurnRate()
	{
		if (trtRoutine != null)
		{
			StopCoroutine(trtRoutine);
		}
		turnRateData.Clear();
		trtRoutine = StartCoroutine(TurnRateRecordRoutine());
	}

	private IEnumerator TurnRateRecordRoutine()
	{
		float SPEED_THRESHOLD = 5f;
		DataGraph maxTrtGraph = DataGraph.CreateGraph("Max Turn Rate", Vector3.zero);
		float lastAirspeed = MeasurementManager.SpeedToKnot(IndicatedAirspeed(flightInfo.airspeed, flightInfo.rb.position));
		while (base.enabled)
		{
			yield return new WaitForFixedUpdate();
			float num = MeasurementManager.SpeedToKnot(IndicatedAirspeed(flightInfo.airspeed, flightInfo.rb.position));
			_ = Mathf.Abs(num - lastAirspeed) / Time.fixedDeltaTime;
			lastAirspeed = num;
			if (!plotTrt)
			{
				continue;
			}
			bool flag = true;
			for (int i = 0; i < turnRateData.Count && flag; i++)
			{
				if (Mathf.Abs(turnRateData[i].kias - num) < SPEED_THRESHOLD)
				{
					flag = false;
				}
			}
			if (flag)
			{
				STRTDataPoint sTRTDataPoint = default(STRTDataPoint);
				sTRTDataPoint.kias = num;
				sTRTDataPoint.turnRate = turnRate;
				sTRTDataPoint.alpha = flightInfo.aoa;
				sTRTDataPoint.g = flightInfo.playerGs;
				STRTDataPoint item = sTRTDataPoint;
				turnRateData.Add(item);
				maxTrtGraph.AddValue(new Vector2(item.kias, item.turnRate));
			}
			plotTrt = false;
		}
	}

	public void PlotTRT()
	{
		plotTrt = true;
	}

	public void StopRecordingTrtData()
	{
		if (trtRoutine != null)
		{
			StopCoroutine(trtRoutine);
		}
	}
}
