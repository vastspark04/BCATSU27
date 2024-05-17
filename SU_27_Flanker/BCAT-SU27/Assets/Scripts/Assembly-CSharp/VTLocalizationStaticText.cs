using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VTLocalizationStaticText : MonoBehaviour, ILocalizationUser
{
	public enum FormatOptions
	{
		None,
		ToUpper,
		ToLower,
		Keep
	}

	public string key;

	public string description;

	private VTText vtt;

	private VRInteractable vri;

	private Text text;

	private TextMeshPro tmp;

	private string defaultText;

	public FormatOptions format;

	private bool hasInit;

	private string localizedString = string.Empty;

	private bool wasLocalized;

	private void Init()
	{
		if (!hasInit)
		{
			hasInit = true;
			vtt = GetComponent<VTText>();
			vri = GetComponent<VRInteractable>();
			text = GetComponent<Text>();
			tmp = GetComponent<TextMeshPro>();
			if (format == FormatOptions.Keep)
			{
				defaultText = GetText();
			}
			else
			{
				defaultText = CapitalizeFirst(GetText());
			}
		}
	}

	private string CapitalizeFirst(string s)
	{
		if (s.Length < 2)
		{
			return s.ToUpper();
		}
		return $"{s.Substring(0, 1).ToUpper()}{s.Substring(1, s.Length - 1).ToLower()}";
	}

	public void ApplyLocalization()
	{
		Init();
		switch (format)
		{
		case FormatOptions.ToUpper:
			localizedString = VTLocalizationManager.GetString(key, defaultText, description).ToUpper();
			break;
		case FormatOptions.ToLower:
			localizedString = VTLocalizationManager.GetString(key, defaultText, description).ToLower();
			break;
		default:
			localizedString = VTLocalizationManager.GetString(key, defaultText, description);
			break;
		}
		wasLocalized = true;
	}

	public string GetLocalizedString()
	{
		if (!wasLocalized)
		{
			ApplyLocalization();
		}
		return localizedString;
	}

	private void Awake()
	{
		Init();
		SetText();
		VTLocalizationManager.OnSetLangauge += SetLanguage;
	}

	private void SetLanguage(string obj)
	{
		wasLocalized = false;
		SetText();
	}

	private void OnDestroy()
	{
		VTLocalizationManager.OnSetLangauge -= SetLanguage;
	}

	private void SetText()
	{
		string interactableName = GetLocalizedString();
		if ((bool)vtt)
		{
			vtt.text = interactableName;
			vtt.ApplyText();
		}
		if ((bool)vri)
		{
			vri.interactableName = interactableName;
		}
		if ((bool)text)
		{
			text.text = interactableName;
			text.SetAllDirty();
		}
		if ((bool)tmp)
		{
			tmp.text = interactableName;
			tmp.SetAllDirty();
		}
	}

	private string GetText()
	{
		if ((bool)vtt)
		{
			return vtt.text;
		}
		if ((bool)vri)
		{
			return vri.interactableName;
		}
		if ((bool)text)
		{
			return text.text;
		}
		if ((bool)tmp)
		{
			return tmp.text;
		}
		return string.Empty;
	}
}
