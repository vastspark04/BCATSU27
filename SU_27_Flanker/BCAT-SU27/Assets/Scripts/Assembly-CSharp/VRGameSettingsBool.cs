using UnityEngine;
using UnityEngine.UI;

public class VRGameSettingsBool : VRGameSettingsUI
{
	public GameObject checkObject;

	public Text optionValue;

	public VRInteractable interactable;

	private bool bCurrVal;

	public void SetValueSwitch(int val)
	{
		bCurrVal = val > 0;
		UpdateBoolValue(bCurrVal);
	}

	public void ToggleValue()
	{
		bCurrVal = !bCurrVal;
		UpdateBoolValue(bCurrVal);
	}

	protected override void UpdateValueIndicator()
	{
		base.UpdateValueIndicator();
		checkObject.SetActive(bCurrVal);
		optionValue.text = (bCurrVal ? VTLStaticStrings.setting_enabled : VTLStaticStrings.setting_disabled);
		optionValue.color = (bCurrVal ? Color.green : Color.red);
	}

	public override void RevertSetting()
	{
		bCurrVal = setting.GetBoolValue();
		interactable.interactableName = setting.GetLocalizedName();
		base.RevertSetting();
	}
}
