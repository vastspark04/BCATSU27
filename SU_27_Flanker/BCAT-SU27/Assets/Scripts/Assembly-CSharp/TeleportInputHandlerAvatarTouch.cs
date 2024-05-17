public class TeleportInputHandlerAvatarTouch : TeleportInputHandlerHMD
{
	public enum InputModes
	{
		CapacitiveButtonForAimAndTeleport = 0,
		SeparateButtonsForAimAndTeleport = 1,
		ThumbstickTeleport = 2,
		ThumbstickTeleportForwardBackOnly = 3,
	}

	public enum AimCapTouchButtons
	{
		A = 0,
		B = 1,
		LeftTrigger = 2,
		LeftThumbstick = 3,
		RightTrigger = 4,
		RightThumbstick = 5,
		X = 6,
		Y = 7,
	}

	public OvrAvatar Avatar;
	public InputModes InputMode;
	public OVRInput.Controller AimingController;
	public AimCapTouchButtons CapacitiveAimAndTeleportButton;
	public float ThumbstickTeleportThreshold;
}
