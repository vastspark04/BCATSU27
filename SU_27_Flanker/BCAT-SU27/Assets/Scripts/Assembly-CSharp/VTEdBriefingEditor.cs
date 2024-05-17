using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdBriefingEditor : MonoBehaviour
{
	public class BriefingListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		public int idx;

		public VTEdBriefingEditor editor;

		public void OnPointerClick(PointerEventData e)
		{
			editor.SelectNote(idx);
		}
	}

	public VTScenarioEditor editor;

	public Button[] itemDependentButtons;

	public InputField textInputField;

	public RawImage noteImage;

	public Texture2D noImageTex;

	public Text audioText;

	public AudioSource testAudioSource;

	public GameObject noteDisplayObject;

	public ScrollRect listScroll;

	public GameObject listItemTemplate;

	public Transform selectionTf;

	public GameObject loadingAudioObject;

	private int selectedIdx;

	private List<ProtoBriefingNote> origNotes;

	private List<ProtoBriefingNote> modifiedNotes;

	private List<GameObject> listObjects = new List<GameObject>();

	private float lineHeight;

	private bool teamB;

	public void OpenEditor(bool teamB = false)
	{
		base.gameObject.SetActive(value: true);
		this.teamB = teamB;
		editor.BlockEditor(base.transform);
		editor.editorCamera.inputLock.AddLock("briefingEditor");
		lineHeight = ((RectTransform)listItemTemplate.transform).rect.height;
		listItemTemplate.SetActive(value: false);
		if ((!teamB && editor.currentScenario.briefingNotes == null) || (teamB && editor.currentScenario.briefingNotesB == null))
		{
			origNotes = new List<ProtoBriefingNote>();
		}
		else if (teamB)
		{
			origNotes = editor.currentScenario.briefingNotesB;
		}
		else
		{
			origNotes = editor.currentScenario.briefingNotes;
		}
		modifiedNotes = new List<ProtoBriefingNote>();
		foreach (ProtoBriefingNote origNote in origNotes)
		{
			ProtoBriefingNote protoBriefingNote = new ProtoBriefingNote();
			protoBriefingNote.imageDirty = origNote.imageDirty;
			protoBriefingNote.audioDirty = origNote.audioDirty;
			protoBriefingNote.imagePath = origNote.imagePath;
			protoBriefingNote.text = origNote.text;
			protoBriefingNote.audioClipPath = origNote.audioClipPath;
			modifiedNotes.Add(protoBriefingNote);
			if (protoBriefingNote.cachedImage == null && !string.IsNullOrEmpty(protoBriefingNote.imagePath))
			{
				if (protoBriefingNote.imageDirty)
				{
					protoBriefingNote.cachedImage = VTResources.GetTexture(protoBriefingNote.imagePath);
					continue;
				}
				string fullResourcePath = VTResources.GetCustomScenario(editor.currentScenario.scenarioID, editor.currentScenario.campaignID).GetFullResourcePath(protoBriefingNote.imagePath);
				protoBriefingNote.cachedImage = VTResources.GetTexture(fullResourcePath);
			}
		}
		UpdateListObjects();
		listScroll.verticalNormalizedPosition = 1f;
		if (origNotes.Count > 0)
		{
			SelectNote(0);
		}
		else
		{
			SelectNote(-1);
		}
		textInputField.onEndEdit.AddListener(OnEditedText);
	}

	private void UpdateListObjects()
	{
		foreach (GameObject listObject in listObjects)
		{
			Object.Destroy(listObject);
		}
		listObjects.Clear();
		for (int i = 0; i < modifiedNotes.Count; i++)
		{
			GameObject gameObject = Object.Instantiate(listItemTemplate, listScroll.content);
			gameObject.SetActive(value: true);
			gameObject.transform.localPosition = new Vector3(0f, (float)(-i) * lineHeight, 0f);
			BriefingListItem briefingListItem = gameObject.AddComponent<BriefingListItem>();
			briefingListItem.editor = this;
			briefingListItem.idx = i;
			gameObject.GetComponent<Text>().text = "Note " + (i + 1);
			listObjects.Add(gameObject);
		}
		listScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (float)modifiedNotes.Count * lineHeight);
	}

	private void Close()
	{
		editor.UnblockEditor(base.transform);
		editor.editorCamera.inputLock.RemoveLock("briefingEditor");
		textInputField.onEndEdit.RemoveListener(OnEditedText);
		base.gameObject.SetActive(value: false);
	}

	public void NewNoteButton()
	{
		ProtoBriefingNote item = new ProtoBriefingNote();
		modifiedNotes.Add(item);
		UpdateListObjects();
		SelectNote(modifiedNotes.Count - 1);
	}

	public void DeleteNoteButton()
	{
		modifiedNotes.RemoveAt(selectedIdx);
		UpdateListObjects();
		if (modifiedNotes.Count > 0)
		{
			SelectNote(Mathf.Min(selectedIdx, modifiedNotes.Count - 1));
		}
		else
		{
			SelectNote(-1);
		}
	}

	public void ShiftUpButton()
	{
		if (modifiedNotes.Count >= 2 && selectedIdx != 0)
		{
			ProtoBriefingNote value = modifiedNotes[selectedIdx - 1];
			modifiedNotes[selectedIdx - 1] = modifiedNotes[selectedIdx];
			modifiedNotes[selectedIdx] = value;
			SelectNote(selectedIdx - 1);
		}
	}

	public void ShiftDownButton()
	{
		if (modifiedNotes.Count >= 2 && selectedIdx != modifiedNotes.Count - 1)
		{
			ProtoBriefingNote value = modifiedNotes[selectedIdx + 1];
			modifiedNotes[selectedIdx + 1] = modifiedNotes[selectedIdx];
			modifiedNotes[selectedIdx] = value;
			SelectNote(selectedIdx + 1);
		}
	}

	public void SelectImageButton()
	{
		if (CheckIsSaved())
		{
			editor.resourceBrowser.OpenBrowser("Select Image", OnSelectedImage, VTResources.supportedImageExtensions);
		}
	}

	private void OnSelectedImage(string path)
	{
		modifiedNotes[selectedIdx].imagePath = path;
		Texture2D texture = VTResources.GetTexture(path);
		noteImage.texture = texture;
		modifiedNotes[selectedIdx].imageDirty = true;
		modifiedNotes[selectedIdx].cachedImage = texture;
	}

	public void SelectAudioButton()
	{
		if (CheckIsSaved())
		{
			editor.resourceBrowser.OpenBrowser("Select Audio", OnSelectedAudio, VTResources.supportedAudioExtensions);
		}
	}

	private void OnSelectedAudio(string path)
	{
		modifiedNotes[selectedIdx].audioClipPath = path;
		modifiedNotes[selectedIdx].cachedAudio = null;
		modifiedNotes[selectedIdx].audioDirty = true;
		audioText.text = Path.GetFileName(path);
	}

	private bool CheckIsSaved()
	{
		if (string.IsNullOrEmpty(editor.currentScenario.scenarioID))
		{
			editor.scenarioTitle = "untitled";
			editor.confirmDialogue.DisplayConfirmation("Save Required", "The scenario must be saved to a file in order to import resources.", editor.Save, null);
			return false;
		}
		return true;
	}

	public void TestAudioButton()
	{
		if (testAudioSource.isPlaying)
		{
			testAudioSource.Stop();
		}
		else
		{
			if (string.IsNullOrEmpty(modifiedNotes[selectedIdx].audioClipPath))
			{
				return;
			}
			if (modifiedNotes[selectedIdx].cachedAudio == null)
			{
				string path;
				if (modifiedNotes[selectedIdx].audioDirty)
				{
					path = modifiedNotes[selectedIdx].audioClipPath;
				}
				else
				{
					path = Path.GetDirectoryName(VTResources.GetCustomScenario(editor.currentScenario.scenarioID, editor.currentScenario.campaignID).filePath);
					path = Path.Combine(path, modifiedNotes[selectedIdx].audioClipPath);
				}
				StartCoroutine(TestAudioRoutine(path, modifiedNotes[selectedIdx]));
			}
			else if (modifiedNotes[selectedIdx].cachedAudio != null)
			{
				testAudioSource.loop = false;
				testAudioSource.clip = modifiedNotes[selectedIdx].cachedAudio;
				testAudioSource.Play();
			}
			else
			{
				Debug.Log("cached audio is null");
			}
		}
	}

	private IEnumerator TestAudioRoutine(string path, ProtoBriefingNote note)
	{
		loadingAudioObject.SetActive(value: true);
		editor.BlockEditor(loadingAudioObject.transform);
		AudioClip ac;
		if (Path.GetExtension(path).ToLower() == ".mp3")
		{
			VTResources.AsyncMp3ClipLoader mp3Loader = VTResources.LoadMP3Clip(path);
			while (!mp3Loader.clip)
			{
				yield return null;
			}
			Debug.LogFormat("Loaded mp3 to test with length: {0} and samples {1}", mp3Loader.clip.length, mp3Loader.clip.samples);
			ac = mp3Loader.clip;
			testAudioSource.PlayOneShot(ac);
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
			testAudioSource.PlayOneShot(ac);
		}
		editor.UnblockEditor(loadingAudioObject.transform);
		loadingAudioObject.SetActive(value: false);
		note.cachedAudio = ac;
	}

	public void OkayButton()
	{
		if (teamB)
		{
			editor.currentScenario.briefingNotesB = modifiedNotes;
		}
		else
		{
			editor.currentScenario.briefingNotes = modifiedNotes;
		}
		Close();
	}

	public void CancelButton()
	{
		Close();
	}

	public void SelectNote(int idx)
	{
		if (idx >= 0)
		{
			selectedIdx = idx;
			selectionTf.gameObject.SetActive(value: true);
			selectionTf.localPosition = new Vector3(0f, (float)(-idx) * lineHeight, 0f);
			itemDependentButtons.SetInteractable(interactable: true);
			DisplayNote(modifiedNotes[idx]);
		}
		else
		{
			selectedIdx = -1;
			selectionTf.gameObject.SetActive(value: false);
			itemDependentButtons.SetInteractable(interactable: false);
			noteDisplayObject.SetActive(value: false);
		}
	}

	private void DisplayNote(ProtoBriefingNote note)
	{
		noteDisplayObject.SetActive(value: true);
		textInputField.text = note.text;
		if (note.cachedImage != null)
		{
			noteImage.texture = note.cachedImage;
		}
		else
		{
			noteImage.texture = noImageTex;
		}
		if (!string.IsNullOrEmpty(note.audioClipPath))
		{
			audioText.text = Path.GetFileName(note.audioClipPath);
		}
		else
		{
			audioText.text = "No audio.";
		}
	}

	private void OnEditedText(string s)
	{
		s = ConfigNodeUtils.SanitizeInputString(s);
		modifiedNotes[selectedIdx].text = s;
	}
}
