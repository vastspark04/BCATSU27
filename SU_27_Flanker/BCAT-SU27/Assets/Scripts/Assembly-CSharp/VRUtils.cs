using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Valve.VR;

public static class VRUtils
{
	public class VibeCoroutineScript : MonoBehaviour
	{
		private Dictionary<uint, ushort> pulses = new Dictionary<uint, ushort>();

		private List<uint> keys = new List<uint>();
	}

	private static GameObject vibeObject;

	private static VibeCoroutineScript vibeScript;

	public static void VibeController(VRHandController con, ushort pulse, float time)
	{
	}

	public static void HapticPulse(VRHandController con, ushort pulse)
	{
	}

	public static void DisableVR()
	{
		if (XRSettings.enabled && !(XRSettings.loadedDeviceName == ""))
		{
			Debug.Log($"Disabling VR. XRSettings.enabled = {XRSettings.enabled}, loadedDeviceName = {XRSettings.loadedDeviceName}");
			if (!GameSettings.VR_SDK_IS_OCULUS)
			{
				Object.Destroy(Object.FindObjectOfType<SteamVR_Behaviour>());
				SteamVR.enabled = false;
			}
			XRGeneralSettings.Instance.Manager.StopSubsystems();
			XRGeneralSettings.Instance.Manager.DeinitializeLoader();
			Application.targetFrameRate = Mathf.Min(120, Screen.currentResolution.refreshRate);
			Debug.Log("Disabling VR and setting targetFrameRate to " + Application.targetFrameRate);
		}
	}
}
