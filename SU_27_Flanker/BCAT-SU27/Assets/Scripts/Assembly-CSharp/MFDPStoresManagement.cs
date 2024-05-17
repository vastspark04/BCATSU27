using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MFDPStoresManagement : MFDPortalPage, IQSVehicleComponent, ILocalizationUser
{
	public enum SMSModes
	{
		Config,
		Arming,
		Jettison
	}

	[Header("Stores Management System")]
	public SMSWpnInfo[] wpnInfos;

	public GameObject masterSafeObj;

	public WeaponManager weaponManager;

	public SMSEquipConfigView configView;

	public GameObject wbdOverridesObj;

	public GameObject displayObj;

	[Header("Modes")]
	public Text modeText;

	public Image modeImage;

	public Color[] modeColors;

	private string[] modeNames = new string[3] { "Config", "Arming", "Jettison" };

	[Header("Fuel Config")]
	public SMSFuelConfigView fuelConfigView;

	private HPEquippable displayedEq;

	public CMSConfigUI cmsConfig;

	public SMSModes smsMode { get; private set; }

	public override void ApplyLocalization()
	{
		base.ApplyLocalization();
		for (int i = 0; i < modeNames.Length; i++)
		{
			string key = $"s_sms_mode_{i}";
			modeNames[i] = VTLocalizationManager.GetString(key, modeNames[i], "SMS mode button labels");
		}
	}

	protected override void Awake()
	{
		base.Awake();
		configView.gameObject.SetActive(value: false);
		displayObj.SetActive(value: true);
		modeImage.color = modeColors[(int)smsMode];
		modeText.text = modeNames[(int)smsMode].ToUpper();
	}

	public void SetMasterArmed(int idx)
	{
		masterSafeObj.SetActive(idx == 0);
	}

	public void OpenConfigView(HPEquippable hpEq)
	{
		displayedEq = hpEq;
		configView.gameObject.SetActive(value: true);
		configView.Display(hpEq);
		displayObj.SetActive(value: false);
	}

	public void CloseConfigView()
	{
		configView.gameObject.SetActive(value: false);
		displayObj.SetActive(value: true);
		quarter.half.manager.PlayInputSound();
	}

	public void OpenWBDOverrides()
	{
		displayObj.SetActive(value: false);
		wbdOverridesObj.SetActive(value: true);
	}

	public void CloseWBDOverrides()
	{
		displayObj.SetActive(value: true);
		wbdOverridesObj.SetActive(value: false);
	}

	public void ToggleMode()
	{
		smsMode = (SMSModes)((int)(smsMode + 1) % 3);
		modeImage.color = modeColors[(int)smsMode];
		modeText.text = modeNames[(int)smsMode].ToUpper();
	}

	public void FuelConfigButton()
	{
		fuelConfigView.gameObject.SetActive(value: true);
		displayObj.SetActive(value: false);
	}

	public void CloseFuelConfig()
	{
		fuelConfigView.gameObject.SetActive(value: false);
		displayObj.SetActive(value: true);
	}

	public void CMSConfigButton()
	{
		cmsConfig.gameObject.SetActive(value: true);
		displayObj.SetActive(value: false);
	}

	public void CloseCMSConfigButton()
	{
		cmsConfig.gameObject.SetActive(value: false);
		displayObj.SetActive(value: true);
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("MFDPStoresManagement");
		configNode.SetValue("smsMode", smsMode);
		configNode.SetValue("fuelDisplayed", fuelConfigView.gameObject.activeSelf);
		configNode.SetValue("wbdDisplayed", wbdOverridesObj.activeSelf);
		int value = -1;
		if (configView.gameObject.activeSelf && displayedEq != null)
		{
			value = displayedEq.hardpointIdx;
		}
		configNode.SetValue("dispEq", value);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("MFDPStoresManagement");
		if (node != null)
		{
			SMSModes value = node.GetValue<SMSModes>("smsMode");
			while (smsMode != value)
			{
				ToggleMode();
			}
			if (node.GetValue<bool>("fuelDisplayed"))
			{
				FuelConfigButton();
			}
			if (node.GetValue<bool>("wbdDisplayed"))
			{
				OpenWBDOverrides();
			}
			int value2 = node.GetValue<int>("dispEq");
			if (value2 >= 0 && base.gameObject.activeInHierarchy)
			{
				StartCoroutine(QL_ConfigViewRoutine(value2));
			}
		}
	}

	private IEnumerator QL_ConfigViewRoutine(int dispEq)
	{
		yield return null;
		for (int i = 0; i < 5; i++)
		{
			HPEquippable equip = weaponManager.GetEquip(dispEq);
			if ((bool)equip)
			{
				OpenConfigView(equip);
				break;
			}
			yield return null;
		}
	}
}
