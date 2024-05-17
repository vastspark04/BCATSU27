using UnityEngine;
using UnityEngine.UI;

public class SMSWpnInfo : MonoBehaviour
{
	public MFDPStoresManagement sms;

	public MFDPortalPage portalPage;

	public WeaponManager wm;

	public int hpIdx;

	public VRInteractable interactable;

	public Color armedColor;

	public Color disarmedColor;

	public Color jettisonColor;

	public Color hoverColor;

	public Text nameText;

	public Text countText;

	public Image borderImage;

	public GameObject selectedObj;

	private HPEquippable equip;

	private bool hovering;

	private void Awake()
	{
		interactable.OnHover += Interactable_OnHover;
		interactable.OnUnHover += Interactable_OnUnHover;
		interactable.OnStartInteraction += Interactable_OnStartInteraction;
		wm.OnWeaponChanged.AddListener(OnWeaponChanged);
	}

	private void OnEnable()
	{
		equip = wm.GetEquip(hpIdx);
	}

	private void OnWeaponChanged()
	{
		equip = wm.GetEquip(hpIdx);
	}

	private void Interactable_OnStartInteraction(VRHandController controller)
	{
		portalPage.quarter.half.manager.PlayInputSound();
		switch (sms.smsMode)
		{
		case MFDPStoresManagement.SMSModes.Config:
			sms.OpenConfigView(equip);
			break;
		case MFDPStoresManagement.SMSModes.Arming:
			if ((bool)equip && equip.armable)
			{
				equip.armed = !equip.armed;
				wm.ReportWeaponArming(equip);
				wm.RefreshWeapon();
			}
			break;
		case MFDPStoresManagement.SMSModes.Jettison:
			if ((bool)equip && equip.jettisonable)
			{
				equip.markedForJettison = !equip.markedForJettison;
				wm.ReportEquipJettisonMark(equip);
				wm.RefreshWeapon();
			}
			break;
		}
	}

	private void Interactable_OnUnHover(VRHandController controller)
	{
		hovering = false;
	}

	private void Interactable_OnHover(VRHandController controller)
	{
		hovering = true;
	}

	private void UpdateBorderColor()
	{
		if ((bool)equip)
		{
			interactable.enabled = true;
			if (hovering)
			{
				borderImage.color = hoverColor;
			}
			else if (equip.markedForJettison)
			{
				borderImage.color = jettisonColor;
			}
			else if (equip.armed)
			{
				borderImage.color = armedColor;
			}
			else
			{
				borderImage.color = disarmedColor;
			}
		}
		else
		{
			interactable.enabled = false;
			borderImage.color = disarmedColor;
		}
	}

	private void UpdateTexts()
	{
		if ((bool)equip)
		{
			nameText.gameObject.SetActive(value: true);
			countText.gameObject.SetActive(value: true);
			nameText.text = equip.shortName;
			countText.text = equip.GetCount().ToString();
		}
		else
		{
			nameText.gameObject.SetActive(value: false);
			countText.gameObject.SetActive(value: false);
		}
	}

	private void UpdateSelected()
	{
		if ((bool)equip && wm.currentEquip == equip)
		{
			selectedObj.SetActive(value: true);
		}
		else
		{
			selectedObj.SetActive(value: false);
		}
	}

	private void Update()
	{
		if (portalPage.pageState != MFDPortalPage.PageStates.Minimized && portalPage.pageState != MFDPortalPage.PageStates.SubSized)
		{
			UpdateBorderColor();
			UpdateTexts();
			UpdateSelected();
		}
	}
}
