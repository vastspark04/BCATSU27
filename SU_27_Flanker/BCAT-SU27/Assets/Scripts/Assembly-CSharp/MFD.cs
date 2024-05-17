using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MFD : ElectronicComponent, IQSVehicleComponent
{
	[Serializable]
	public enum MFDButtons
	{
		L1,
		L2,
		L3,
		L4,
		L5,
		R1,
		R2,
		R3,
		R4,
		R5,
		T1Home,
		T2,
		T3,
		T4,
		T5,
		B1,
		B2,
		B3,
		B4,
		B5
	}

	private class MFDButtonEvents
	{
		public UnityEvent OnPress;

		public UnityEvent OnHold;

		public UnityEvent OnRelease;

		public MFDButtonEvents(UnityEvent onPress, UnityEvent onHold, UnityEvent onRelease)
		{
			OnPress = onPress;
			OnHold = onHold;
			OnRelease = onRelease;
		}
	}

	private class MFDButtonComp
	{
		public VRInteractable interactable;

		public Text text;

		public ContentSizeFitter sizeFitter;

		public Image bgImage;

		public VRButton vrButton;
	}

	public GameObject displayObject;

	public bool powerOn;

	private bool hasPower;

	public float powerDrain = 5f;

	public VRTwistKnobInt powerKnob;

	public GameObject activeInputDisplayObject;

	private MFDPage homePage;

	private Dictionary<MFDButtons, MFDButtonEvents> buttonEvents;

	private Dictionary<MFDButtons, MFDButtonComp> buttonComps;

	public VRInteractable[] buttons;

	private List<MFDButtons> heldButtons = new List<MFDButtons>();

	private bool quickloaded;

	private string quickLoadedPage;

	public MFDManager manager { get; private set; }

	public MFDPage activePage { get; private set; }

	public void UpdateInputDisplayObject()
	{
		activeInputDisplayObject.SetActive(activePage.isSOI);
	}

	public void Initialize(MFDManager manager, MFDPage homePage)
	{
		this.homePage = homePage;
		this.manager = manager;
		SetupButtonComps();
		ClearButtons();
		if (!quickloaded)
		{
			GoHome();
		}
		else
		{
			OpenPage(quickLoadedPage);
		}
		displayObject.SetActive(powerOn);
	}

	public void SetPower(int p)
	{
		if (p == 0)
		{
			TurnOff();
		}
		else
		{
			TurnOn();
		}
	}

	public void TogglePower()
	{
		if (!powerOn)
		{
			TurnOn();
		}
		else
		{
			TurnOff();
		}
	}

	public void TurnOn()
	{
		powerOn = true;
		if (quickloaded)
		{
			quickloaded = false;
			StartCoroutine(DelayedQuickLoadPage());
			Debug.Log("Quickloaded MFD: " + base.gameObject.name + " : " + quickLoadedPage);
		}
	}

	private IEnumerator DelayedQuickLoadPage()
	{
		yield return null;
		OpenPage(quickLoadedPage);
	}

	public void TurnOff()
	{
		if ((bool)activePage && activePage.locked)
		{
			if ((bool)powerKnob)
			{
				powerKnob.GetComponent<VRInteractable>().activeController?.ReleaseFromInteractable();
				powerKnob.RemoteSetState(1);
			}
		}
		else
		{
			powerOn = false;
		}
	}

	public void PressButton(int buttonIdx)
	{
		PressButton((MFDButtons)buttonIdx);
	}

	public void PressButton(MFDButtons button)
	{
		if (!powerOn)
		{
			return;
		}
		if (button == MFDButtons.T1Home)
		{
			GoHome();
			return;
		}
		heldButtons.Add(button);
		if (buttonEvents.TryGetValue(button, out var value) && value.OnPress != null)
		{
			buttonEvents[button].OnPress.Invoke();
		}
	}

	public void HoldButton(MFDButtons button)
	{
		if (powerOn && button != MFDButtons.T1Home && buttonEvents.TryGetValue(button, out var value) && value.OnHold != null)
		{
			buttonEvents[button].OnHold.Invoke();
		}
	}

	public void ReleaseButton(MFDButtons button)
	{
		if (powerOn && heldButtons.Contains(button) && button != (MFDButtons)(-1))
		{
			heldButtons.Remove(button);
			if (buttonEvents.TryGetValue(button, out var value) && value.OnRelease != null)
			{
				buttonEvents[button].OnRelease.Invoke();
			}
		}
	}

	private void ReleaseAllHeldButtons()
	{
		for (int num = heldButtons.Count - 1; num >= 0; num--)
		{
			ReleaseButton(heldButtons[num]);
		}
	}

	public void GoHome()
	{
		SetPage(homePage, isHomepage: true);
	}

	public MFDPage OpenPage(string pageName)
	{
		manager.EnsureReady();
		MFDPage page = manager.GetPage(pageName);
		if (page.locked)
		{
			return null;
		}
		SetPage(page, isHomepage: false);
		return page;
	}

	public void SetPage(string pageName)
	{
		if (!hasPower)
		{
			StartCoroutine(DelayedSetPage(pageName));
		}
		else
		{
			OpenPage(pageName);
		}
	}

	private IEnumerator DelayedSetPage(string pageName)
	{
		yield return null;
		if (hasPower)
		{
			SetPage(pageName);
		}
	}

	private void SetPage(MFDPage page, bool isHomepage)
	{
		if ((bool)activePage)
		{
			if (activePage.locked)
			{
				return;
			}
			ReleaseAllHeldButtons();
		}
		if (!isHomepage)
		{
			page.GoHome();
		}
		if ((bool)activePage)
		{
			activePage.Release();
			activePage.gameObject.SetActive(value: false);
		}
		activePage = page;
		page.transform.SetParent(displayObject.transform, worldPositionStays: false);
		page.gameObject.SetActive(value: true);
		page.transform.localPosition = Vector3.zero;
		page.transform.localRotation = Quaternion.identity;
		page.transform.localScale = Vector3.one;
		page.transform.SetAsFirstSibling();
		page.Initialize(this);
		UpdateInputDisplayObject();
	}

	private void SetupButtonComps()
	{
		buttonComps = new Dictionary<MFDButtons, MFDButtonComp>();
		for (int i = 0; i < buttons.Length; i++)
		{
			MFDButtons mb = (MFDButtons)i;
			MFDButtonComp mFDButtonComp = new MFDButtonComp();
			mFDButtonComp.interactable = buttons[i];
			mFDButtonComp.text = mFDButtonComp.interactable.GetComponentInChildren<Text>();
			mFDButtonComp.sizeFitter = mFDButtonComp.text.gameObject.AddComponent<ContentSizeFitter>();
			mFDButtonComp.sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			mFDButtonComp.bgImage = new GameObject("BGImage").AddComponent<Image>();
			mFDButtonComp.bgImage.color = new Color(0f, 0f, 0f, 0.65f);
			mFDButtonComp.bgImage.transform.SetParent(mFDButtonComp.text.transform.parent);
			mFDButtonComp.bgImage.rectTransform.pivot = mFDButtonComp.text.rectTransform.pivot;
			mFDButtonComp.bgImage.transform.localPosition = mFDButtonComp.text.transform.localPosition;
			mFDButtonComp.bgImage.transform.localRotation = mFDButtonComp.text.transform.localRotation;
			mFDButtonComp.bgImage.transform.localScale = mFDButtonComp.text.transform.localScale;
			mFDButtonComp.bgImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, mFDButtonComp.text.rectTransform.rect.height);
			mFDButtonComp.bgImage.transform.SetAsFirstSibling();
			buttonComps.Add(mb, mFDButtonComp);
			buttons[i].OnInteract.AddListener(delegate
			{
				PressButton(mb);
			});
			buttons[i].OnInteracting.AddListener(delegate
			{
				HoldButton(mb);
			});
			buttons[i].OnStopInteract.AddListener(delegate
			{
				ReleaseButton(mb);
			});
			mFDButtonComp.vrButton = buttons[i].GetComponent<VRButton>();
		}
	}

	public void ClearButtons()
	{
		if (buttons == null)
		{
			return;
		}
		ReleaseAllHeldButtons();
		buttonEvents = new Dictionary<MFDButtons, MFDButtonEvents>();
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].enabled = false;
			for (int j = 0; j < buttons[i].transform.childCount; j++)
			{
				buttons[i].transform.GetChild(j).gameObject.SetActive(value: false);
			}
		}
		SetButton(MFDButtons.T1Home, VTLStaticStrings.mfd_home.ToUpper(), VTLStaticStrings.mfd_home, null, null, null);
	}

	public void SetButton(MFDButtons button, string label, string tooltip, UnityEvent onPress, UnityEvent onHold, UnityEvent onRelease)
	{
		if (buttons != null && buttons.Length != 0)
		{
			buttons[(int)button].enabled = true;
			buttons[(int)button].interactableName = tooltip;
			MFDButtonEvents value = new MFDButtonEvents(onPress, onHold, onRelease);
			if (buttonEvents.ContainsKey(button))
			{
				buttonEvents[button] = value;
			}
			else
			{
				buttonEvents.Add(button, value);
			}
			MFDButtonComp mFDButtonComp = buttonComps[button];
			if (label != null)
			{
				label = label.Trim();
			}
			if (!string.IsNullOrEmpty(label))
			{
				mFDButtonComp.text.text = label;
				mFDButtonComp.text.gameObject.SetActive(value: true);
				mFDButtonComp.bgImage.gameObject.SetActive(value: true);
				mFDButtonComp.sizeFitter.SetLayoutHorizontal();
				mFDButtonComp.bgImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, mFDButtonComp.text.rectTransform.rect.width + 4f);
			}
			else
			{
				mFDButtonComp.text.gameObject.SetActive(value: false);
				mFDButtonComp.bgImage.gameObject.SetActive(value: false);
			}
			if (mFDButtonComp.vrButton != null)
			{
				mFDButtonComp.vrButton.pressAndHold = onRelease.HasListeners() || onHold.HasListeners();
			}
		}
	}

	private void Update()
	{
		if (powerOn)
		{
			if (DrainElectricity(powerDrain * Time.deltaTime))
			{
				hasPower = true;
				if (!displayObject.activeSelf)
				{
					displayObject.SetActive(value: true);
					GoHome();
				}
			}
			else
			{
				ReleaseAllHeldButtons();
				hasPower = false;
				if (displayObject.activeSelf)
				{
					displayObject.SetActive(value: false);
				}
			}
		}
		else
		{
			ReleaseAllHeldButtons();
			hasPower = false;
			if (displayObject.activeSelf)
			{
				displayObject.SetActive(value: false);
			}
		}
	}

	public void OnInputButtonDown()
	{
		if (hasPower && activePage.isSOI)
		{
			activePage.OnInputButtonDown.Invoke();
		}
	}

	public void OnInputButtonUp()
	{
		if (hasPower && activePage.isSOI)
		{
			activePage.OnInputButtonUp.Invoke();
		}
	}

	public void OnInputButton()
	{
		if (hasPower && activePage.isSOI)
		{
			activePage.OnInputButton.Invoke();
		}
	}

	public void OnInputAxis(Vector3 axis)
	{
		if (hasPower && activePage.isSOI)
		{
			activePage.OnInputAxis.Invoke(axis);
		}
	}

	public void OnInputAxisReleased()
	{
		if (hasPower && activePage.isSOI)
		{
			activePage.OnInputAxisReleased.Invoke();
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode(base.gameObject.name + "_MFD");
		configNode.SetValue("powerOn", powerOn);
		if (!activePage.pageName.Equals(homePage.pageName))
		{
			configNode.SetValue("activePage", activePage.pageName);
		}
		qsNode.AddNode(configNode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = base.gameObject.name + "_MFD";
		if (qsNode.HasNode(text))
		{
			ConfigNode node = qsNode.GetNode(text);
			powerOn = ConfigNodeUtils.ParseBool(node.GetValue("powerOn"));
			displayObject.SetActive(powerOn);
			if (powerOn && node.HasValue("activePage"))
			{
				quickLoadedPage = node.GetValue("activePage");
				quickloaded = true;
				SetPage(quickLoadedPage);
			}
		}
	}
}
