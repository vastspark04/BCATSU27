using UnityEngine;
using UnityEngine.UI;

public class VRGameSettingsFloat : VRGameSettingsUI
{
	public float adjustRate = 0.5f;

	public Transform indicatorTf;

	public Text valueText;

	private float currT;

	public void IncrementT()
	{
		currT = Mathf.Clamp01(currT + adjustRate * Time.deltaTime);
		UpdateFloatValue(Mathf.Lerp(setting.minValue, setting.maxValue, currT));
	}

	public void DecrementT()
	{
		currT = Mathf.Clamp01(currT - adjustRate * Time.deltaTime);
		UpdateFloatValue(Mathf.Lerp(setting.minValue, setting.maxValue, currT));
	}

	public override void RevertSetting()
	{
		currT = Mathf.InverseLerp(setting.minValue, setting.maxValue, setting.GetFloatValue());
		base.RevertSetting();
	}

	protected override void UpdateValueIndicator()
	{
		base.UpdateValueIndicator();
		indicatorTf.localScale = new Vector3(currT, 1f, 1f);
		valueText.text = base.currValue.ToString("0.0");
	}
}
