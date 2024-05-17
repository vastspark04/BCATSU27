using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VTLocalizationComponentKey : MonoBehaviour
{
	public string key;

	public string description;

	private VTText vtt;

	private VRInteractable vri;

	private Text text;

	private TextMeshPro tmp;

	private string fallbackText;

	private bool hasInit;

	private void Init()
	{
		if (!hasInit)
		{
			hasInit = true;
			vtt = GetComponent<VTText>();
			vri = GetComponent<VRInteractable>();
			text = GetComponent<Text>();
			tmp = GetComponent<TextMeshPro>();
			fallbackText = GetText();
		}
	}

	[ContextMenu("Set Key")]
	public void SetKey()
	{
		key = base.gameObject.name + "_" + base.gameObject.GetInstanceID();
	}

	private void Awake()
	{
		Init();
	}

	public void SetText(string s)
	{
		Init();
		if (string.IsNullOrEmpty(s))
		{
			s = fallbackText;
		}
		if ((bool)vtt)
		{
			vtt.text = s;
			vtt.ApplyText();
		}
		if ((bool)vri)
		{
			vri.interactableName = s;
		}
		if ((bool)text)
		{
			text.text = s;
			text.SetAllDirty();
		}
		if ((bool)tmp)
		{
			tmp.text = s;
			tmp.SetAllDirty();
		}
	}

	public string GetText()
	{
		Init();
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

	public string GetDescription()
	{
		if (string.IsNullOrEmpty(description))
		{
			Init();
			if ((bool)vtt)
			{
				return "VT Text";
			}
			if ((bool)vri)
			{
				return "VR Interactable";
			}
			if ((bool)text)
			{
				return "UI Text";
			}
			if ((bool)tmp)
			{
				return "Text Mesh";
			}
			return string.Empty;
		}
		return description;
	}
}
