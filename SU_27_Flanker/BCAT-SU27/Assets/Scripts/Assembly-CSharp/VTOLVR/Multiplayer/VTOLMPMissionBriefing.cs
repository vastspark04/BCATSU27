using System.Collections;
using UnityEngine;

namespace VTOLVR.Multiplayer{

public class VTOLMPMissionBriefing : MonoBehaviour
{
	public GameObject briefingTemplate;

	public GameObject briefingDisplayObject;

	public GameObject briefingControllerOnlyObjects;

	private MissionBriefingUI briefingUI;

	private bool displayingUI;

	private bool bothTeams;

	private void Awake()
	{
		briefingTemplate.SetActive(value: false);
		briefingDisplayObject.SetActive(value: false);
	}

	public void Setup(bool bothTeams)
	{
		this.bothTeams = bothTeams;
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		yield return null;
		while (!VTOLMPBriefingManager.instance)
		{
			yield return null;
		}
		while (!VTOLMPLobbyManager.localPlayerInfo.chosenTeam)
		{
			yield return null;
		}
		if (VTOLMPLobbyManager.instance.GetLobbyGameState() == VTOLMPLobbyManager.GameStates.Briefing)
		{
			briefingDisplayObject.SetActive(value: true);
			GameObject gameObject = briefingTemplate;
			gameObject.SetActive(value: true);
			briefingUI = gameObject.GetComponent<MissionBriefingUI>();
			Debug.Log($"Scenario separateBriefings = {VTScenario.current.separateBriefings}, local team is {VTOLMPLobbyManager.localPlayerInfo.team}");
			briefingUI.InitializeMission(PilotSaveManager.currentScenario, VTScenario.current.separateBriefings && VTOLMPLobbyManager.localPlayerInfo.team == Teams.Enemy);
			displayingUI = true;
			briefingUI.OnControllerSetNote += BriefingUI_OnControllerSetNote;
			VTOLMPLobbyManager.OnLobbyDataChanged += VTOLMPLobbyManager_OnLobbyDataChanged;
			VTOLMPBriefingManager.instance.OnSetBriefingController += Instance_OnSetBriefingController;
			VTOLMPBriefingManager.instance.OnSetBriefingNote += Instance_OnSetBriefingNote;
			VTOLMPSceneManager.instance.OnMPScenarioStart += Instance_OnMPScenarioStart;
			briefingControllerOnlyObjects.SetActive(VTOLMPBriefingManager.instance.LocalPlayerIsBriefingController());
		}
	}

	private void Instance_OnMPScenarioStart()
	{
		VTOLMPBriefingManager.instance.ReleaseControl();
		briefingDisplayObject.SetActive(value: false);
	}

	private void BriefingUI_OnControllerSetNote(int noteIdx)
	{
		VTOLMPBriefingManager.instance.SetBriefingNote(noteIdx);
	}

	private void VTOLMPLobbyManager_OnLobbyDataChanged()
	{
		if (displayingUI && VTOLMPLobbyManager.instance.GetLobbyGameState() != 0)
		{
			briefingDisplayObject.SetActive(value: false);
			displayingUI = false;
			VTOLMPLobbyManager.OnLobbyDataChanged -= VTOLMPLobbyManager_OnLobbyDataChanged;
			VTOLMPBriefingManager.instance.OnSetBriefingController -= Instance_OnSetBriefingController;
			VTOLMPBriefingManager.instance.OnSetBriefingNote -= Instance_OnSetBriefingNote;
		}
	}

	private void OnDestroy()
	{
		VTOLMPLobbyManager.OnLobbyDataChanged -= VTOLMPLobbyManager_OnLobbyDataChanged;
		if ((bool)VTOLMPBriefingManager.instance)
		{
			VTOLMPBriefingManager.instance.OnSetBriefingController -= Instance_OnSetBriefingController;
			VTOLMPBriefingManager.instance.OnSetBriefingNote -= Instance_OnSetBriefingNote;
		}
	}

	private void Instance_OnSetBriefingNote(int idx, Teams team)
	{
		if ((bool)briefingUI && (team == VTOLMPLobbyManager.localPlayerInfo.team || bothTeams))
		{
			briefingUI.RemoteSetNote(idx);
		}
	}

	private void Instance_OnSetBriefingController(ulong userId, Teams team)
	{
		if (team == VTOLMPLobbyManager.localPlayerInfo.team || bothTeams)
		{
			briefingControllerOnlyObjects.SetActive(userId == BDSteamClient.mySteamID);
		}
	}
}

}