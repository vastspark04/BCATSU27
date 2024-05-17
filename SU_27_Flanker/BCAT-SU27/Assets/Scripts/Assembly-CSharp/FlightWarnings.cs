using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FlightWarnings : MonoBehaviour
{
	[Serializable]
	public class FlightWarning
	{
		public string warningName;

		public AudioClip audioClip;

		public FlightWarning(string warningName, AudioClip clip)
		{
			this.warningName = warningName;
			audioClip = clip;
		}
	}

	public enum CommonWarnings
	{
		EngineFailure,
		LeftEngineFailure,
		RightEngineFailure,
		APUFailure,
		HydraulicsFailure,
		Chaff,
		ChaffLow,
		ChaffEmpty,
		Flare,
		FlareLow,
		FlareEmpty,
		BingoFuel,
		Altitude,
		PullUp,
		OverG,
		MissileLaunch,
		Missile,
		Shoot,
		Pitbull,
		Warning,
		Fire,
		FuelLeak,
		FuelDump,
		LandingGear,
		AutopilotOff,
		WingFold
	}

	[Serializable]
	public class CommonWarningsClips
	{
		public AudioClip EngineFailure;

		public AudioClip LeftEngineFailure;

		public AudioClip RightEngineFailure;

		public AudioClip APUFailure;

		public AudioClip HydraulicsFailure;

		public AudioClip Chaff;

		public AudioClip ChaffLow;

		public AudioClip ChaffEmpty;

		public AudioClip Flare;

		public AudioClip FlareLow;

		public AudioClip FlareEmpty;

		public AudioClip BingoFuel;

		public AudioClip Altitude;

		public AudioClip PullUp;

		public AudioClip OverG;

		public AudioClip MissileLaunch;

		public AudioClip Missile;

		public AudioClip Shoot;

		public AudioClip Pitbull;

		public AudioClip Warning;

		public AudioClip Fire;

		public AudioClip FuelLeak;

		public AudioClip FuelDump;

		public AudioClip LandingGear;

		public AudioClip AutopilotOff;

		public AudioClip WingFold;

		private FlightWarning[] cwp;

		private bool[] cWarned;

		public FlightWarning[] ToArray()
		{
			if (cwp == null)
			{
				cwp = new FlightWarning[26]
				{
					new FlightWarning("EngineFailure", EngineFailure),
					new FlightWarning("LeftEngineFailure", LeftEngineFailure),
					new FlightWarning("RightEngineFailure", RightEngineFailure),
					new FlightWarning("APUFailure", APUFailure),
					new FlightWarning("HydraulicsFailure", HydraulicsFailure),
					new FlightWarning("Chaff", Chaff),
					new FlightWarning("ChaffLow", ChaffLow),
					new FlightWarning("ChaffEmpty", ChaffEmpty),
					new FlightWarning("Flare", Flare),
					new FlightWarning("FlareLow", FlareLow),
					new FlightWarning("FlareEmpty", FlareEmpty),
					new FlightWarning("BingoFuel", BingoFuel),
					new FlightWarning("Altitude", Altitude),
					new FlightWarning("PullUp", PullUp),
					new FlightWarning("OverG", OverG),
					new FlightWarning("MissileLaunch", MissileLaunch),
					new FlightWarning("Missile", Missile),
					new FlightWarning("Shoot", Shoot),
					new FlightWarning("Pitbull", Pitbull),
					new FlightWarning("Warning", Warning),
					new FlightWarning("Fire", Fire),
					new FlightWarning("FuelLeak", FuelLeak),
					new FlightWarning("FuelDump", FuelDump),
					new FlightWarning("LandingGear", LandingGear),
					new FlightWarning("AutopilotOff", AutopilotOff),
					new FlightWarning("WingFold", WingFold)
				};
			}
			return cwp;
		}

		public CommonWarningsClips()
		{
			cWarned = new bool[Enum.GetValues(typeof(CommonWarnings)).Length];
		}

		public bool CheckWasContinuousWarned(CommonWarnings cw)
		{
			return cWarned[(int)cw];
		}

		public void SetContinuousWarned(CommonWarnings cw, bool warned)
		{
			cWarned[(int)cw] = warned;
		}

		public void ClearContinuousWarns()
		{
			for (int i = 0; i < cWarned.Length; i++)
			{
				cWarned[i] = false;
			}
		}
	}

	public Battery battery;

	public AudioSource audioSource;

	public float bingoFuelLevel;

	public FuelTank fuelTank;

	private List<FlightWarning> continuousWarnings = new List<FlightWarning>();

	private Queue<FlightWarning> oneShotWarnings = new Queue<FlightWarning>();

	public float warnInterval = 3f;

	public GameObject flashObject;

	public UIImageStatusLight warningStatusLight;

	private float lastFlashTime;

	private bool clearedWarnings;

	public UnityEvent OnClearedWarnings;

	private bool noBattCleared;

	public CommonWarningsClips commonWarningsClips;

	private FlightWarning[] cwp;

	public bool isBingoFuel { get; private set; }

	private void Awake()
	{
		if (commonWarningsClips != null)
		{
			cwp = commonWarningsClips.ToArray();
		}
		VehicleMaster component = GetComponent<VehicleMaster>();
		if ((bool)component)
		{
			component.OnSetNormBingoFuel += Vm_OnSetNormBingoFuel;
			Vm_OnSetNormBingoFuel(component.normBingoLevel);
		}
	}

	private void Vm_OnSetNormBingoFuel(float normBingoLevel)
	{
		bingoFuelLevel = normBingoLevel * fuelTank.maxFuel;
	}

	private void OnEnable()
	{
		StartCoroutine(WarningRoutine());
	}

	private void Update()
	{
		CheckFuel();
		bool flag = !battery || battery.Drain(0.001f);
		if (flag)
		{
			noBattCleared = false;
		}
		else if (!noBattCleared)
		{
			noBattCleared = true;
			ClearWarnings();
		}
		if (flag && (continuousWarnings.Count > 0 || oneShotWarnings.Count > 0))
		{
			audioSource.volume = 1f;
			if (Time.time - lastFlashTime > 0.25f)
			{
				if ((bool)flashObject)
				{
					flashObject.SetActive(!flashObject.activeSelf);
				}
				if ((bool)warningStatusLight)
				{
					warningStatusLight.Toggle();
				}
				lastFlashTime = Time.time;
			}
		}
		else
		{
			if (!flag)
			{
				audioSource.volume = 0f;
			}
			if ((bool)flashObject)
			{
				flashObject.SetActive(value: false);
			}
			if ((bool)warningStatusLight)
			{
				warningStatusLight.SetColor(Color.black);
			}
		}
	}

	public void ClearWarnings()
	{
		continuousWarnings = new List<FlightWarning>();
		oneShotWarnings = new Queue<FlightWarning>();
		commonWarningsClips.ClearContinuousWarns();
		audioSource.Stop();
		clearedWarnings = true;
		if (OnClearedWarnings != null)
		{
			OnClearedWarnings.Invoke();
		}
	}

	private void CheckFuel()
	{
		if (fuelTank.fuel > bingoFuelLevel)
		{
			if (isBingoFuel)
			{
				RemoveCommonWarning(CommonWarnings.BingoFuel);
			}
			isBingoFuel = false;
		}
		else if (!isBingoFuel)
		{
			AddCommonWarning(CommonWarnings.BingoFuel);
			isBingoFuel = true;
		}
	}

	private IEnumerator WarningRoutine()
	{
		Queue<FlightWarning> continuousQueue = new Queue<FlightWarning>();
		while (base.enabled)
		{
			yield return new WaitForSeconds(warnInterval);
			while (oneShotWarnings.Count == 0 && continuousWarnings.Count == 0)
			{
				yield return null;
			}
			while (oneShotWarnings.Count > 0 && !clearedWarnings)
			{
				AudioClip audioClip = oneShotWarnings.Dequeue().audioClip;
				if ((bool)audioClip)
				{
					if (!battery || battery.Drain(0.001f))
					{
						audioSource.volume = 1f;
						audioSource.PlayOneShot(audioClip);
					}
					yield return new WaitForSeconds(audioClip.length + 0.5f);
				}
				else
				{
					yield return new WaitForSeconds(1f);
				}
			}
			foreach (FlightWarning continuousWarning in continuousWarnings)
			{
				if (clearedWarnings)
				{
					break;
				}
				continuousQueue.Enqueue(continuousWarning);
			}
			while (continuousQueue.Count > 0 && !clearedWarnings)
			{
				AudioClip audioClip2 = continuousQueue.Dequeue().audioClip;
				if ((bool)audioClip2)
				{
					if ((!battery || battery.Drain(0.001f)) && (bool)audioSource)
					{
						audioSource.volume = 1f;
						audioSource.PlayOneShot(audioClip2);
					}
					yield return new WaitForSeconds(audioClip2.length + 0.5f);
				}
				else
				{
					yield return new WaitForSeconds(1f);
				}
			}
			if (clearedWarnings)
			{
				continuousQueue = new Queue<FlightWarning>();
				clearedWarnings = false;
			}
		}
	}

	public void AddOneShotWarning(FlightWarning w)
	{
		oneShotWarnings.Enqueue(w);
	}

	public void AddContinuousWarning(FlightWarning w)
	{
		continuousWarnings.Add(w);
	}

	public void RemoveContinuousWarning(FlightWarning w)
	{
		continuousWarnings.Remove(w);
	}

	public void AddCommonWarning(CommonWarnings commonWarning)
	{
		if (cwp != null)
		{
			if (cwp[(int)commonWarning] != null)
			{
				AddOneShotWarning(cwp[(int)commonWarning]);
			}
		}
	}

	public bool IsCommonWarningThrown(CommonWarnings w)
	{
		return commonWarningsClips.CheckWasContinuousWarned(w);
	}

	[EnumAction(typeof(CommonWarnings))]
	public void AddCommonWarning(int commonWarning)
	{
		AddCommonWarning((CommonWarnings)commonWarning);
	}

	public void AddCommonWarningContinuous(CommonWarnings commonWarning)
	{
		if (cwp != null)
		{
			if (cwp[(int)commonWarning] != null && !commonWarningsClips.CheckWasContinuousWarned(commonWarning))
			{
				commonWarningsClips.SetContinuousWarned(commonWarning, warned: true);
				AddContinuousWarning(cwp[(int)commonWarning]);
			}
		}
	}

	[EnumAction(typeof(CommonWarnings))]
	public void AddCommonWarningContinuous(int commonWarning)
	{
		AddCommonWarningContinuous((CommonWarnings)commonWarning);
	}

	public void RemoveCommonWarning(CommonWarnings commonWarning)
	{
		if (cwp != null)
		{
			if (cwp[(int)commonWarning] != null && commonWarningsClips.CheckWasContinuousWarned(commonWarning))
			{
				commonWarningsClips.SetContinuousWarned(commonWarning, warned: false);
				RemoveContinuousWarning(cwp[(int)commonWarning]);
			}
		}
	}

	public bool CheckIsContinuouslyWarning(CommonWarnings cw)
	{
		if (commonWarningsClips == null)
		{
			return false;
		}
		return commonWarningsClips.CheckWasContinuousWarned(cw);
	}

	public AudioClip GetCommonWarningClip(CommonWarnings cw)
	{
		if (cwp == null)
		{
			return null;
		}
		if (cwp[(int)cw] == null)
		{
			return null;
		}
		return cwp[(int)cw].audioClip;
	}
}
