using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using VTNetworking;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94PilotInserter : MonoBehaviour
{
	public BlackoutEffect blackoutEffect;

	public HelmetController helmetController;

	public Transform bobbleCollider1;

	public Transform bobbleCollider2;

	public IKTwo leftLegIK;

	public IKTwo rightLegIK;

	public Transform joystickFrameOfReference;

	public AudioListener playerAudioListener;

	public UnityEvent OnBeginRearm;

	public UnityEvent OnEndRearm;

	public VRInteractable neckPTTInteractable;

	public ScreenMaskedColorRamp playerNvg;

	public FloatingOriginShifter originShifter;

	public void ConnectLocal(AH94PilotReceiver receiver)
	{
		blackoutEffect.flightInfo = receiver.flightInfo;
		blackoutEffect.rb = blackoutEffect.flightInfo.rb;
		blackoutEffect.enabled = true;
		helmetController.hudMaskToggler = receiver.hudMaskToggler;
		helmetController.battery = receiver.battery;
		helmetController.hudPowerObject = receiver.hudPowerObject;
		helmetController.enabled = true;
		helmetController.RefreshHMCSUpdate();
		if ((bool)receiver.bobbleHead)
		{
			receiver.bobbleHead.handColliders[0].transform = bobbleCollider1;
			receiver.bobbleHead.handColliders[1].transform = bobbleCollider2;
		}
		leftLegIK.targetTransform = receiver.leftFootTarget;
		rightLegIK.targetTransform = receiver.rightFootTarget;
		VRJoystick[] joysticks = receiver.joysticks;
		for (int i = 0; i < joysticks.Length; i++)
		{
			joysticks[i].frameOfReference = joystickFrameOfReference;
		}
		receiver.tgpPage.SetHelmet(helmetController);
		receiver.sCam.playerAudioListener = playerAudioListener;
		receiver.sCam.SetPlayerNVG(playerNvg);
		if ((bool)receiver.hmdSwitch)
		{
			receiver.hmdSwitch.OnSetState.AddListener(helmetController.SetPower);
			helmetController.SetPower(receiver.hmdSwitch.currentState);
		}
		PlayerVehicleSetup component = receiver.flightInfo.GetComponent<PlayerVehicleSetup>();
		component.OnBeginRearming.AddListener(BeginRearm);
		component.OnEndRearming.AddListener(EndRearm);
		IPilotReceiverHandler[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<IPilotReceiverHandler>(includeInactive: true);
		for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
		{
			componentsInChildrenImplementing[i].OnPilotReceiver(receiver);
		}
		VTNetworkVoicePTT[] ptt = receiver.ptt;
		foreach (VTNetworkVoicePTT vTNetworkVoicePTT in ptt)
		{
			if ((bool)vTNetworkVoicePTT)
			{
				neckPTTInteractable.OnInteract.AddListener(vTNetworkVoicePTT.StartVoice);
				neckPTTInteractable.OnStopInteract.AddListener(vTNetworkVoicePTT.StopVoice);
			}
		}
		originShifter.rb = receiver.flightInfo.rb;
		ControllerEventHandler.UnpauseEvents();
		ScreenFader.FadeIn();
	}

	private void BeginRearm()
	{
		OnBeginRearm?.Invoke();
	}

	private void EndRearm()
	{
		OnEndRearm?.Invoke();
	}

	private void OnEnable()
	{
		StartCoroutine(SetParentRoutine());
	}

	private IEnumerator SetParentRoutine()
	{
		VTNetEntity netEnt = GetComponent<VTNetEntity>();
		while (!netEnt.hasRegistered)
		{
			yield return null;
		}
		Actor a = null;
		while (a == null)
		{
			a = VTOLMPSceneManager.instance.GetActor(netEnt.ownerID);
			yield return null;
		}
		MultiUserVehicleSync component = a.GetComponent<MultiUserVehicleSync>();
		PlayerInfo player = VTOLMPLobbyManager.GetPlayer(netEnt.ownerID);
		MultiSlotVehicleManager.PlayerSlot slot = component.GetSlot(player);
		Debug.Log("Setting pilot avatar parent to " + slot.spawnTransform.gameObject.name + " for player " + player.pilotName, base.gameObject);
		base.transform.parent = slot.spawnTransform;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
	}
}

}