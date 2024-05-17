using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class MissionBriefingUI : MonoBehaviour
{
	public Image fadeInImage;

	public Text missionBriefingText;

	public Transform missionBriefingTarget;

	public Image imageBg;

	public Transform imageBgTarget;

	public RawImage briefingImage;

	public Text briefingNotes;

	public Image briefingNotesBg;

	public Text missionNameText;

	public Text flyButtonText;

	public GameObject nextPrevNoteButtons;

	public AudioSource briefingNotesAudioSource;

	public Text playerDesignationText;

	public Transform playerDesignationTarget;

	public bool playBGM = true;

	private CampaignScenario cs;

	private int noteIdx;

	private int maxLength;

	private List<Coroutine> noteRoutines = new List<Coroutine>();

	public event Action<int> OnControllerSetNote;

	public void FlyButton()
	{
		StartCoroutine(FlyButtonRoutine());
	}

	private IEnumerator FlyButtonRoutine()
	{
		ControllerEventHandler.PauseEvents();
		ScreenFader.FadeOut(Color.black, 0.85f);
		yield return new WaitForSeconds(1f);
		if (cs.equipConfigurable)
		{
			LoadingSceneController.LoadSceneImmediate("VehicleConfiguration");
		}
		else
		{
			BGMManager.FadeOut();
			Loadout loadout = new Loadout();
			loadout.normalizedFuel = PilotSaveManager.currentScenario.forcedFuel;
			loadout.hpLoadout = new string[PilotSaveManager.currentVehicle.hardpointCount];
			loadout.cmLoadout = new int[2] { 99999, 99999 };
			if (PilotSaveManager.currentScenario.forcedEquips != null)
			{
				CampaignScenario.ForcedEquip[] forcedEquips = PilotSaveManager.currentScenario.forcedEquips;
				foreach (CampaignScenario.ForcedEquip forcedEquip in forcedEquips)
				{
					loadout.hpLoadout[forcedEquip.hardpointIdx] = forcedEquip.weaponName;
				}
			}
			VehicleEquipper.loadout = loadout;
			if (PilotSaveManager.currentCampaign.isCustomScenarios)
			{
				VTScenario.LaunchScenario(VTResources.GetScenario(PilotSaveManager.currentScenario.scenarioID, PilotSaveManager.currentCampaign));
			}
			else
			{
				LoadingSceneController.LoadScene(PilotSaveManager.currentScenario.mapSceneName);
			}
		}
		ControllerEventHandler.UnpauseEvents();
	}

	public void InitializeMission(CampaignScenario cs, bool teamB = false)
	{
		this.cs = cs;
		Debug.Log("MissionBriefingUI: Initializing mission: " + cs.scenarioID);
		if (cs.customScenarioInfo != null)
		{
			VTScenarioInfo customScenarioInfo = cs.customScenarioInfo;
			if (VTOLMPUtils.IsMultiplayer() || cs.briefingNotes == null || cs.briefingNotes.Length == 0)
			{
				Debug.Log("MissionBriefingUI: Loading briefing notes from custom scenario config. (Team " + (teamB ? "B" : "A") + ")");
				cs.briefingNotes = ProtoBriefingNote.GetBriefingFromConfig(customScenarioInfo, teamB);
			}
			else
			{
				Debug.Log("MissionBriefingUI: Custom scenario config already had loaded briefing notes.");
			}
		}
		nextPrevNoteButtons.SetActive(value: false);
		maxLength = cs.briefingNotes.Length;
		missionNameText.text = cs.scenarioName;
		briefingNotes.enabled = false;
		briefingNotesBg.enabled = false;
		missionNameText.enabled = false;
		if ((bool)playerDesignationText)
		{
			if (cs.customScenarioInfo != null)
			{
				playerDesignationText.text = cs.customScenarioInfo.GetPlayerDesignation().ToString();
				playerDesignationText.gameObject.SetActive(value: true);
			}
			else
			{
				playerDesignationText.gameObject.SetActive(value: false);
			}
		}
		if ((bool)flyButtonText)
		{
			if (cs.equipConfigurable)
			{
				flyButtonText.text = VTLStaticStrings.briefingUI_configure;
			}
			else
			{
				flyButtonText.text = VTLStaticStrings.briefingUI_fly;
			}
		}
		VRPointInteractableCanvas componentInParent = GetComponentInParent<VRPointInteractableCanvas>();
		if ((bool)componentInParent)
		{
			componentInParent.RefreshInteractables();
		}
		StartCoroutine(InitializeRoutine());
	}

	private IEnumerator InitializeRoutine()
	{
		StartCoroutine(FadeInRoutine());
		StartCoroutine(IntroRoutine());
		yield return new WaitForSeconds(1.5f);
		StartCoroutine(FadeNameRoutine());
		StartCoroutine(IntroImageRoutine());
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(NoteIntroRoutine());
		nextPrevNoteButtons.SetActive(value: true);
		ShowNote(0);
	}

	private IEnumerator IntroRoutine()
	{
		Vector3 scale = missionBriefingText.transform.localScale;
		Vector3 pDesScale = (playerDesignationText ? playerDesignationText.transform.localScale : Vector3.one);
		Vector3 startScale = 0.1f * Vector3.one;
		missionBriefingText.transform.localScale = startScale;
		if ((bool)playerDesignationText)
		{
			playerDesignationText.transform.localScale = startScale;
		}
		float t2 = 0f;
		while (t2 < 0.9999f)
		{
			yield return null;
			t2 = Mathf.Lerp(t2, 1f, 8f * Time.deltaTime);
			missionBriefingText.transform.localScale = Vector3.Lerp(startScale, scale, t2);
			if ((bool)playerDesignationText)
			{
				playerDesignationText.transform.localScale = Vector3.Lerp(startScale, pDesScale, t2 * t2);
			}
		}
		Vector3 targetPos = missionBriefingTarget.transform.localPosition;
		Vector3 startPos = missionBriefingText.transform.localPosition;
		Vector3 pDesStartPos = (playerDesignationText ? playerDesignationText.transform.localPosition : Vector3.zero);
		t2 = 0f;
		while (t2 < 0.9999f)
		{
			yield return null;
			t2 = Mathf.Lerp(t2, 1f, 5f * Time.deltaTime);
			missionBriefingText.transform.localPosition = Vector3.Lerp(startPos, targetPos, t2);
			if ((bool)playerDesignationText)
			{
				playerDesignationText.transform.localPosition = Vector3.Lerp(pDesStartPos, playerDesignationTarget.localPosition, t2 * t2);
			}
		}
	}

	private IEnumerator FadeInRoutine()
	{
		float t = 0f;
		while (t < 1f)
		{
			yield return null;
			t = Mathf.MoveTowards(t, 1f, 3f * Time.deltaTime);
			fadeInImage.color = Color.Lerp(Color.black, Color.clear, t);
		}
	}

	private IEnumerator IntroImageRoutine()
	{
		float t = 0f;
		Color startColor = new Color(0f, 0f, 0f, 0f);
		Color targetColor = new Color(0f, 0f, 0f, 1f);
		Vector3 startPos = imageBg.transform.localPosition;
		Vector3 targetPos = imageBgTarget.transform.localPosition;
		Vector3 startScale = imageBg.transform.localScale;
		Vector3 targetScale = imageBgTarget.transform.localScale;
		while (t < 1f)
		{
			yield return null;
			t = Mathf.Lerp(t, 1f, 5f * Time.deltaTime);
			imageBg.color = Color.Lerp(startColor, targetColor, t);
			imageBg.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
			imageBg.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
		}
	}

	private IEnumerator FadeNameRoutine()
	{
		missionNameText.enabled = true;
		Color tgtColor = missionNameText.color;
		missionNameText.color = Color.clear;
		float t = 0f;
		while (t < 1f)
		{
			yield return null;
			t = Mathf.MoveTowards(t, 1f, 0.5f * Time.deltaTime);
			missionNameText.color = Color.Lerp(Color.clear, tgtColor, t);
		}
	}

	private IEnumerator NoteIntroRoutine()
	{
		briefingNotes.enabled = false;
		briefingNotes.text = string.Empty;
		briefingNotesBg.enabled = true;
		Vector3 startScale = new Vector3(1f, 0.001f, 1f);
		float t = 0f;
		Color startColor = new Color(0f, 0f, 0f, 0f);
		Color targetColor = new Color(0f, 0f, 0f, 1f);
		while (t < 0.999f)
		{
			yield return null;
			t = Mathf.Lerp(t, 1f, 8f * Time.deltaTime);
			briefingNotesBg.color = Color.Lerp(startColor, targetColor, t);
			briefingNotesBg.transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
		}
		briefingNotes.enabled = true;
	}

	public void NextNote()
	{
		if (maxLength != 0)
		{
			noteIdx = (noteIdx + 1) % maxLength;
			this.OnControllerSetNote?.Invoke(noteIdx);
			ShowNote(noteIdx);
		}
	}

	public void PrevNote()
	{
		if (maxLength != 0)
		{
			noteIdx--;
			if (noteIdx < 0)
			{
				noteIdx = maxLength - 1;
			}
			this.OnControllerSetNote?.Invoke(noteIdx);
			ShowNote(noteIdx);
		}
	}

	public void RemoteSetNote(int idx)
	{
		if (idx != noteIdx && idx >= 0 && idx < maxLength)
		{
			noteIdx = idx;
			ShowNote(noteIdx);
		}
	}

	private void ShowNote(int idx)
	{
		foreach (Coroutine noteRoutine in noteRoutines)
		{
			if (noteRoutine != null)
			{
				StopCoroutine(noteRoutine);
			}
		}
		noteRoutines = new List<Coroutine>();
		if (idx >= cs.briefingNotes.Length)
		{
			return;
		}
		CampaignScenario.BriefingNote briefingNote = cs.briefingNotes[idx];
		noteRoutines.Add(StartCoroutine(TypeNoteRoutine(briefingNote.note + $"\n\n({idx + 1}/{cs.briefingNotes.Length})")));
		noteRoutines.Add(StartCoroutine(ChangeImageRoutine(briefingNote.image)));
		if ((bool)briefingNotesAudioSource)
		{
			if ((bool)briefingNote.sound)
			{
				BGMManager.FadeOut();
				noteRoutines.Add(StartCoroutine(ChangeAudioRoutine(briefingNote.sound)));
			}
			else
			{
				noteRoutines.Add(StartCoroutine(WaitForSoundRoutine(briefingNote)));
			}
		}
	}

	private IEnumerator WaitForSoundRoutine(CampaignScenario.BriefingNote bn)
	{
		if (playBGM)
		{
			BGMManager.FadeIn();
		}
		Coroutine coroutine = StartCoroutine(ChangeAudioRoutine(null));
		noteRoutines.Add(coroutine);
		yield return coroutine;
		while (!bn.sound)
		{
			yield return null;
		}
		if (playBGM)
		{
			BGMManager.FadeOut();
		}
		noteRoutines.Add(StartCoroutine(ChangeAudioRoutine(bn.sound)));
	}

	private IEnumerator TypeNoteRoutine(string note)
	{
		StringBuilder sb = new StringBuilder();
		briefingNotes.text = string.Empty;
		for (int i = 0; i < note.Length; i++)
		{
			sb.Append(note[i]);
			briefingNotes.text = sb.ToString();
			yield return null;
		}
	}

	private IEnumerator ChangeImageRoutine(Texture2D image)
	{
		float t = briefingImage.color.r;
		while (t > 0f)
		{
			yield return null;
			t = Mathf.MoveTowards(t, 0f, 2f * Time.deltaTime);
			briefingImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0f), Color.white, t);
		}
		if (image != null)
		{
			briefingImage.texture = image;
			while (t < 1f)
			{
				yield return null;
				t = Mathf.MoveTowards(t, 1f, 2f * Time.deltaTime);
				briefingImage.color = Color.Lerp(new Color(0f, 0f, 0f, 0f), Color.white, t);
			}
		}
	}

	private IEnumerator ChangeAudioRoutine(AudioClip clip)
	{
		if (briefingNotesAudioSource.isPlaying)
		{
			float t = briefingNotesAudioSource.volume;
			while (t > 0f)
			{
				yield return null;
				t = Mathf.MoveTowards(t, 0f, 2f * Time.deltaTime);
				briefingNotesAudioSource.volume = t;
			}
			briefingNotesAudioSource.Stop();
		}
		yield return new WaitForSeconds(1f);
		if (clip != null)
		{
			briefingNotesAudioSource.clip = clip;
			briefingNotesAudioSource.volume = 1f;
			briefingNotesAudioSource.Play();
		}
	}
}
