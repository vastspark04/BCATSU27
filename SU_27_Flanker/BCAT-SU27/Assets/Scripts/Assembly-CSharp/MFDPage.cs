using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class MFDPage : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	[Serializable]
	public class MFDButtonInfo
	{
		public string label;

		public string toolTip;

		public MFD.MFDButtons button;

		public UnityEvent OnPress;

		public UnityEvent OnHold;

		public UnityEvent OnRelease;

		public bool spOnly;

		public bool mpOnly;

		public MFDButtonInfo()
		{
			OnPress = new UnityEvent();
			OnHold = new UnityEvent();
			OnRelease = new UnityEvent();
		}
	}

	public string pageName;

	public MFDButtonInfo[] buttons;

	private List<MFDButtonInfo> modifiedButtons;

	public UnityEvent OnActivatePage;

	public UnityEvent OnDeactivatePage;

	[HideInInspector]
	public Vector3Event OnInputAxis;

	[HideInInspector]
	public UnityEvent OnInputAxisReleased;

	[HideInInspector]
	public UnityEvent OnInputButton;

	[HideInInspector]
	public UnityEvent OnInputButtonDown;

	[HideInInspector]
	public UnityEvent OnInputButtonUp;

	[HideInInspector]
	public MFDManager manager;

	public bool canSOI;

	public List<Text> textFields;

	private Dictionary<string, Text> textDict = new Dictionary<string, Text>();

	private bool _locked;

	private bool hasSetupTextDict;

	private bool appliedLocalization;

	private MFDPage activeSubpage;

	private MFDPage parentPage;

	public MFD mfd { get; private set; }

	public bool isOpen { get; private set; }

	public bool isSOI { get; set; }

	public bool locked
	{
		get
		{
			return _locked;
		}
		set
		{
			_locked = value;
			if (locked)
			{
				mfd.ClearButtons();
			}
			else
			{
				SetPageButtons(modifiedButtons.ToArray());
			}
		}
	}

	public event Action<bool> OnSetSOI;

	public void ToggleInput()
	{
		if (!isSOI)
		{
			foreach (MFDPage mfdPage in mfd.manager.mfdPages)
			{
				if (mfdPage.isSOI)
				{
					mfdPage.ToggleInput();
				}
			}
		}
		isSOI = !isSOI;
		if ((bool)mfd)
		{
			mfd.UpdateInputDisplayObject();
		}
		this.OnSetSOI?.Invoke(isSOI);
	}

	private void SetupTextDict()
	{
		if (hasSetupTextDict)
		{
			return;
		}
		hasSetupTextDict = true;
		foreach (Text textField in textFields)
		{
			textDict.Add(textField.gameObject.name, textField);
		}
	}

	private void Awake()
	{
		ApplyLocalization();
		SetupTextDict();
	}

	public void ApplyLocalization()
	{
		if (Application.isPlaying && appliedLocalization)
		{
			return;
		}
		appliedLocalization = true;
		VehicleMaster componentInChildren = base.transform.root.GetComponentInChildren<VehicleMaster>();
		if (!componentInChildren)
		{
			Debug.LogError("No VM to get pv from! No key! " + UIUtils.GetHierarchyString(base.gameObject));
			return;
		}
		if (!componentInChildren.playerVehicle)
		{
			Debug.LogError("No reference to pv in VM!  No key! " + UIUtils.GetHierarchyString(base.gameObject));
			return;
		}
		string vehicleName = componentInChildren.playerVehicle.vehicleName;
		MFDButtonInfo[] array = buttons;
		foreach (MFDButtonInfo mFDButtonInfo in array)
		{
			string text = $"{vehicleName}_mfd_{pageName}:{mFDButtonInfo.button}";
			if (!string.IsNullOrEmpty(mFDButtonInfo.label))
			{
				string @string = VTLocalizationManager.GetString(text, mFDButtonInfo.label, "Label for an MFD button (short!).");
				if (Application.isPlaying)
				{
					mFDButtonInfo.label = @string;
				}
			}
			if (!string.IsNullOrEmpty(mFDButtonInfo.toolTip))
			{
				string string2 = VTLocalizationManager.GetString(text + "_tooltip", mFDButtonInfo.toolTip, "Tooltip for an MFD button.");
				if (Application.isPlaying)
				{
					mFDButtonInfo.toolTip = string2;
				}
			}
		}
	}

	public void Initialize(MFD mfd)
	{
		this.mfd = mfd;
		ApplyLocalization();
		if ((bool)activeSubpage)
		{
			OpenSubpage(activeSubpage.pageName);
			return;
		}
		modifiedButtons = new List<MFDButtonInfo>();
		mfd.ClearButtons();
		if (buttons != null)
		{
			bool flag = VTOLMPUtils.IsMultiplayer();
			for (int i = 0; i < buttons.Length; i++)
			{
				if (!(buttons[i].spOnly && flag) && (!buttons[i].mpOnly || flag))
				{
					mfd.SetButton(buttons[i].button, buttons[i].label, string.IsNullOrEmpty(buttons[i].toolTip) ? buttons[i].label : buttons[i].toolTip, buttons[i].OnPress, buttons[i].OnHold, buttons[i].OnRelease);
					modifiedButtons.Add(buttons[i]);
				}
			}
		}
		isOpen = true;
		if (OnActivatePage != null)
		{
			OnActivatePage.Invoke();
		}
	}

	private void SetPageButtons(MFDButtonInfo[] newButtons)
	{
		mfd.ClearButtons();
		for (int i = 0; i < newButtons.Length; i++)
		{
			mfd.SetButton(newButtons[i].button, newButtons[i].label, string.IsNullOrEmpty(newButtons[i].toolTip) ? newButtons[i].label : newButtons[i].toolTip, newButtons[i].OnPress, newButtons[i].OnHold, newButtons[i].OnRelease);
		}
	}

	public void SetText(string fieldName, string text)
	{
		SetupTextDict();
		if (textDict.TryGetValue(fieldName, out var value))
		{
			value.text = text;
		}
	}

	public void SetText(string fieldName, string text, Color color)
	{
		SetupTextDict();
		if (textDict.TryGetValue(fieldName, out var value))
		{
			value.text = text;
			value.color = color;
		}
	}

	public void GoHome()
	{
		if ((bool)mfd)
		{
			mfd.GoHome();
		}
		Release();
	}

	public void Release()
	{
		if ((bool)mfd && isSOI)
		{
			ToggleInput();
		}
		mfd = null;
		isOpen = false;
		if (OnDeactivatePage != null)
		{
			OnDeactivatePage.Invoke();
		}
	}

	public void OpenPage(string pageName)
	{
		mfd.OpenPage(pageName);
	}

	private void SetParentPage(MFDPage page)
	{
		parentPage = page;
	}

	public void OpenSubpage(string pageName)
	{
		MFDPage mFDPage = mfd.OpenPage(pageName);
		if ((bool)mFDPage)
		{
			mFDPage.SetParentPage(this);
			activeSubpage = mFDPage;
		}
	}

	public void OpenParentPage()
	{
		if ((bool)mfd && (bool)parentPage && !parentPage.locked)
		{
			parentPage.activeSubpage = null;
			mfd.OpenPage(parentPage.pageName);
		}
	}

	public void OpenBasePage()
	{
		if ((bool)mfd && (bool)parentPage && !parentPage.locked)
		{
			parentPage.activeSubpage = null;
			mfd.OpenPage(parentPage.pageName);
			parentPage.OpenBasePage();
		}
	}

	public void SetPageButton(MFDButtonInfo info)
	{
		mfd.SetButton(info.button, info.label, info.toolTip, info.OnPress, info.OnHold, info.OnRelease);
		bool flag = false;
		for (int i = 0; i < modifiedButtons.Count; i++)
		{
			if (modifiedButtons[i].button == info.button)
			{
				modifiedButtons[i] = info;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			modifiedButtons.Add(info);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (!(pageName == "home"))
		{
			ConfigNode configNode = new ConfigNode("MFDPage_" + pageName + "_" + base.gameObject.name);
			configNode.SetValue("isSOI", isSOI);
			if (parentPage != null)
			{
				configNode.SetValue("parentPage", parentPage.pageName);
			}
			if (activeSubpage != null)
			{
				configNode.SetValue("activeSubpage", activeSubpage.pageName);
			}
			qsNode.AddNode(configNode);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (pageName == "home")
		{
			return;
		}
		string text = "MFDPage_" + pageName + "_" + base.gameObject.name;
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			isSOI = node.GetValue<bool>("isSOI");
			if ((bool)mfd)
			{
				mfd.UpdateInputDisplayObject();
			}
			if (node.HasValue("parentPage"))
			{
				string value = node.GetValue("parentPage");
				parentPage = manager.GetPage(value);
			}
			if (node.HasValue("activeSubpage"))
			{
				string value2 = node.GetValue("activeSubpage");
				activeSubpage = manager.GetPage(value2);
			}
		}
	}
}
