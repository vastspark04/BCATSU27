using Rewired.Dev;

namespace RewiredConsts{

public static class Action
{
	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Rudder")]
	public const int Rudder = 0;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Wheel Brake")]
	public const int Wheel_Brake_L = 5;

	[ActionIdFieldInfo(categoryName = "Default", friendlyName = "Wheel Brake")]
	public const int Wheel_Brake_R = 6;

	[ActionIdFieldInfo(categoryName = "Replace Unity Mouse Inputs", friendlyName = "Mouse X")]
	public const int Mouse_X = 2;

	[ActionIdFieldInfo(categoryName = "Replace Unity Mouse Inputs", friendlyName = "Mouse Y")]
	public const int Mouse_Y = 3;

	[ActionIdFieldInfo(categoryName = "Replace Unity Mouse Inputs", friendlyName = "Action0")]
	public const int Mouse_Scroll = 4;
}

}