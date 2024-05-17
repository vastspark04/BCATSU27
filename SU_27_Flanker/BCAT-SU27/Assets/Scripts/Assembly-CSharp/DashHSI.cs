using UnityEngine;
using UnityEngine.UI;

public class DashHSI : ElectronicComponent
{
	public FlightInfo flightInfo;

	[Header("Compass")]
	public float compassSlerpRate = 7f;

	[Header("HSI")]
	public GameObject hsiBearingObject;

	public GameObject hsiOffsetObject;

	public Transform compassTf;

	public Transform referenceTransform;

	public Transform fromToTf;

	public Transform apHeadingBugTf;

	public Text crsHeadingText;

	public Text wpDistanceText;

	public Text distUnitText;

	public float maxPathOffsetDist = 1000f;

	public float pathOffsetPower = 0.5f;

	public float maxUIPathOffset = 37.5f;

	public float crsSensitivity = 1f;

	[Header("ILS")]
	public GameObject ilsObject;

	public Transform ilsSlopeTf;

	public float maxILSTransformOffset = 30f;

	public float maxILSDistOffset = 100f;

	public Runway ilsRunway;

	private float crsHeading;

	[Header("Component References")]
	public VTOLAutoPilot autopilot;

	public MeasurementManager measurements;

	private void Awake()
	{
		measurements.OnChangedDistanceMode += Measurements_OnChangedDistanceMode;
	}

	private void Measurements_OnChangedDistanceMode()
	{
		switch (measurements.distanceMode)
		{
		case MeasurementManager.DistanceModes.Meters:
			distUnitText.text = "KM";
			break;
		case MeasurementManager.DistanceModes.NautMiles:
			distUnitText.text = "NM";
			break;
		default:
			distUnitText.text = "MI";
			break;
		}
	}

	private void Start()
	{
		if ((bool)ilsObject)
		{
			ilsObject.SetActive(value: false);
		}
		Measurements_OnChangedDistanceMode();
	}

	private void Update()
	{
		if (DrainElectricity(0.1f * Time.deltaTime))
		{
			compassTf.localRotation = Quaternion.Slerp(compassTf.localRotation, Quaternion.Euler(0f, 0f, flightInfo.heading), compassSlerpRate * Time.deltaTime);
			crsHeadingText.gameObject.SetActive(value: true);
			distUnitText.gameObject.SetActive(value: true);
			if ((bool)WaypointManager.instance.currentWaypoint || (bool)ilsRunway)
			{
				hsiOffsetObject.SetActive(value: true);
				hsiBearingObject.SetActive(value: true);
				Vector3 position;
				if (!ilsRunway)
				{
					position = WaypointManager.instance.currentWaypoint.position;
					Transform previousGPSPoint = WaypointManager.instance.GetPreviousGPSPoint();
					if ((bool)previousGPSPoint)
					{
						crsHeading = VectorUtils.Bearing(previousGPSPoint.position, position);
					}
					ilsObject.SetActive(value: false);
				}
				else
				{
					position = ilsRunway.transform.position;
					crsHeading = VectorUtils.Bearing(ilsRunway.transform.position, ilsRunway.transform.position + ilsRunway.transform.forward);
					ilsObject.SetActive(value: true);
					Vector3 rhs = Vector3.Cross(Quaternion.AngleAxis(4f, ilsRunway.transform.right) * ilsRunway.transform.forward, ilsRunway.transform.right);
					float num = Vector3.Dot(referenceTransform.position - ilsRunway.transform.position, rhs);
					float num2 = Mathf.Clamp(Mathf.Sign(num) * Mathf.Pow(Mathf.Abs(num / maxILSDistOffset), 0.5f) * maxILSTransformOffset, 0f - maxILSTransformOffset, maxILSTransformOffset);
					ilsSlopeTf.localPosition = new Vector3(0f, 0f - num2, 0f);
				}
				Vector3 position2 = referenceTransform.position;
				crsHeadingText.text = Mathf.Round(crsHeading).ToString("000");
				hsiBearingObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f - crsHeading);
				Vector3 rhs2 = Quaternion.AngleAxis(crsHeading, Vector3.up) * Vector3.forward;
				Vector3 vector = Vector3.Cross(Vector3.up, rhs2);
				float num3 = Mathf.Pow(Vector3.Project(position2 - position, vector).magnitude / maxPathOffsetDist, pathOffsetPower) * Mathf.Sign(Vector3.Dot(position2 - position, vector));
				num3 = Mathf.Clamp(0f - num3, -1f, 1f) * maxUIPathOffset;
				Vector3 localPosition = hsiOffsetObject.transform.localPosition;
				localPosition.x = num3;
				hsiOffsetObject.transform.localPosition = localPosition;
				UpdateDistText(Vector3.Distance(position, position2));
				float num4 = Vector3.Dot(position - position2, referenceTransform.forward);
				fromToTf.localRotation = Quaternion.Euler(0f, 0f, (!(num4 > 0f)) ? 180 : 0);
			}
			else
			{
				hsiBearingObject.SetActive(value: false);
				ilsObject.SetActive(value: false);
				crsHeadingText.text = "---";
				wpDistanceText.text = "---";
			}
			if (autopilot.headingHold)
			{
				apHeadingBugTf.gameObject.SetActive(value: true);
				apHeadingBugTf.localRotation = Quaternion.Euler(0f, 0f, 0f - autopilot.headingToHold);
			}
			else
			{
				apHeadingBugTf.gameObject.SetActive(value: false);
			}
		}
		else
		{
			hsiBearingObject.SetActive(value: false);
			crsHeadingText.gameObject.SetActive(value: false);
			distUnitText.gameObject.SetActive(value: false);
			apHeadingBugTf.gameObject.SetActive(value: false);
		}
	}

	public void OnTwistCrsKnob(float delta)
	{
		crsHeading = Mathf.Repeat(crsHeading + delta * crsSensitivity, 360f);
	}

	public void OnTwistHdgKnob(float delta)
	{
		autopilot.headingToHold = Mathf.Repeat(autopilot.headingToHold + delta * crsSensitivity, 360f);
	}

	private void UpdateDistText(float dist)
	{
		float num = 0f;
		switch (measurements.distanceMode)
		{
		case MeasurementManager.DistanceModes.Meters:
			num = Mathf.Round(dist / 1000f);
			distUnitText.text = "KM";
			break;
		case MeasurementManager.DistanceModes.NautMiles:
			num = Mathf.Round(MeasurementManager.DistToNauticalMile(dist));
			distUnitText.text = "NM";
			break;
		default:
			num = Mathf.Round(MeasurementManager.DistToMiles(dist));
			distUnitText.text = "MI";
			break;
		}
		wpDistanceText.text = num.ToString("000");
	}
}
