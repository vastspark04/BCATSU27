using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94PilotReceiver : MonoBehaviour
{
	public Battery battery;

	public HUDMaskToggler hudMaskToggler;

	public GameObject hudPowerObject;

	public FlightInfo flightInfo;

	public InteractiveBobbleHead bobbleHead;

	public Transform leftFootTarget;

	public Transform rightFootTarget;

	public VRJoystick[] joysticks;

	public TargetingMFDPage tgpPage;

	public FlybyCameraMFDPage sCam;

	public VRTwistKnobInt hmdSwitch;

	public VTNetworkVoicePTT[] ptt;

	public void ConnectLocal()
	{
		GetComponentInChildren<AH94PilotInserter>().ConnectLocal(this);
	}
}

}