using UnityEngine;

public class HUDEBracket : MonoBehaviour
{
	public FlightInfo fInfo;

	public GearAnimator gear;

	public GameObject displayObj;

	public Transform bracketTf;

	public RectTransform bracketRect;

	public MinMax aoaRange;

	public float lerpRate = 5f;

	private bool tEnabled;

	private float aoaToPos;

	private void Start()
	{
		aoaToPos = bracketRect.rect.height / (aoaRange.max - aoaRange.min);
		gear.OnSetFinalState += Gear_OnSetFinalState;
		Gear_OnSetFinalState(gear.state);
	}

	private void Gear_OnSetFinalState(GearAnimator.GearStates obj)
	{
		bool flag = (tEnabled = obj == GearAnimator.GearStates.Extended);
		if (!tEnabled)
		{
			displayObj.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (tEnabled)
		{
			if (!fInfo.isLanded)
			{
				float num = aoaRange.Lerp(0.5f);
				float b = (0f - (fInfo.aoa - num)) * aoaToPos;
				b = Mathf.Lerp(bracketTf.localPosition.y, b, lerpRate * Time.deltaTime);
				bracketTf.localPosition = new Vector3(0f, b, 0f);
				displayObj.SetActive(value: true);
			}
			else
			{
				displayObj.SetActive(value: false);
			}
		}
	}
}
