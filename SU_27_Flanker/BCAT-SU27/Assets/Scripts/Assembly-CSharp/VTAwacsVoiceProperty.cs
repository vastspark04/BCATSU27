using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTAwacsVoiceProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text valueText;

	[HideInInspector]
	public AIAircraftSpawn unit;

	private AWACSVoiceProfile currentProfile;

	public GameObject testButtonObject;

	public override void SetInitialValue(object value)
	{
		currentProfile = (AWACSVoiceProfile)value;
		UpdateValueText();
	}

	private void UpdateValueText()
	{
		if (currentProfile != null)
		{
			valueText.text = currentProfile.name;
			testButtonObject.SetActive(value: true);
			return;
		}
		valueText.text = "Random";
		testButtonObject.SetActive(value: false);
		if (CommRadioManager.instance.commAudioSource.isPlaying)
		{
			CommRadioManager.instance.StopAllRadioMessages();
		}
	}

	public override object GetValue()
	{
		return currentProfile;
	}

	public void SelectProfileButton()
	{
		List<AWACSVoiceProfile> allAWACSVoices = VTResources.GetAllAWACSVoices();
		string[] array = new string[allAWACSVoices.Count + 1];
		object[] array2 = new object[array.Length];
		array[0] = "Random";
		array2[0] = null;
		int selected = 0;
		for (int i = 1; i < array.Length; i++)
		{
			array[i] = allAWACSVoices[i - 1].name;
			array2[i] = allAWACSVoices[i - 1];
			if (currentProfile != null && array[i] == currentProfile.name)
			{
				selected = i;
			}
		}
		editor.optionSelector.Display("Select Voice", array, array2, selected, OnSelectedVoice);
	}

	private void OnSelectedVoice(object selected)
	{
		if (selected != null)
		{
			currentProfile = (AWACSVoiceProfile)selected;
		}
		else
		{
			currentProfile = null;
		}
		UpdateValueText();
	}

	public void TestVoice()
	{
		if (currentProfile != null)
		{
			if (CommRadioManager.instance.commAudioSource.isPlaying)
			{
				CommRadioManager.instance.StopAllRadioMessages();
			}
			else
			{
				currentProfile.PlayRandomMessage();
			}
		}
	}
}
