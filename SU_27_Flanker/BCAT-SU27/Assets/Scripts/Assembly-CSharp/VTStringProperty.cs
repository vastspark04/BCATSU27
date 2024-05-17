using UnityEngine;
using UnityEngine.UI;

public class VTStringProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public int charLimit;

	public int labelCharLimit = 26;

	public Text buttonLabel;

	public bool multiLine;

	private Color origColor;

	private string val;

	private void Awake()
	{
		origColor = buttonLabel.color;
	}

	public override void SetInitialValue(object value)
	{
		base.SetInitialValue(value);
		val = (string)value;
		if (val == null)
		{
			val = string.Empty;
		}
		UpdateLabel();
	}

	public override object GetValue()
	{
		return val;
	}

	private void UpdateLabel()
	{
		if (string.IsNullOrEmpty(val))
		{
			buttonLabel.color = Color.gray;
			buttonLabel.text = "Enter text...";
			return;
		}
		buttonLabel.color = origColor;
		string text = val.Replace("\n", " ");
		string text2 = text.Substring(0, Mathf.Min(val.Length, labelCharLimit));
		if (labelCharLimit < text.Length)
		{
			text2 += "...";
		}
		buttonLabel.text = text2;
	}

	public void EditStringButton()
	{
		if (multiLine)
		{
			editor.textInputWindowLong.Display("Enter Text", labelText.text, val, charLimit, OnEntered);
		}
		else
		{
			editor.textInputWindow.Display("Enter Text", labelText.text, val, charLimit, OnEntered);
		}
	}

	private void OnEntered(string s)
	{
		val = s;
		UpdateLabel();
		ValueChanged();
	}
}
