using System.IO;
using UnityEngine.UI;

public class VTVideoProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text valueText;

	private VTSVideoReference currentValue;

	public override void SetInitialValue(object value)
	{
		currentValue = (VTSVideoReference)value;
		UpdateText();
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public void EditButton()
	{
		if (string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			editor.confirmDialogue.DisplayConfirmation("Save Required", "The scenario must be saved to a file before importing video.", editor.Save, null);
		}
		else
		{
			editor.resourceBrowser.OpenBrowser("Select Video", OnSelectedVideo, VTResources.supportedVideoExtensions);
		}
	}

	private void OnSelectedVideo(string vidUrl)
	{
		if (currentValue == null)
		{
			currentValue = new VTSVideoReference();
		}
		currentValue.relativeUrl = vidUrl;
		currentValue.resourceDirty = true;
		UpdateText();
		ValueChanged();
	}

	private void UpdateText()
	{
		if (currentValue != null && !string.IsNullOrEmpty(currentValue.relativeUrl))
		{
			valueText.text = Path.GetFileName(currentValue.relativeUrl);
		}
		else
		{
			valueText.text = "Select Video";
		}
	}
}
