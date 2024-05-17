using UnityEngine;
using UnityEngine.UI;

public abstract class VRGameSettingsUI : MonoBehaviour
{
	public Text optionName;

	public Text optionDescription;

	protected GameSettings.Setting setting;

	public GameObject dirtyObject;

	protected float currValue { get; private set; }

	public void Setup(GameSettings.Setting setting)
	{
		this.setting = setting;
		optionName.text = setting.GetLocalizedName();
		optionDescription.text = setting.GetLocalizedDescription();
		RevertSetting();
	}

	public virtual void SaveSetting()
	{
		setting.SetFloatValue(currValue);
		UpdateValueIndicator();
	}

	public virtual void RevertSetting()
	{
		currValue = setting.GetFloatValue();
		UpdateValueIndicator();
	}

	protected virtual void UpdateValueIndicator()
	{
		dirtyObject.SetActive(Mathf.Abs(currValue - setting.GetFloatValue()) > 0.001f);
	}

	protected void UpdateBoolValue(bool v)
	{
		currValue = (v ? 1 : (-1));
		UpdateValueIndicator();
	}

	protected void UpdateFloatValue(float v)
	{
		currValue = v;
		UpdateValueIndicator();
	}
}
