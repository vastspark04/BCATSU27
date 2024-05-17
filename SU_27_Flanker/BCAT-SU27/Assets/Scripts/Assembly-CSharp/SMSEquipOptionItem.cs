using UnityEngine;
using UnityEngine.UI;

public class SMSEquipOptionItem : MonoBehaviour, ILocalizationUser
{
	public SMSEquipConfigView configView;

	public VRInteractable interactable;

	public Text nameText;

	public Text valueText;

	public HPEquippable.EquipFunction function;

	private int fIdx;

	private int eqIdx;

	private bool ripple;

	private string s_sms_optionToggle;

	private string s_sms_optionRipple;

	private string s_sms_rippleSingle;

	public void ApplyLocalization()
	{
		s_sms_optionToggle = VTLocalizationManager.GetString("s_sms_optionToggle", "Toggle", "Tooltip prefix for SMS equipment option toggles");
		s_sms_optionRipple = VTLocalizationManager.GetString("s_sms_optionRipple", "Ripple", "Ripple button label for SMS equipment option.");
		s_sms_rippleSingle = VTLocalizationManager.GetString("s_sms_rippleSingle", "Single", "Label for ripple setting set to single-fire (no ripple)");
	}

	private void Awake()
	{
		ApplyLocalization();
		interactable.OnInteract.AddListener(Click);
	}

	public void Setup(HPEquippable.EquipFunction f, int functionIdx, int equipIdx)
	{
		function = f;
		fIdx = functionIdx;
		eqIdx = equipIdx;
		nameText.text = f.optionName;
		valueText.text = f.optionReturnLabel;
		interactable.interactableName = $"{s_sms_optionToggle} {f.optionName}";
		ripple = false;
	}

	public void SetupRipple(int equipIdx)
	{
		eqIdx = equipIdx;
		ripple = true;
		nameText.text = s_sms_optionRipple.ToUpper();
		valueText.text = RippleLabel();
		interactable.interactableName = s_sms_optionRipple;
	}

	public void Click()
	{
		configView.sms.quarter.half.manager.PlayInputSound();
		if (ripple)
		{
			configView.sms.weaponManager.CycleRippleRates(eqIdx);
			valueText.text = RippleLabel();
		}
		else
		{
			configView.sms.weaponManager.WeaponFunctionButton(fIdx, eqIdx);
			valueText.text = function.optionReturnLabel;
		}
	}

	private string RippleLabel()
	{
		IRippleWeapon rippleWeapon = (IRippleWeapon)configView.sms.weaponManager.GetEquip(eqIdx);
		float num = rippleWeapon.GetRippleRates()[rippleWeapon.GetRippleRateIdx()];
		if (num <= 1f)
		{
			return s_sms_rippleSingle;
		}
		return num.ToString("0");
	}
}
