using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class WeaponManagerUI : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	private HPEquippable equip;

	private int equipIdx;

	public Text fullNameText;

	public Text armedText;

	public Text jettisonText;

	public Text masterArmText;

	public Text rippleRateText;

	public MFDPage mfdPage;

	public MFDPage fullInfoMFDPage;

	public HUDWeaponInfo hudInfo;

	public WeaponManager weaponManager;

	public GameObject[] equipOptionObjects;

	public List<MFDHardpointInfo> hardpointInfos;

	private int hpCount;

	public EmissiveTextureLight masterArmLight;

	public EmissiveTextureLight jettisonLight;

	public ABObjectToggler masterArmABToggler;

	private int displayingFullPage = -1;

	private int mfdMode;

	private string[] modeLabels = new string[3] { "CONFIG", "ARMING", "JETTISON" };

	private Color[] modeColors = new Color[3]
	{
		Color.yellow,
		Color.green,
		Color.red
	};

	private string s_ripple_single;

	private string s_ripple;

	private string s_rippleTooltip;

	private string s_masterArmed = "MASTER ARMED";

	private string s_masterDisarmed = "MASTER DISARMED";

	private string s_eq_armed = "ARMED";

	private string s_eq_disarmed = "DISARMED";

	private string s_eq_jettison = "JETTISON";

	private bool armed;

	public MultiUserVehicleSync muvs;

	public int currentMode => mfdMode;

	public void ToggleMFDMode()
	{
		mfdMode = (mfdMode + 1) % 3;
		mfdPage.SetText("ModeText", modeLabels[mfdMode], modeColors[mfdMode]);
	}

	public void UIToggleMasterArmed()
	{
		weaponManager.ToggleMasterArmed();
		UpdateArmed();
	}

	public void UISetMasterArm(int i)
	{
		if (i > 0)
		{
			if (!weaponManager.isMasterArmed)
			{
				UIToggleMasterArmed();
			}
		}
		else if (weaponManager.isMasterArmed)
		{
			UIToggleMasterArmed();
		}
	}

	public void ApplyLocalization()
	{
		s_ripple_single = VTLocalizationManager.GetString("ripple_single", "SINGLE", "Label when a rapid-fire weapon is set to only fire one shot at a time.");
		s_ripple = VTLocalizationManager.GetString("wmui_ripple", "RIPPLE", "Option title for selecting a rocket/missile's ripple (rapid fire) rate.");
		s_rippleTooltip = VTLocalizationManager.GetString("wmui_rippleTooltip", "Cycle Ripple Rates", "Tooltip for button to cycle a weapon's ripple (rapid fire) rate.");
		s_masterArmed = VTLocalizationManager.GetString("s_masterArmed", s_masterArmed, "Master armed label in EQUIP MFD page");
		s_masterDisarmed = VTLocalizationManager.GetString("s_masterDisarmed", s_masterDisarmed, "Master disarmed label in EQUIP MFD page");
		s_eq_armed = VTLocalizationManager.GetString("s_eq_armed", s_eq_armed, "Armed label for single equip in EQUIP MFD page");
		s_eq_disarmed = VTLocalizationManager.GetString("s_eq_disarmed", s_eq_disarmed, "Disarmed label for single equip in EQUIP MFD page");
		s_eq_jettison = VTLocalizationManager.GetString("s_eq_jettison", s_eq_jettison, "Jettison label for single equip in EQUIP MFD page");
		for (int i = 0; i < modeLabels.Length; i++)
		{
			string key = $"s_eqMode_{i}";
			modeLabels[i] = VTLocalizationManager.GetString(key, modeLabels[i], "Equip MFD page mode label");
		}
	}

	private void Awake()
	{
		weaponManager.ui = this;
		ApplyLocalization();
	}

	private void Start()
	{
		fullInfoMFDPage.OnActivatePage.AddListener(OnActivateFullInfoPage);
		mfdPage.OnActivatePage.AddListener(OnOpenedMFDPage);
		if ((bool)jettisonLight)
		{
			jettisonLight.SetColor(Color.black);
		}
		hpCount = hardpointInfos.Count;
		hudInfo.wm = weaponManager;
		weaponManager.OnWeaponChanged.AddListener(OnWeaponChanged);
		UpdateArmed();
	}

	private void OnOpenedMFDPage()
	{
		mfdPage.SetText("ModeText", modeLabels[mfdMode], modeColors[mfdMode]);
	}

	private void Update()
	{
		if (weaponManager.isFiring)
		{
			UpdateDisplay();
		}
		if (armed != weaponManager.isMasterArmed)
		{
			UpdateArmed();
		}
	}

	private void OnWeaponChanged()
	{
		UpdateDisplay();
		hudInfo.SetWeapon(weaponManager.isMasterArmed ? weaponManager.currentEquip : null);
	}

	public void UpdateArmed()
	{
		armed = weaponManager.isMasterArmed;
		masterArmText.text = (armed ? s_masterArmed : s_masterDisarmed);
		masterArmText.color = (armed ? Color.red : new Color(180f, 180f, 180f, 1f));
		if ((bool)masterArmLight)
		{
			masterArmLight.SetColor(weaponManager.isMasterArmed ? Color.red : Color.black);
		}
		if ((bool)masterArmABToggler)
		{
			if (armed)
			{
				masterArmABToggler.SetToB();
			}
			else
			{
				masterArmABToggler.SetToA();
			}
		}
		if (!armed)
		{
			hudInfo.SetWeapon(null);
		}
	}

	public void UpdateEquip(int equipIdx)
	{
		hardpointInfos[equipIdx].equip = weaponManager.GetEquip(equipIdx);
		hardpointInfos[equipIdx].UpdateDisplay();
	}

	public void UpdateDisplay()
	{
		if (displayingFullPage >= 0 && weaponManager.GetEquip(displayingFullPage) == null)
		{
			ClearInfoPage();
			fullInfoMFDPage.OpenParentPage();
		}
		UpdateArmed();
		for (int i = 0; i < hpCount; i++)
		{
			hardpointInfos[i].equip = weaponManager.GetEquip(i);
			hardpointInfos[i].UpdateDisplay();
		}
		if (displayingFullPage < 0 || !(equip != null))
		{
			return;
		}
		fullNameText.text = equip.GetLocalizedFullName();
		if (equip.armable)
		{
			armedText.gameObject.SetActive(value: true);
			armedText.text = (equip.armed ? s_eq_armed : s_eq_disarmed);
			armedText.color = (equip.armed ? Color.green : Color.white);
		}
		else
		{
			armedText.gameObject.SetActive(value: false);
		}
		if (equip.jettisonable)
		{
			jettisonText.gameObject.SetActive(value: true);
			jettisonText.text = (equip.markedForJettison ? $"[{s_eq_jettison}]" : s_eq_jettison);
			jettisonText.color = (equip.markedForJettison ? Color.red : Color.white);
		}
		else
		{
			jettisonText.gameObject.SetActive(value: false);
		}
		if (equip is IRippleWeapon)
		{
			IRippleWeapon rippleWeapon = (IRippleWeapon)equip;
			float num = rippleWeapon.GetRippleRates()[rippleWeapon.GetRippleRateIdx()];
			if (num > 0f)
			{
				rippleRateText.text = Mathf.Round(num).ToString();
			}
			else
			{
				rippleRateText.text = s_ripple_single;
			}
		}
		UpdateEquipFunctions();
	}

	private void UpdateEquipFunctions()
	{
		for (int i = 0; i < equipOptionObjects.Length; i++)
		{
			equipOptionObjects[i].SetActive(value: false);
		}
		if (equip.equipFunctions == null)
		{
			return;
		}
		for (int j = 0; j < equipOptionObjects.Length && j < equip.equipFunctions.Length; j++)
		{
			GameObject gameObject = equipOptionObjects[j];
			HPEquippable.EquipFunction equipFunction = equip.equipFunctions[j];
			Text text = null;
			Text text2 = null;
			Text[] componentsInChildren = gameObject.GetComponentsInChildren<Text>();
			foreach (Text text3 in componentsInChildren)
			{
				if (text3.name == "name")
				{
					text = text3;
				}
				else if (text3.name == "subLabel")
				{
					text2 = text3;
				}
			}
			text.text = equipFunction.optionName;
			text2.text = equipFunction.optionReturnLabel;
			gameObject.SetActive(value: true);
		}
	}

	private void OnActivateFullInfoPage()
	{
		if (!equip)
		{
			equip = weaponManager.GetEquip(equipIdx);
			if (!equip)
			{
				Debug.LogError("WeaponManagerUI: Activated full info page with no equip!");
				fullInfoMFDPage.OpenParentPage();
				return;
			}
		}
		if (equip is IRippleWeapon)
		{
			rippleRateText.gameObject.SetActive(value: true);
			MFDPage.MFDButtonInfo mFDButtonInfo = new MFDPage.MFDButtonInfo();
			mFDButtonInfo.button = MFD.MFDButtons.L4;
			mFDButtonInfo.label = s_ripple;
			mFDButtonInfo.OnPress = new UnityEvent();
			mFDButtonInfo.OnPress.AddListener(MFDCycleRippleRates);
			mFDButtonInfo.toolTip = s_rippleTooltip;
			fullInfoMFDPage.SetPageButton(mFDButtonInfo);
		}
		else
		{
			rippleRateText.gameObject.SetActive(value: false);
		}
		if (equip.equipFunctions != null)
		{
			for (int i = 0; i < equipOptionObjects.Length && i < equip.equipFunctions.Length; i++)
			{
				HPEquippable.EquipFunction equipFunction = equip.equipFunctions[i];
				MFDPage.MFDButtonInfo mFDButtonInfo2 = new MFDPage.MFDButtonInfo();
				switch (i)
				{
				case 0:
					mFDButtonInfo2.button = MFD.MFDButtons.L2;
					break;
				case 1:
					mFDButtonInfo2.button = MFD.MFDButtons.L3;
					break;
				case 2:
					mFDButtonInfo2.button = MFD.MFDButtons.R2;
					break;
				case 3:
					mFDButtonInfo2.button = MFD.MFDButtons.R3;
					break;
				case 4:
					mFDButtonInfo2.button = MFD.MFDButtons.R4;
					break;
				}
				mFDButtonInfo2.label = string.Empty;
				mFDButtonInfo2.OnPress = new UnityEvent();
				int idx = i;
				UnityAction call = delegate
				{
					MFDWeaponFunctionButton(idx);
				};
				mFDButtonInfo2.OnPress.AddListener(call);
				mFDButtonInfo2.OnPress.AddListener(UpdateDisplay);
				mFDButtonInfo2.toolTip = equipFunction.optionName;
				fullInfoMFDPage.SetPageButton(mFDButtonInfo2);
			}
		}
		UpdateDisplay();
	}

	public void MFDHardpointButton(int idx)
	{
		if (displayingFullPage < 0 && weaponManager.GetEquip(idx) != null)
		{
			if (mfdMode == 0)
			{
				equip = weaponManager.GetEquip(idx);
				equipIdx = idx;
				displayingFullPage = idx;
				mfdPage.OpenSubpage("hardpointInfoPage");
			}
			else if (mfdMode == 1)
			{
				displayingFullPage = idx;
				MFDToggleArmedButton();
				displayingFullPage = -1;
			}
			else if (mfdMode == 2)
			{
				displayingFullPage = idx;
				MFDToggleJettisonButton();
				displayingFullPage = -1;
			}
		}
	}

	public void MFDToggleArmedButton()
	{
		weaponManager.EndAllTriggerAxis();
		if (weaponManager.isFiring)
		{
			weaponManager.EndAllFire();
		}
		if (displayingFullPage >= 0)
		{
			HPEquippable hPEquippable = weaponManager.GetEquip(displayingFullPage);
			if (hPEquippable.markedForJettison)
			{
				MFDToggleJettisonButton();
			}
			hPEquippable.armed = !hPEquippable.armed;
			weaponManager.ReportWeaponArming(hPEquippable);
			weaponManager.RefreshWeapon();
			UpdateDisplay();
		}
	}

	public void MFDToggleJettisonButton()
	{
		weaponManager.EndAllTriggerAxis();
		if (weaponManager.isFiring)
		{
			weaponManager.EndAllFire();
		}
		if (displayingFullPage >= 0)
		{
			HPEquippable hPEquippable = weaponManager.GetEquip(displayingFullPage);
			if (hPEquippable.jettisonable)
			{
				hPEquippable.markedForJettison = !hPEquippable.markedForJettison;
				weaponManager.ReportEquipJettisonMark(hPEquippable);
				weaponManager.RefreshWeapon();
				UpdateJettisonLight();
				UpdateDisplay();
			}
		}
	}

	public void MFDBackButton()
	{
		if (displayingFullPage >= 0)
		{
			displayingFullPage = -1;
			fullInfoMFDPage.OpenParentPage();
		}
	}

	public void MFDCycleRippleRates()
	{
		weaponManager.CycleRippleRates(displayingFullPage);
		UpdateDisplay();
	}

	public void MFDWeaponFunctionButton(int idx)
	{
		weaponManager.WeaponFunctionButton(idx, displayingFullPage);
		hudInfo.RefreshWeaponInfo();
	}

	private void UpdateJettisonLight()
	{
		if ((bool)jettisonLight)
		{
			bool flag = weaponManager.IsAnyWeaponMarkedJettison();
			jettisonLight.SetColor(flag ? new Color(1f, 0.1f, 0f, 1f) : Color.black);
		}
	}

	public void ClearInfoPage()
	{
		displayingFullPage = -1;
	}

	public void MarkEmptyToJettison()
	{
		weaponManager.MarkEmptyToJettison();
		UpdateJettisonLight();
		UpdateDisplay();
	}

	public void MarkAllJettison()
	{
		weaponManager.MarkAllJettison();
		UpdateJettisonLight();
		UpdateDisplay();
	}

	public void MarkTanksJettison()
	{
		weaponManager.MarkDroptanksToJettison();
		UpdateJettisonLight();
		UpdateDisplay();
	}

	public void MarkNoneJettison()
	{
		weaponManager.MarkNoneJettison();
		UpdateJettisonLight();
		UpdateDisplay();
	}

	public void JettisonMarkedItems()
	{
		if (VTOLMPUtils.IsMultiplayer() && (bool)muvs && !muvs.isMine)
		{
			muvs.RemoteJettisonItems();
			return;
		}
		weaponManager.JettisonMarkedItems();
		UpdateJettisonLight();
		UpdateDisplay();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("WeaponManagerUI");
		configNode.SetValue("mfdMode", mfdMode);
		configNode.SetValue("equipIdx", equipIdx);
		configNode.SetValue("fullInfoOpen", fullInfoMFDPage.isOpen);
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "WeaponManagerUI";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			mfdMode = node.GetValue<int>("mfdMode");
			equipIdx = node.GetValue<int>("equipIdx");
			displayingFullPage = equipIdx;
			equip = weaponManager.GetEquip(equipIdx);
			if (node.GetValue<bool>("fullInfoOpen"))
			{
				StartCoroutine(QuickloadFullInfoPageRoutine(equipIdx));
			}
		}
		mfdPage.SetText("ModeText", modeLabels[mfdMode], modeColors[mfdMode]);
	}

	private IEnumerator QuickloadFullInfoPageRoutine(int equipIdx)
	{
		while (!fullInfoMFDPage.mfd)
		{
			yield return null;
		}
		this.equipIdx = equipIdx;
		displayingFullPage = equipIdx;
		equip = weaponManager.GetEquip(equipIdx);
		OnActivateFullInfoPage();
	}
}
