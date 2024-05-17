using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTNetworkVoiceToggle : MonoBehaviour
{
	public GameObject indicatorObj;

	private static bool voiceSetting;

	public void Toggle()
	{
		SetVoice(!voiceSetting);
	}

	private void OnEnable()
	{
		SetVoice(voiceSetting);
	}

	private void OnDisable()
	{
		SetVoice(v: false, persist: false);
	}

	private void SetVoice(bool v, bool persist = true)
	{
		if ((bool)VTNetworkVoice.instance)
		{
			indicatorObj.SetActive(v);
			VTNetworkVoice.instance.SetVoiceRecord(v);
			if (persist)
			{
				voiceSetting = v;
			}
		}
		else
		{
			Debug.LogError($"VTNetworkVoiceToggle tried to set voice record {v} but VTNetworkVoice is not initialized.");
		}
	}

	public void SetVolume(float t)
	{
		VTNetworkVoice.SetVolume(t);
	}
}

}