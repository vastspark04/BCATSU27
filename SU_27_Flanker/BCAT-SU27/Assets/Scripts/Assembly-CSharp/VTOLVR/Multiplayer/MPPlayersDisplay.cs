using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPPlayersDisplay : MonoBehaviour
{
	public GameObject playerNameTemplate;

	public Transform teamANamePos;

	public Transform teamBNamePos;

	private List<GameObject> nameObjs = new List<GameObject>();

	public ScrollRect teamAScroll;

	public ScrollRect teamBScroll;

	public float scrollRate = 20f;

	private void Awake()
	{
		playerNameTemplate.SetActive(value: false);
	}

	private void OnEnable()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam += Instance_OnPlayerSelectedTeam;
			VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated += OnPlayerListUpdated;
			UpdateDisplay();
		}
		else
		{
			base.enabled = false;
		}
	}

	private void OnPlayerListUpdated()
	{
		UpdateDisplay();
	}

	private void OnDisable()
	{
		if ((bool)VTOLMPSceneManager.instance)
		{
			VTOLMPSceneManager.instance.OnPlayerSelectedTeam -= Instance_OnPlayerSelectedTeam;
		}
		if ((bool)VTOLMPLobbyManager.instance)
		{
			VTOLMPLobbyManager.instance.OnConnectedPlayerListUpdated -= OnPlayerListUpdated;
		}
	}

	private void Instance_OnPlayerSelectedTeam(PlayerInfo obj)
	{
		UpdateDisplay();
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
				PlayerNameAndPing component = gameObject.GetComponent<PlayerNameAndPing>();
				component.nameText.text = connectedPlayer.pilotName;
				component.userId = connectedPlayer.steamUser.Id;
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
				UIMaskedTextScroller component2 = gameObject.GetComponent<UIMaskedTextScroller>();
				if ((bool)component2)
				{
					component2.Refresh();
				}
				nameObjs.Add(gameObject);
			}
		}
		if ((bool)teamAScroll)
		{
			teamAScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3 * (float)num);
		}
		if ((bool)teamBScroll)
		{
			teamBScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num3 * (float)num2);
		}
	}

	public void ScrollUp()
	{
		teamAScroll.verticalNormalizedPosition += scrollRate * Time.deltaTime;
		teamBScroll.verticalNormalizedPosition += scrollRate * Time.deltaTime;
	}

	public void ScrollDown()
	{
		teamAScroll.verticalNormalizedPosition -= scrollRate * Time.deltaTime;
		teamBScroll.verticalNormalizedPosition -= scrollRate * Time.deltaTime;
	}
}

}