using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class VTAudioRefProperty : VTPropertyField
{
	public enum FieldTypes
	{
		Radio,
		CopilotRadio,
		BGM
	}

	public VTScenarioEditor editor;

	public Text currValText;

	private VTSAudioReference currentValue;

	public UIToolTipRect tooltip;

	private string tooltipDefaultText;

	public FieldTypes fieldType;

	private static bool isPreviewingBgm;

	private AudioClip cachedAudio;

	private static VTScenario.ScenarioSystemActions.VTSRadioMessagePlayer.MP3ClipStreamer mp3ClipStreamer;

	private void Awake()
	{
		if ((bool)tooltip)
		{
			tooltipDefaultText = tooltip.text;
		}
		editor.OnBeforeSave += Editor_OnBeforeSave;
	}

	private void Editor_OnBeforeSave()
	{
		StopAndUnloadMusic();
	}

	private void OnDestroy()
	{
		if ((bool)editor)
		{
			editor.OnBeforeSave -= Editor_OnBeforeSave;
		}
		StopAndUnloadMusic();
	}

	private void OnDisable()
	{
		StopAndUnloadMusic();
	}

	private void StopAndUnloadMusic()
	{
		if (isPreviewingBgm)
		{
			BGMManager.Stop();
			isPreviewingBgm = false;
		}
		if ((bool)cachedAudio)
		{
			Debug.Log("Unloading cached audio: " + cachedAudio.name);
			cachedAudio.UnloadAudioData();
			cachedAudio = null;
		}
		if (mp3ClipStreamer != null)
		{
			mp3ClipStreamer.Dispose();
			mp3ClipStreamer = null;
		}
	}

	public override void SetInitialValue(object value)
	{
		currentValue = (VTSAudioReference)value;
		UpdateText();
	}

	public override object GetValue()
	{
		return currentValue;
	}

	public void SelectButton()
	{
		if (CommRadioManager.instance.commAudioSource.isPlaying)
		{
			CommRadioManager.instance.StopAllRadioMessages();
		}
		else if (isPreviewingBgm)
		{
			BGMManager.Stop();
			isPreviewingBgm = false;
			if (mp3ClipStreamer != null)
			{
				mp3ClipStreamer.Rewind();
			}
		}
		if (string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			editor.confirmDialogue.DisplayConfirmation("Save Required", "The scenario must be saved to a file before importing audio.", editor.Save, null);
		}
		else
		{
			editor.resourceBrowser.OpenBrowser("Select Audio", OnSelectedAudio, VTResources.supportedAudioExtensions);
		}
	}

	private void OnSelectedAudio(string audioPath)
	{
		if (currentValue == null)
		{
			currentValue = new VTSAudioReference();
		}
		if (mp3ClipStreamer != null)
		{
			mp3ClipStreamer.Dispose();
			mp3ClipStreamer = null;
		}
		currentValue.audioPath = audioPath;
		currentValue.audioResourceDirty = true;
		cachedAudio = null;
		UpdateText();
		ValueChanged();
	}

	private void UpdateText()
	{
		if (currentValue == null || string.IsNullOrEmpty(currentValue.audioPath))
		{
			currValText.text = "None";
			return;
		}
		string fileName = Path.GetFileName(currentValue.audioPath);
		currValText.text = fileName;
	}

	public void TestAudioButton()
	{
		if (CommRadioManager.instance.commAudioSource.isPlaying)
		{
			CommRadioManager.instance.StopAllRadioMessages();
		}
		else if (isPreviewingBgm)
		{
			BGMManager.Stop();
			isPreviewingBgm = false;
		}
		else
		{
			if (currentValue == null || string.IsNullOrEmpty(currentValue.audioPath))
			{
				return;
			}
			if (fieldType == FieldTypes.BGM)
			{
				isPreviewingBgm = true;
			}
			if (cachedAudio == null)
			{
				string path;
				if (currentValue.audioResourceDirty)
				{
					path = currentValue.audioPath;
				}
				else
				{
					path = Path.GetDirectoryName(VTResources.GetCustomScenario(editor.currentScenario.scenarioID, editor.currentScenario.campaignID).filePath);
					path = Path.Combine(path, currentValue.audioPath);
				}
				StartCoroutine(TestAudioRoutine(path));
			}
			else if (cachedAudio != null)
			{
				if (fieldType == FieldTypes.BGM)
				{
					BGMManager.FadeTo(cachedAudio, 1f, loop: false);
				}
				else if (fieldType == FieldTypes.CopilotRadio)
				{
					CommRadioManager.instance.PlayCopilotMessage(cachedAudio);
				}
				else
				{
					CommRadioManager.instance.PlayMessage(cachedAudio);
				}
			}
			else
			{
				Debug.Log("cached audio is null");
			}
		}
	}

	private IEnumerator TestAudioRoutine(string path)
	{
		editor.BlockEditor(editor.editorBlocker);
		if (mp3ClipStreamer != null)
		{
			mp3ClipStreamer.Dispose();
			mp3ClipStreamer = null;
		}
		AudioClip ac;
		if (path.ToLower().EndsWith("mp3"))
		{
			mp3ClipStreamer = new VTScenario.ScenarioSystemActions.VTSRadioMessagePlayer.MP3ClipStreamer(path);
			ac = (cachedAudio = mp3ClipStreamer.audioClip);
			Debug.Log("TestAudioRoutine attempting to create audioClip from mp3 stream.");
		}
		else
		{
			WWW www = new WWW("file://" + path);
			while (!www.isDone)
			{
				yield return www;
			}
			ac = www.GetAudioClip();
			while (ac.loadState != AudioDataLoadState.Loaded)
			{
				yield return null;
			}
			cachedAudio = ac;
			if ((bool)tooltip)
			{
				tooltip.text = string.Format("{0}({1}s)", tooltipDefaultText, ac.length.ToString("0.00"));
			}
		}
		if (fieldType == FieldTypes.BGM)
		{
			BGMManager.FadeTo(ac, 0.5f, loop: false);
		}
		else if (fieldType == FieldTypes.CopilotRadio)
		{
			CommRadioManager.instance.PlayCopilotMessage(ac);
		}
		else
		{
			CommRadioManager.instance.PlayMessage(ac);
		}
		editor.UnblockEditor(editor.editorBlocker);
	}
}
