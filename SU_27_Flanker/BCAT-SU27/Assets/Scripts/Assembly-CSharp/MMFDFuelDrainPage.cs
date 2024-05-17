using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MMFDFuelDrainPage : MonoBehaviour
{
	public MFDPage mfdPage;

	public MeasurementManager measurements;

	public FuelTank fuelTank;

	public ModuleEngine[] engines;

	public Rigidbody rb;

	public FlightInfo flightInfo;

	public Text fuelDrainText;

	public Text estTimeText;

	public Text estDistanceText;

	public Text twrText;

	public bool showLandingTWR = true;

	private Coroutine updateRoutine;

	private const int AVG_SAMPLES = 5;

	private float[] fuelDrains;

	private float[] speeds;

	private int avgIdx;

	private int sampleIdx;

	private int sampleInterval = 5;

	public float drainPerMinute { get; private set; }

	public float estTimeSeconds { get; private set; }

	public float estDistMeters { get; private set; }

	private void Awake()
	{
		fuelDrains = new float[5];
		speeds = new float[5];
		for (int i = 0; i < 5; i++)
		{
			fuelDrains[i] = (speeds[i] = -1f);
		}
		if ((bool)mfdPage)
		{
			mfdPage.OnActivatePage.AddListener(OnActivate);
			mfdPage.OnDeactivatePage.AddListener(OnDeactivate);
		}
	}

	private void OnEnable()
	{
		if (!mfdPage)
		{
			OnActivate();
		}
	}

	private void OnActivate()
	{
		updateRoutine = StartCoroutine(UpdateRoutine());
	}

	private void OnDeactivate()
	{
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
	}

	private IEnumerator UpdateRoutine()
	{
		while (fuelDrains == null)
		{
			yield return null;
		}
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (true)
		{
			UpdateInfo();
			yield return wait;
		}
	}

	private void UpdateInfo()
	{
		if (sampleIdx == 0)
		{
			fuelDrains[avgIdx] = fuelTank.fuelDrain;
			speeds[avgIdx] = flightInfo.surfaceSpeed;
			avgIdx = (avgIdx + 1) % 5;
		}
		sampleIdx = (sampleIdx + 1) % sampleInterval;
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < 5; i++)
		{
			if (fuelDrains[i] > 0f)
			{
				num += fuelDrains[i];
				num3++;
			}
			if (speeds[i] > 0f)
			{
				num2 += speeds[i];
				num4++;
			}
		}
		num /= (float)num3;
		num2 /= (float)num4;
		if (num > 0f)
		{
			drainPerMinute = fuelTank.fuelDrain * 60f;
			string text = string.Format("{0} L/min", drainPerMinute.ToString("0.00"));
			fuelDrainText.text = text;
			float num6 = (estTimeSeconds = fuelTank.totalFuel / num);
			TimeSpan timeSpan = new TimeSpan(0, 0, 0, Mathf.RoundToInt(num6));
			string text2 = string.Format("{0}:{1}:{2}", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00"));
			estTimeText.text = text2;
			if (num2 > 0f)
			{
				float distance = (estDistMeters = num6 * num2);
				string text3 = measurements.FormattedDistance(distance);
				estDistanceText.text = text3;
			}
			else
			{
				estDistanceText.text = $"-.- {measurements.SpeedLabel()}";
			}
		}
		else
		{
			fuelDrainText.text = "-.-- L/min";
			estTimeText.text = "--:--:--";
			estDistanceText.text = $"-.- {measurements.SpeedLabel()}";
		}
		float num8 = 0f;
		float num9 = 0f;
		for (int j = 0; j < engines.Length; j++)
		{
			if ((bool)engines[j])
			{
				num8 += engines[j].GetAffectedMaxThrust();
				num9 += engines[j].GetLandingThrust();
			}
		}
		if (num8 > 0f)
		{
			float num10 = num8 / (9.81f * rb.mass);
			if (showLandingTWR)
			{
				float num11 = num9 / (9.81f * rb.mass);
				twrText.text = string.Format("{0} / {1}", num11.ToString("0.00"), num10.ToString("0.00"));
			}
			else
			{
				twrText.text = num10.ToString("0.00");
			}
		}
		else
		{
			twrText.text = "-.--";
		}
	}
}
