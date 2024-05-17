using UnityEngine;
using VTOLVR.Multiplayer;

namespace VTOLVR.DLC.Rotorcraft{

public class AH94CollectiveFunctions : MonoBehaviour
{
	public VRThrottle flightCollective;

	public VRThrottle combatCollective;

	public MFDManager mfdManager;

	public CountermeasureManager cmm;

	public ThrottleSOISwitcher soiSwitcher;

	public ArticulatingHardpoint artiHardpoint;

	public ArticulatingHardpointSync ahpSync;

	public AH94CentralAPController apController;

	public VehicleInputManager vim;

	public HelicopterRotor tailRotor;

	public MultiUserVehicleSync muvs;

	private bool isMP;

	private bool thumbRudder;

	private bool firingCM;

	private bool cMovingStick;

	private float timeSetArtiAuto;

	private void Start()
	{
		isMP = VTOLMPUtils.IsMultiplayer();
		if ((bool)flightCollective)
		{
			thumbRudder = vim.thumbRudderAllowed && GameSettings.CurrentSettings.GetBoolSetting("THUMB_RUDDER");
			flightCollective.OnMenuButtonDown.AddListener(FlightMenuButtonDown);
			flightCollective.OnSetThumbstick.AddListener(OnFlightCollectiveThumbstick);
		}
		if ((bool)combatCollective)
		{
			combatCollective.OnMenuButtonDown.AddListener(CombatMenuButtonDown);
			combatCollective.OnMenuButtonUp.AddListener(CombatMenuButtonUp);
			combatCollective.OnSetThumbstick.AddListener(CombatOnSetThumbstick);
			combatCollective.OnResetThumbstick.AddListener(CombatOnResetThumbstick);
			combatCollective.OnStickPressDown.AddListener(CombatOnStickPressDown);
			combatCollective.OnStickPressUp.AddListener(CombatOnStickPressUp);
			combatCollective.OnStickPressed.AddListener(CombatOnStickPressed);
			combatCollective.OnTriggerDown.AddListener(CombatOnTriggerDown);
			combatCollective.OnTriggerUp.AddListener(CombatOnTriggerUp);
		}
	}

	private void FlightMenuButtonDown()
	{
		if (apController.vtolAp.flightInfo.isLanded)
		{
			return;
		}
		if (apController.vtolAp.headingHold || apController.vtolAp.altitudeHold || apController.vtolAp.navMode)
		{
			apController.AllAPOff();
		}
		else if (apController.vtolAp.hoverMode)
		{
			apController.ToggleHoverMode();
			if (apController.vtolAp.altitudeHold)
			{
				apController.ToggleAltitudeHold();
			}
		}
		else
		{
			apController.ToggleHoverMode();
			if (!apController.vtolAp.altitudeHold)
			{
				apController.ToggleAltitudeHold();
			}
		}
	}

	private void OnFlightCollectiveThumbstick(Vector3 s)
	{
		if (thumbRudder)
		{
			vim.SetThrottleThumbstick(s);
		}
		else
		{
			tailRotor.ShifitTrimYaw(s);
		}
	}

	private void CombatMenuButtonDown()
	{
		if (combatCollective.IsTriggerPressed())
		{
			if (isMP && !muvs.netEntity.isMine)
			{
				muvs.RemoteStartCM();
			}
			else
			{
				cmm.FireCM();
			}
			firingCM = true;
		}
		else
		{
			soiSwitcher.ToggleSOI();
		}
	}

	private void CombatMenuButtonUp()
	{
		if (firingCM)
		{
			if (isMP && !muvs.netEntity.isMine)
			{
				muvs.RemoteStopCM();
			}
			else
			{
				cmm.StopFireCM();
			}
			firingCM = false;
		}
	}

	private void CombatOnSetThumbstick(Vector3 axis)
	{
		if (combatCollective.IsTriggerPressed())
		{
			if ((!VTOLMPUtils.IsMultiplayer() || muvs.IsLocalWeaponController()) && Time.time - timeSetArtiAuto > 0.5f)
			{
				if ((bool)ahpSync && !ahpSync.isMine)
				{
					ahpSync.RemoteInput(0f - axis.y);
				}
				else
				{
					artiHardpoint.Tilt(0f - axis.y, Time.deltaTime);
				}
			}
		}
		else
		{
			cMovingStick = true;
			mfdManager.OnInputAxis(axis);
		}
	}

	private void CombatOnResetThumbstick()
	{
		if (!combatCollective.IsTriggerPressed())
		{
			mfdManager.OnInputAxisReleased();
			cMovingStick = false;
		}
	}

	private void CombatOnStickPressDown()
	{
		if (combatCollective.IsTriggerPressed())
		{
			if (!VTOLMPUtils.IsMultiplayer() || muvs.IsLocalWeaponController())
			{
				if ((bool)ahpSync && !ahpSync.isMine)
				{
					ahpSync.RemoteSetAuto();
				}
				else
				{
					artiHardpoint.autoMode = true;
				}
				timeSetArtiAuto = Time.time;
			}
		}
		else
		{
			mfdManager.OnInputButtonDown();
		}
	}

	private void CombatOnStickPressUp()
	{
		if (!combatCollective.IsTriggerPressed())
		{
			mfdManager.OnInputButtonUp();
		}
	}

	private void CombatOnStickPressed()
	{
		if (!combatCollective.IsTriggerPressed())
		{
			mfdManager.OnInputButton();
		}
	}

	private void CombatOnTriggerDown()
	{
		if (cMovingStick)
		{
			mfdManager.OnInputAxisReleased();
			cMovingStick = false;
		}
	}

	private void CombatOnTriggerUp()
	{
	}
}

}