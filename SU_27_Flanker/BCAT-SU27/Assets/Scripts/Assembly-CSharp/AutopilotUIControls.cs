using UnityEngine;
using UnityEngine.UI;

public class AutopilotUIControls : MonoBehaviour
{
	public VTOLAutoPilot autoPilot;

	[Header("Heading")]
	public GameObject headingDisabledObj;

	public Text headingText;

	public VRInteractable headingRightInteractable;

	public VRInteractable headingLeftInteractable;

	public float headingAdjustMinRate;

	public float headingAdjustMaxRate;

	public float headingAdjustAccel;

	private float headingSpd;

	private bool adjHeading;

	private bool headingEnabled;

	[Header("Altitude")]
	public GameObject altDisabledObj;

	public Text altText;

	public VRInteractable altUpInteractable;

	public VRInteractable altDownInteractable;

	public float altAdjustMinRate;

	public float altAdjustMaxRate;

	public float altAdjustAccel;

	public float maxAltitudeHold = 16000f;

	public float minAltitudeHold = 15f;

	private float altSpd;

	private bool adjAlt;

	private bool altEnabled;

	private MeasurementManager measurements;

	private bool navModeEnabled;

	private int navModeHeading;

	private int navModeAlt;

	private void Awake()
	{
		measurements = GetComponentInParent<MeasurementManager>();
		headingRightInteractable.OnStopInteract.AddListener(HeadingAdjStop);
		headingRightInteractable.OnInteracting.AddListener(HeadingRightHold);
		headingLeftInteractable.OnStopInteract.AddListener(HeadingAdjStop);
		headingLeftInteractable.OnInteracting.AddListener(HeadingLeftHold);
		headingText.text = "--";
		headingDisabledObj.SetActive(value: true);
		altUpInteractable.OnStopInteract.AddListener(AltStopAdjust);
		altUpInteractable.OnInteracting.AddListener(AltUpHold);
		altDownInteractable.OnStopInteract.AddListener(AltStopAdjust);
		altDownInteractable.OnInteracting.AddListener(AltDownHold);
		altText.text = "--";
		altDisabledObj.SetActive(value: true);
		headingRightInteractable.enabled = false;
		headingLeftInteractable.enabled = false;
		altUpInteractable.enabled = false;
		altDownInteractable.enabled = false;
	}

	private void Update()
	{
		if (autoPilot.headingHold != headingEnabled)
		{
			headingEnabled = autoPilot.headingHold;
			headingDisabledObj.SetActive(!headingEnabled);
			headingRightInteractable.enabled = headingEnabled;
			headingLeftInteractable.enabled = headingEnabled;
			if (headingEnabled)
			{
				headingText.text = Mathf.RoundToInt(autoPilot.headingToHold).ToString("000");
			}
			else
			{
				headingText.text = "--";
			}
		}
		if (autoPilot.altitudeHold != altEnabled)
		{
			altEnabled = autoPilot.altitudeHold;
			altDisabledObj.SetActive(!altEnabled);
			altUpInteractable.enabled = altEnabled;
			altDownInteractable.enabled = altEnabled;
			if (altEnabled)
			{
				altText.text = Mathf.RoundToInt(measurements.ConvertedAltitude(autoPilot.altitudeToHold)).ToString();
			}
			else
			{
				altText.text = "--";
			}
		}
		if (navModeEnabled != autoPilot.navMode)
		{
			navModeEnabled = autoPilot.navMode;
			if (!headingEnabled)
			{
				headingText.text = "--";
				navModeHeading = -1;
			}
			if (!altEnabled)
			{
				altText.text = "--";
				navModeAlt = -1;
			}
		}
		if (!autoPilot.navMode)
		{
			return;
		}
		int num = Mathf.RoundToInt(autoPilot.headingToHold);
		if (num != navModeHeading)
		{
			navModeHeading = num;
			headingText.text = num.ToString("000");
		}
		if (!altEnabled)
		{
			int num2 = Mathf.RoundToInt(autoPilot.altitudeToHold);
			if (num2 != navModeAlt)
			{
				navModeAlt = num2;
				altText.text = Mathf.RoundToInt(measurements.ConvertedAltitude(num2)).ToString();
			}
		}
	}

	private void AltUpHold()
	{
		AdjustAlt(1);
	}

	private void AltDownHold()
	{
		AdjustAlt(-1);
	}

	private void AdjustAlt(int dir)
	{
		float num = measurements.ConvertedAltitude(autoPilot.altitudeToHold);
		if (adjAlt)
		{
			num += (float)dir * altSpd * Time.deltaTime;
			altSpd += altAdjustAccel * Time.deltaTime;
			altSpd = Mathf.Min(altSpd, altAdjustMaxRate);
		}
		else
		{
			num += (float)dir;
			altSpd = altAdjustMinRate;
			adjAlt = true;
		}
		num = Mathf.Clamp(num, minAltitudeHold, measurements.ConvertedAltitude(maxAltitudeHold));
		altText.text = Mathf.RoundToInt(num).ToString();
		autoPilot.altitudeToHold = num / measurements.ConvertedAltitude(1f);
	}

	private void AltStopAdjust()
	{
		adjAlt = false;
	}

	private void HeadingRightHold()
	{
		AdjustHeading(1);
	}

	private void HeadingLeftHold()
	{
		AdjustHeading(-1);
	}

	private void AdjustHeading(int dir)
	{
		if (adjHeading)
		{
			autoPilot.headingToHold += (float)dir * headingSpd * Time.deltaTime;
			headingSpd += headingAdjustAccel * Time.deltaTime;
			headingSpd = Mathf.Min(headingSpd, headingAdjustMaxRate);
		}
		else
		{
			autoPilot.headingToHold += dir;
			headingSpd = headingAdjustMinRate;
			adjHeading = true;
		}
		if (autoPilot.headingToHold > 360f)
		{
			autoPilot.headingToHold -= 360f;
		}
		else if (autoPilot.headingToHold < 0f)
		{
			autoPilot.headingToHold += 360f;
		}
		headingText.text = Mathf.RoundToInt(autoPilot.headingToHold).ToString();
	}

	private void HeadingAdjStop()
	{
		adjHeading = false;
	}
}
