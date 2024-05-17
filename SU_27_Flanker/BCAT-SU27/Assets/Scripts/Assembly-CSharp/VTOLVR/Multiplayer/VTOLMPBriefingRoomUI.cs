using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class VTOLMPBriefingRoomUI : MonoBehaviour
{
	public VTOLMPBriefingRoom briefingRoom;

	[Header("UI")]
	public Transform configUIParent;

	public GameObject slotsMenuDisplayObj;

	public ScrollRect slotsScrollRect;

	public GameObject slotTemplate;

	public VTConfirmationDialogue hostStartConfirmDialogue;

	public GameObject equipConfigButton;

	public GameObject hostStartButton;

	public GameObject enterVehicleButton;

	public GameObject clientReadyObject;

	public GameObject clientReadyIndicator;

	public GameObject selectASlotObj;

	public GameObject scenarioStartedObj;

	public GameObject missionFinishedObj;

	public GameObject teamAWinObj;

	public GameObject teamBWinObj;

	public GameObject otherTeamInfoObj;

	public GameObject otherTeamReady;

	public GameObject otherTeamNotReady;

	public GameObject voiceToggleObj;

	public GameObject briefingControlButton;

	[Header("New Game")]
	public GameObject skipMissionButton;

	public GameObject hostNewGameButton;

	public GameObject hostNewGameWindow;

	public MPMissionBrowser missionBrowser;

	public Text lobbyNameText;

	public Text missionNameText;

	public Text missionDescriptionText;

	public RawImage missionImage;

	public RawImage mapImage;

	public VTMPScenarioSettings scenarioSettings;

	public VRKeyboard keyboard;

	public Campaign swStandaloneCampaign;

	private VTScenarioInfo selectedNewMission;

	public GameObject newMissionPlayerCountWarning;

	public void EnterVehicle()
	{
		briefingRoom.EnterVehicle();
	}

	public void ToggleClientReady()
	{
		briefingRoom.ToggleClientReady();
	}

	public void Host_BeginScenario()
	{
		briefingRoom.Host_BeginScenario();
	}

	public void QuitButton()
	{
		briefingRoom.QuitButton();
	}

	public void OpenEquipConfig()
	{
		briefingRoom.OpenEquipConfig();
	}

	public void CloseEquipConfig()
	{
		briefingRoom.CloseEquipConfig();
	}

	public void HostNewGameButton()
	{
		briefingRoom.HostNewGameButton();
	}

	public void NG_BackButton()
	{
		briefingRoom.NG_BackButton();
	}

	public void NG_StartButton()
	{
		briefingRoom.NG_StartButton();
	}

	public void NG_SetLobbyNameButton()
	{
		briefingRoom.NG_SetLobbyNameButton();
	}

	public void NG_SelectMissionButton()
	{
		briefingRoom.NG_SelectMissionButton();
	}

	public void RequestBriefingControlButton()
	{
		briefingRoom.RequestBriefingControlButton();
	}

	public void SkipMissionButton()
	{
		briefingRoom.SkipMissionButton();
	}
}

}