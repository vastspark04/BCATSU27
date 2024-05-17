using System;
using UnityEngine;
using UnityEngine.XR;

public class PerVRDeviceLocalPosition : MonoBehaviour
{
	[Serializable]
	public class DeviceSetting
	{
		public string nameIncludes;

		public Vector3 localPosition;
	}

	public bool defaultIsZero = true;

	public PerVRDevicePositionSettings deviceSettings;

	private void Start()
	{
		InputDevice deviceAtXRNode = InputDevices.GetDeviceAtXRNode(XRNode.Head);
		if (string.IsNullOrEmpty(deviceAtXRNode.name))
		{
			return;
		}
		Debug.Log("PerVRDeviceLocalPosition -- Device: " + deviceAtXRNode.name);
		string text = deviceAtXRNode.name.ToLower();
		for (int i = 0; i < deviceSettings.settings.Length; i++)
		{
			if (text.Contains(deviceSettings.settings[i].nameIncludes))
			{
				Debug.Log($" - includes '{deviceSettings.settings[i].nameIncludes}' -- setting position {deviceSettings.settings[i].localPosition}");
				base.transform.localPosition = deviceSettings.settings[i].localPosition;
				return;
			}
		}
		Debug.Log(" - default position");
		if (defaultIsZero)
		{
			base.transform.localPosition = Vector3.zero;
		}
	}
}
