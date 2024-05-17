using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTOLMPTeamSelectUI : MonoBehaviour
{
	public GameObject playerNameTemplate;

	public Transform teamANamePos;

	public Transform teamBNamePos;

	public GameObject requestingObj;

	public GameObject connectingObject;

	public GameObject teamSelectObject;

	private bool requestingTeam;

	private List<GameObject> nameObjs = new List<GameObject>();

	private void Awake()
	{
		playerNameTemplate.SetActive(value: false);
	}

	private void OnEnable()
	{
		VTOLMPSceneManager.instance.OnPlayerSelectedTeam += Instance_OnPlayerSelectedTeam;
		StartCoroutine(StartRoutine());
	}

	private IEnumerator StartRoutine()
	{
		ScreenFader.FadeIn();
		teamSelectObject.SetActive(value: false);
		connectingObject.SetActive(value: true);
		while (VTNetworkManager.instance.connectionState != VTNetworkManager.ConnectionStates.Connected)
		{
			yield return null;
		}
		teamSelectObject.SetActive(value: true);
		connectingObject.SetActive(value: false);
		UpdateDisplay();
	}

	private void OnDisable()
	{
		VTOLMPSceneManager.instance.OnPlayerSelectedTeam -= Instance_OnPlayerSelectedTeam;
	}

	private void Instance_OnPlayerSelectedTeam(PlayerInfo obj)
	{
		UpdateDisplay();
	}

	public void SelectTeamA()
	{
		StartCoroutine(SelectTeamRoutine(Teams.Allied));
	}

	public void SelectTeamB()
	{
		StartCoroutine(SelectTeamRoutine(Teams.Enemy));
	}

	private IEnumerator SelectTeamRoutine(Teams team)
	{
		if (requestingTeam)
		{
			yield break;
		}
		Debug.Log($"We requested team {team}");
		requestingObj.SetActive(value: true);
		requestingTeam = true;
		VTOLMPSceneManager.TeamRequest req = VTOLMPSceneManager.instance.RequestTeam(team);
		while (!req.isReady)
		{
			yield return null;
		}
		if (req.accepted)
		{
			Debug.Log(" - Team request accepted");
			VTOLMPSceneManager.instance.localPlayer.team = team;
			VTOLMPSceneManager.instance.localPlayer.chosenTeam = true;
			VTOLMPSceneManager.instance.BeginVoiceChat();
			ScreenFader.FadeOut();
			yield return new WaitForSeconds(1.2f);
			VTOLMPBriefingRoom.instance.SetupForTeam();
			VTOLMPSceneManager.instance.SpawnBriefingAvatar(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		}
		else
		{
			Debug.Log(" - Team request Denied!");
			requestingObj.SetActive(value: false);
		}
		requestingTeam = false;
	}

	public void LeaveButton()
	{
		Debug.Log(" - Leaving MP game from TeamSelectUI");
		StartCoroutine(LeaveRoutine());
	}

	private IEnumerator LeaveRoutine()
	{
		ScreenFader.FadeOut();
		yield return new WaitForSeconds(1.2f);
		VTOLMPSceneManager.instance.DisconnectToMainMenu();
	}

	private void UpdateDisplay()
	{
		foreach (GameObject nameObj in nameObjs)
		{
			Object.Destroy(nameObj);
		}
		nameObjs.Clear();
		int num = 0;
		int num2 = 0;
		float num3 = ((RectTransform)playerNameTemplate.transform).rect.height * playerNameTemplate.transform.localScale.y;
		foreach (PlayerInfo connectedPlayer in VTOLMPLobbyManager.instance.connectedPlayers)
		{
			if (connectedPlayer.chosenTeam)
			{
				GameObject gameObject = Object.Instantiate(playerNameTemplate, (connectedPlayer.team == Teams.Allied) ? teamANamePos : teamBNamePos);
				gameObject.SetActive(value: true);
				gameObject.GetComponent<Text>().text = connectedPlayer.pilotName;
				if (connectedPlayer.team == Teams.Allied)
				{
					gameObject.transform.localPosition = new Vector3(0f, (float)(-num) * num3, 0f);
					num++;
				}
				else
				{
					gameObject.transform.localPosition = new Vector3(0f, (float)(-num2) * num3, 0f);
					num2++;
				}
				nameObjs.Add(gameObject);
			}
		}
	}
}

}