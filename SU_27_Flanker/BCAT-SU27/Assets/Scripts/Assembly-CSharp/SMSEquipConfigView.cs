using UnityEngine;
using UnityEngine.UI;

public class SMSEquipConfigView : MonoBehaviour, ILocalizationUser
{
	public SMSEquipOptionItem[] optionItems;

	public MFDPStoresManagement sms;

	public Text eqNameText;

	public Text armedText;

	public Text jettisonText;

	private HPEquippable equip;

	private string s_sms_armed;

	private string s_sms_disarmed;

	private string s_sms_jettison;

	public void ApplyLocalization()
	{
		s_sms_armed = VTLocalizationManager.GetString("s_sms_armed", "[ ARMED ]", "Armed label for an equip in the SMS config view.");
		s_sms_disarmed = VTLocalizationManager.GetString("s_sms_disarmed", "DISARMED", "Disarmed label for an equip in the SMS config view.");
		s_sms_jettison = VTLocalizationManager.GetString("s_sms_jettison", "JETTISON", "Jettison label for an equip in the SMS config view.");
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	public void Display(HPEquippable eq)
	{
		equip = eq;
		eqNameText.text = eq.GetLocalizedFullName();
		int i = 0;
		if (eq.equipFunctions != null)
		{
			for (; i < eq.equipFunctions.Length; i++)
			{
				optionItems[i].gameObject.SetActive(value: true);
				optionItems[i].Setup(eq.equipFunctions[i], i, eq.hardpointIdx);
			}
		}
		if (eq is IRippleWeapon)
		{
			optionItems[i].gameObject.SetActive(value: true);
			optionItems[i].SetupRipple(eq.hardpointIdx);
			i++;
		}
		for (; i < optionItems.Length; i++)
		{
			optionItems[i].gameObject.SetActive(value: false);
		}
		UpdateArmedText();
		UpdateJettisonText();
	}

	public void JettisonButton()
	{
		equip.markedForJettison = !equip.markedForJettison;
		sms.weaponManager.ReportEquipJettisonMark(equip);
		sms.weaponManager.RefreshWeapon();
		UpdateJettisonText();
		sms.quarter.half.manager.PlayInputSound();
	}

	public void ArmedButton()
	{
		equip.armed = !equip.armed;
		sms.weaponManager.ReportWeaponArming(equip);
		sms.weaponManager.RefreshWeapon();
		UpdateArmedText();
		sms.quarter.half.manager.PlayInputSound();
	}

	private void UpdateArmedText()
	{
		if ((bool)equip)
		{
			if (!equip.armable)
			{
				armedText.gameObject.SetActive(value: false);
				return;
			}
			armedText.gameObject.SetActive(value: true);
			armedText.text = (equip.armed ? s_sms_armed : s_sms_disarmed);
			armedText.color = (equip.armed ? Color.green : Color.white);
		}
	}

	private void UpdateJettisonText()
	{
		if ((bool)equip)
		{
			if (!equip.jettisonable)
			{
				jettisonText.gameObject.SetActive(value: false);
				return;
			}
			jettisonText.gameObject.SetActive(value: true);
			jettisonText.text = (equip.markedForJettison ? $"[ {s_sms_jettison} ]" : s_sms_jettison);
			jettisonText.color = (equip.markedForJettison ? Color.red : Color.white);
		}
	}
}
