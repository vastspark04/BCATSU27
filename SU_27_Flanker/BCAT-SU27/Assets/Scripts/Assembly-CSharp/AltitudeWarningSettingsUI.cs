using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AltitudeWarningSettingsUI : MonoBehaviour, ILocalizationUser
{
	private MeasurementManager measurements;

	public AltitudeWarning altWarning;

	public float adjustRateStart;

	public float adjustRateMax;

	public float adjustRateIncrease;

	public float maxMaxAlt = 12000f;

	public Text minAltText;

	public Text maxAltText;

	public Text modeText;

	private Coroutine adjustRoutineMax;

	private Coroutine adjustRoutineMin;

	private string s_radar;

	private string s_asl;

	private void Awake()
	{
		measurements = GetComponentInParent<MeasurementManager>();
		ApplyLocalization();
	}

	private void Start()
	{
		UpdateUI();
	}

	public void ToggleMode()
	{
		altWarning.useRadarAlt = !altWarning.useRadarAlt;
		UpdateUI();
	}

	public void PressMaxAltUp()
	{
		StopAdjust(maxAlt: true);
		adjustRoutineMax = StartCoroutine(AdjustRoutine(maxAlt: true, 1));
	}

	public void PressMaxAltDown()
	{
		StopAdjust(maxAlt: true);
		adjustRoutineMax = StartCoroutine(AdjustRoutine(maxAlt: true, -1));
	}

	public void PressMinAltUp()
	{
		StopAdjust(maxAlt: false);
		adjustRoutineMin = StartCoroutine(AdjustRoutine(maxAlt: false, 1));
	}

	public void PressMinAltDown()
	{
		StopAdjust(maxAlt: false);
		adjustRoutineMin = StartCoroutine(AdjustRoutine(maxAlt: false, -1));
	}

	public void StopAdjust(bool maxAlt)
	{
		if (maxAlt && adjustRoutineMax != null)
		{
			StopCoroutine(adjustRoutineMax);
		}
		else if (!maxAlt && adjustRoutineMin != null)
		{
			StopCoroutine(adjustRoutineMin);
		}
	}

	private IEnumerator AdjustRoutine(bool maxAlt, int dir)
	{
		float adjustRate = adjustRateStart;
		while (true)
		{
			SetVal(maxAlt, GetCurr(maxAlt) + adjustRate * Time.deltaTime * (float)dir);
			adjustRate = Mathf.Min(adjustRate + adjustRateIncrease * Time.deltaTime, adjustRateMax);
			yield return null;
		}
	}

	private float GetCurr(bool maxAlt)
	{
		if (!maxAlt)
		{
			return altWarning.minAltitude;
		}
		return altWarning.maxAltitude;
	}

	private void SetVal(bool maxAlt, float val)
	{
		if (maxAlt)
		{
			altWarning.maxAltitude = Mathf.Clamp(val, altWarning.minAltitude + 10f, maxMaxAlt);
		}
		else
		{
			altWarning.minAltitude = Mathf.Clamp(val, 0f, altWarning.maxAltitude - 10f);
		}
		UpdateUI();
	}

	public void ApplyLocalization()
	{
		s_radar = VTLocalizationManager.GetString("altWarn_mode_radar", "RADAR", "Radar altimeter mode for altitude warning settings.");
		s_asl = VTLocalizationManager.GetString("altWarn_mode_asl", "ASL", "Sea level/barometric altimeter mode for altitude warning settings.");
	}

	private void UpdateUI()
	{
		modeText.text = (altWarning.useRadarAlt ? s_radar : s_asl);
		minAltText.text = Mathf.RoundToInt(measurements.ConvertedAltitude(altWarning.minAltitude)).ToString();
		maxAltText.text = Mathf.RoundToInt(measurements.ConvertedAltitude(altWarning.maxAltitude)).ToString();
	}
}
