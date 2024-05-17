using UnityEngine;

public class WingFoldController : MonoBehaviour, IQSVehicleComponent
{
	public FlightInfo flightInfo;

	public EmissiveTextureLight statusLight;

	public RotationToggle toggler;

	public Battery battery;

	public FlightWarnings warnings;

	public bool useCommonWarning = true;

	public FlightWarnings.FlightWarning speedWarning;

	public float maxSpeed;

	public GameObject[] disableOnFoldedOrDestroyed;

	public Health[] killWhenOverspeedFolded;

	public float killSpeed;

	public CatapultHook cHook;

	private int switchState;

	private bool warningIssued;

	private bool isBlinking;

	private bool wingsFolded;

	private bool killed;

	private bool CheckHasPower()
	{
		if ((bool)battery)
		{
			return battery.Drain(0.1f * Time.deltaTime);
		}
		return true;
	}

	public void SetState(int st)
	{
		switchState = st;
		if (IsWarnCondition())
		{
			if (CheckHasPower())
			{
				warningIssued = true;
				if (useCommonWarning)
				{
					warnings.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.WingFold);
				}
				else
				{
					warnings.AddContinuousWarning(speedWarning);
				}
			}
		}
		else if (st > 0)
		{
			toggler.SetDeployed();
			statusLight.SetColor(Color.red);
			disableOnFoldedOrDestroyed.SetActive(active: false);
			wingsFolded = true;
		}
		else
		{
			toggler.SetDefault();
			statusLight.SetColor(Color.black);
			disableOnFoldedOrDestroyed.SetActive(active: true);
			wingsFolded = false;
		}
	}

	private void SetStateImmediate(int st)
	{
		switchState = st;
		if (flightInfo.surfaceSpeed > maxSpeed)
		{
			warningIssued = true;
			if (useCommonWarning)
			{
				warnings.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.WingFold);
			}
			else
			{
				warnings.AddContinuousWarning(speedWarning);
			}
		}
		else if (st > 0)
		{
			toggler.SetDeployed();
			toggler.SetNormalizedRotationImmediate(1f);
			statusLight.SetColor(Color.red);
			disableOnFoldedOrDestroyed.SetActive(active: false);
			wingsFolded = true;
		}
		else
		{
			toggler.SetDefault();
			toggler.SetNormalizedRotationImmediate(0f);
			statusLight.SetColor(Color.black);
			disableOnFoldedOrDestroyed.SetActive(!killed);
			wingsFolded = false;
		}
	}

	private bool IsWarnCondition()
	{
		if (switchState == 1)
		{
			if (!(flightInfo.surfaceSpeed > maxSpeed))
			{
				return cHook.hooked;
			}
			return true;
		}
		return false;
	}

	private void Update()
	{
		if (!killed && wingsFolded && flightInfo.airspeed > killSpeed)
		{
			DestroyWingFolds();
		}
		if (warningIssued && !killed && !IsWarnCondition())
		{
			if (useCommonWarning)
			{
				warnings.RemoveCommonWarning(FlightWarnings.CommonWarnings.WingFold);
			}
			else
			{
				warnings.RemoveContinuousWarning(speedWarning);
			}
			warningIssued = false;
		}
		bool flag = false;
		if (killed || (flag = IsWarnCondition()))
		{
			if (Mathf.RoundToInt(Time.time * 4f) % 2 == 0)
			{
				statusLight.SetColor(Color.red);
			}
			else
			{
				statusLight.SetColor(Color.black);
			}
			isBlinking = true;
			if (flag && !warningIssued)
			{
				warningIssued = true;
				if (useCommonWarning)
				{
					warnings.AddCommonWarningContinuous(FlightWarnings.CommonWarnings.WingFold);
				}
				else
				{
					warnings.AddContinuousWarning(speedWarning);
				}
			}
		}
		else if (isBlinking)
		{
			isBlinking = false;
			if (switchState == 1)
			{
				statusLight.SetColor(Color.red);
			}
			else
			{
				statusLight.SetColor(Color.black);
			}
		}
	}

	private void DestroyWingFolds()
	{
		killed = true;
		for (int i = 0; i < killWhenOverspeedFolded.Length; i++)
		{
			VehiclePart component = killWhenOverspeedFolded[i].GetComponent<VehiclePart>();
			if ((bool)component)
			{
				component.detachDelay = new MinMax(0f, 0f);
			}
			killWhenOverspeedFolded[i].KillDelayed(Random.Range(0f, 1.5f));
		}
		for (int j = 0; j < disableOnFoldedOrDestroyed.Length; j++)
		{
			if ((bool)disableOnFoldedOrDestroyed[j])
			{
				disableOnFoldedOrDestroyed[j].SetActive(value: false);
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("WingFoldController");
		configNode.SetValue("killed", killed);
		configNode.SetValue("switchState", switchState);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("WingFoldController");
		if (node != null)
		{
			bool value = node.GetValue<bool>("killed");
			int value2 = node.GetValue<int>("switchState");
			if (value)
			{
				DestroyWingFolds();
			}
			else
			{
				SetStateImmediate(value2);
			}
		}
	}
}
