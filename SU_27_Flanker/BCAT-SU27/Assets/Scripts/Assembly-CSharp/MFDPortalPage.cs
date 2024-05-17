using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MFDPortalPage : MonoBehaviour, ILocalizationUser
{
	public enum PageStates
	{
		Uninitialized,
		Standard,
		FullHeight,
		SubSized,
		Maximized,
		Minimized
	}

	public string pageName;

	public string pageLabel;

	public UnityEvent OnShowPage;

	public UnityEvent OnHidePage;

	public bool canSOI;

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

	public List<Text> textFields;

	private Dictionary<string, Text> textDict = new Dictionary<string, Text>();

	public GameObject[] hideOnSubsized;

	public GameObject[] showOnSubsized;

	[Header("Runtime")]
	public bool isSOI;

	public MFDPortalQuarter quarter;

	public PageStates pageState;

	public bool locked;

	public MFDPortalPageSelectButton minimizedButton;

	private bool appliedLoc;

	private bool hasSetupTextDict;

	public RectTransform rectTransform => (RectTransform)base.transform;

	public event UnityAction<PageStates> OnSetPageStateEvent;

	public virtual void ApplyLocalization()
	{
		string key = $"{base.gameObject.name}_MFDPortalPage_pageName";
		string key2 = $"{base.gameObject.name}_MFDPortalPage_pageLabel";
		string @string = VTLocalizationManager.GetString(key, pageName, "MFD portal page name");
		string string2 = VTLocalizationManager.GetString(key2, pageLabel, "MFD portal page button label");
		if (Application.isPlaying && !appliedLoc)
		{
			appliedLoc = true;
			pageName = @string;
			pageLabel = string2;
		}
	}

	protected virtual void Awake()
	{
		ApplyLocalization();
		SetupTextDict();
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
			if ((bool)textField)
			{
				textDict.Add(textField.gameObject.name, textField);
			}
		}
	}

	private void OnEnable()
	{
		if (pageState != 0)
		{
			PageStates pageStates = pageState;
			pageState = PageStates.Uninitialized;
			SetPageState(pageStates);
		}
	}

	protected virtual void OnSetPageState(PageStates s)
	{
	}

	private void SetPageState(PageStates s)
	{
		if (pageState != s)
		{
			pageState = s;
			if (s != PageStates.Minimized && OnShowPage != null)
			{
				OnShowPage.Invoke();
			}
			if (s == PageStates.SubSized || s == PageStates.Minimized)
			{
				hideOnSubsized.SetActive(active: false);
			}
			else
			{
				hideOnSubsized.SetActive(active: true);
			}
			showOnSubsized.SetActive(s == PageStates.SubSized);
			OnSetPageState(s);
			if (this.OnSetPageStateEvent != null)
			{
				this.OnSetPageStateEvent(s);
			}
			if (s == PageStates.Minimized && OnHidePage != null)
			{
				OnHidePage.Invoke();
			}
			if (isSOI && (s == PageStates.SubSized || s == PageStates.Minimized))
			{
				ToggleInput();
			}
		}
	}

	public void ToggleInput()
	{
		if (isSOI)
		{
			isSOI = false;
		}
		else
		{
			quarter.half.manager.DisableAllSOI();
			isSOI = true;
		}
		quarter.half.manager.UpdateSOIUIs();
		quarter.half.manager.PlayInputSound();
	}

	public void SetMaximized()
	{
		base.gameObject.SetActive(value: true);
		RectTransform rectTransform = (RectTransform)quarter.half.transform;
		base.transform.localPosition = Vector3.zero;
		this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width);
		this.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height);
		SetPageState(PageStates.Maximized);
	}

	public void SetMinimized()
	{
		SetPageState(PageStates.Minimized);
		base.gameObject.SetActive(value: false);
	}

	public void SetHideSubs()
	{
		base.gameObject.SetActive(value: true);
		RectTransform displayTransform = quarter.displayTransform;
		base.transform.localPosition = Vector3.zero;
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, displayTransform.rect.width);
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, displayTransform.rect.height);
		SetPageState(PageStates.FullHeight);
	}

	public void SetDisplayAll()
	{
		if (!quarter)
		{
			Debug.LogWarning("MFDPortalPage was SetDisplayAll with no quarter: " + pageName);
			return;
		}
		base.gameObject.SetActive(value: true);
		RectTransform displayTransform = quarter.displayTransform;
		base.transform.localPosition = Vector3.zero;
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, displayTransform.rect.width);
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, displayTransform.rect.height);
		SetPageState(PageStates.Standard);
	}

	public void SetDisplayAsSub(RectTransform subRect)
	{
		base.gameObject.SetActive(value: true);
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, subRect.rect.width);
		rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, subRect.rect.height);
		SetPageState(PageStates.SubSized);
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
}
