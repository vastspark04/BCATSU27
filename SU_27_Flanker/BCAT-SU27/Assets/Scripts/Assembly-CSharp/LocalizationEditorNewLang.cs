using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationEditorNewLang : MonoBehaviour
{
	public LocalizationEditorMainMenu mainMenu;

	public ScrollRect selLangScroll;

	public GameObject selLangTemplate;

	public InputField custLangField;

	public Text custLangErr;

	private Action<string> OnSelected;

	private List<string> knownCodes;

	private List<GameObject> langObjs = new List<GameObject>();

	public void CustomLanguageButton()
	{
		if (!string.IsNullOrEmpty(custLangField.text))
		{
			string text = custLangField.text.ToLower();
			if (VTLocalizationManager.SupportedLanguages.Contains(text))
			{
				custLangErr.text = $"Code '{text}' is already in use!";
			}
			else if (!IsAlphaOnly(text))
			{
				custLangErr.text = "Language code must contain letters only!";
			}
			else
			{
				SelectCode(text);
			}
		}
		else
		{
			custLangErr.text = "Enter a custom language code!";
		}
	}

	private bool IsAlphaOnly(string t)
	{
		char[] stringArray = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
		foreach (char s in t)
		{
			if (!stringArray.Contains(s))
			{
				return false;
			}
		}
		return true;
	}

	public void Open(Action<string> onSelected)
	{
		base.gameObject.SetActive(value: true);
		selLangTemplate.SetActive(value: false);
		custLangErr.text = string.Empty;
		OnSelected = onSelected;
		foreach (GameObject langObj in langObjs)
		{
			UnityEngine.Object.Destroy(langObj);
		}
		langObjs.Clear();
		custLangField.text = string.Empty;
		if (knownCodes == null)
		{
			knownCodes = VTLocalizationManager.GetKnownLanguageCodes();
		}
		int num = 0;
		float num2 = ((RectTransform)selLangTemplate.transform).rect.height * selLangTemplate.transform.localScale.y;
		foreach (string knownCode in knownCodes)
		{
			if (!VTLocalizationManager.SupportedLanguages.Contains(knownCode))
			{
				GameObject obj = UnityEngine.Object.Instantiate(selLangTemplate, selLangScroll.content);
				obj.SetActive(value: true);
				obj.transform.localPosition = new Vector3(0f, (float)(-num) * num2, 0f);
				obj.GetComponentInChildren<Text>().text = VTLocalizationManager.GetFullLanguageName(knownCode);
				string lc = knownCode;
				obj.GetComponentInChildren<Button>().onClick.AddListener(delegate
				{
					SelectCode(lc);
				});
				num++;
			}
		}
		selLangScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)num * num2);
		selLangScroll.verticalNormalizedPosition = 1f;
	}

	private void SelectCode(string langCode)
	{
		base.gameObject.SetActive(value: false);
		OnSelected?.Invoke(langCode);
	}

	public void CancelButton()
	{
		base.gameObject.SetActive(value: false);
		OnSelected?.Invoke(string.Empty);
	}
}
