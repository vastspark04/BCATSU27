using UnityEngine;

public class AoAIndexer : MonoBehaviour
{
	public FlightInfo fInfo;

	public GearAnimator gear;

	public GameObject displayObj;

	public MinMax aoaRange;

	public MinMax aoaWideRange = new MinMax(6.9f, 9.3f);

	public GameObject onSpeedObj;

	public GameObject aoaHighObj;

	public GameObject aoaLowObj;

	private bool tEnabled;

	private float aoaToPos;

	private void Start()
	{
		gear.OnSetFinalState += Gear_OnSetFinalState;
		Gear_OnSetFinalState(gear.state);
	}

	private void Gear_OnSetFinalState(GearAnimator.GearStates obj)
	{
		bool active = obj == GearAnimator.GearStates.Extended;
		displayObj.SetActive(active);
		tEnabled = active;
		if (!tEnabled)
		{
			displayObj.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (!tEnabled)
		{
			return;
		}
		if (!fInfo.isLanded)
		{
			displayObj.SetActive(value: true);
			if (fInfo.aoa < aoaRange.min)
			{
				aoaLowObj.SetActive(value: true);
				aoaHighObj.SetActive(value: false);
				onSpeedObj.SetActive(fInfo.aoa > aoaWideRange.min);
			}
			else if (fInfo.aoa > aoaRange.max)
			{
				aoaLowObj.SetActive(value: false);
				aoaHighObj.SetActive(value: true);
				onSpeedObj.SetActive(fInfo.aoa < aoaWideRange.max);
			}
			else
			{
				aoaLowObj.SetActive(value: false);
				aoaHighObj.SetActive(value: false);
				onSpeedObj.SetActive(value: true);
			}
		}
		else
		{
			displayObj.SetActive(value: false);
		}
	}
}
