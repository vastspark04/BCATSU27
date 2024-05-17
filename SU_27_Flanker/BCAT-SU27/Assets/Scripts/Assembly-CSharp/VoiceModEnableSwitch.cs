using UnityEngine;

public class VoiceModEnableSwitch : MonoBehaviour
{
	public GameObject[] SwitchTargets;

	public bool SetActive<T>(int target) where T : MonoBehaviour
	{
		if (target < 0 || target >= SwitchTargets.Length)
		{
			return false;
		}
		for (int i = 0; i < SwitchTargets.Length; i++)
		{
			SwitchTargets[i].SetActive(value: false);
			OVRLipSyncContextMorphTarget component = SwitchTargets[i].GetComponent<OVRLipSyncContextMorphTarget>();
			if ((bool)component)
			{
				component.enabled = false;
			}
			OVRLipSyncContextTextureFlip component2 = SwitchTargets[i].GetComponent<OVRLipSyncContextTextureFlip>();
			if ((bool)component2)
			{
				component2.enabled = false;
			}
		}
		SwitchTargets[target].SetActive(value: true);
		MonoBehaviour component3 = SwitchTargets[target].GetComponent<T>();
		if (component3 != null)
		{
			component3.enabled = true;
		}
		return true;
	}

	public bool SetActive(int target)
	{
		if (target < 0 || target >= SwitchTargets.Length)
		{
			return false;
		}
		for (int i = 0; i < SwitchTargets.Length; i++)
		{
			SwitchTargets[i].SetActive(value: false);
		}
		SwitchTargets[target].SetActive(value: true);
		return true;
	}
}
