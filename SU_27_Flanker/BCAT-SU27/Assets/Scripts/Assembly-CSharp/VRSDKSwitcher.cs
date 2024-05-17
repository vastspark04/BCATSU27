using UnityEngine;
using Valve.VR;

public class VRSDKSwitcher : MonoBehaviour
{
	public GameObject[] openVROnlyObjects;

	public GameObject[] oculusOnlyObjects;

	private void Awake()
	{
		Debug.Log("VR SDK: " + GameSettings.VR_SDK_ID);
		Debug.Log("SDK is oculus: " + GameSettings.VR_SDK_IS_OCULUS);
		if (!GameSettings.VR_SDK_IS_OCULUS)
		{
			SetToOpenVR();
		}
		else
		{
			SetToOculus();
		}
	}

	private void SetToOculus()
	{
		Debug.Log("Setting gameObject components to Oculus mode.");
		GameObject[] array = openVROnlyObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
		array = oculusOnlyObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		SteamVR_Camera[] componentsInChildren = GetComponentsInChildren<SteamVR_Camera>(includeInactive: true);
		foreach (SteamVR_Camera obj in componentsInChildren)
		{
			obj.enabled = false;
			Object.DestroyImmediate(obj);
		}
		SteamVR_TrackedObject[] componentsInChildren2 = GetComponentsInChildren<SteamVR_TrackedObject>(includeInactive: true);
		foreach (SteamVR_TrackedObject obj2 in componentsInChildren2)
		{
			obj2.enabled = false;
			Object.DestroyImmediate(obj2);
		}
		VTSteamVRController[] componentsInChildren3 = GetComponentsInChildren<VTSteamVRController>();
		foreach (VTSteamVRController obj3 in componentsInChildren3)
		{
			obj3.enabled = false;
			Object.DestroyImmediate(obj3);
		}
		SteamVR_Behaviour_Pose[] componentsInChildren4 = GetComponentsInChildren<SteamVR_Behaviour_Pose>();
		foreach (SteamVR_Behaviour_Pose obj4 in componentsInChildren4)
		{
			obj4.enabled = false;
			Object.DestroyImmediate(obj4);
		}
		SteamVR_PlayArea[] componentsInChildren5 = GetComponentsInChildren<SteamVR_PlayArea>();
		foreach (SteamVR_PlayArea obj5 in componentsInChildren5)
		{
			obj5.enabled = false;
			MeshRenderer component = obj5.GetComponent<MeshRenderer>();
			if ((bool)component)
			{
				component.enabled = false;
			}
			Object.DestroyImmediate(obj5);
		}
		RiftTouchController[] componentsInChildren6 = GetComponentsInChildren<RiftTouchController>();
		for (int i = 0; i < componentsInChildren6.Length; i++)
		{
			componentsInChildren6[i].gameObject.SetActive(value: true);
		}
	}

	private void SetToOpenVR()
	{
		GameObject[] array = openVROnlyObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: true);
		}
		array = oculusOnlyObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}
}
