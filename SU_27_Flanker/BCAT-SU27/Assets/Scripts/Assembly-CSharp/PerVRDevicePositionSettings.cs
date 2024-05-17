using UnityEngine;

[CreateAssetMenu(menuName = "VR/Per Device Position Settings")]
public class PerVRDevicePositionSettings : ScriptableObject
{
	public PerVRDeviceLocalPosition.DeviceSetting[] settings;
}
