using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTNetworkVoicePTT : MonoBehaviour
{
	public GameObject indicatorObj;

	public KeyCode pttKey;

	public AudioClip pttStartClip;

	public AudioClip pttStopClip;

	public AudioSource pttStartStopSource;

	public bool setVoiceRecord = true;

	private bool voiceOn;

	public bool isVoiceOn => voiceOn;

	private void OnEnable()
	{
		StopVoice();
		if (pttKey != 0)
		{
			StartCoroutine(KeycodeRoutine());
		}
	}

	private void OnDisable()
	{
		StopVoice();
	}

	private IEnumerator KeycodeRoutine()
	{
		while (base.enabled)
		{
			if (Input.GetKeyDown(pttKey))
			{
				StartVoice();
			}
			if (Input.GetKeyUp(pttKey))
			{
				StopVoice();
			}
			yield return null;
		}
	}

	public void StartVoice()
	{
		if (base.enabled && base.gameObject.activeInHierarchy && !voiceOn)
		{
			voiceOn = true;
			if ((bool)pttStartStopSource)
			{
				pttStartStopSource.PlayOneShot(pttStartClip);
			}
			if ((bool)indicatorObj)
			{
				indicatorObj.SetActive(value: true);
			}
			if ((bool)VTNetworkVoice.instance)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: true);
			}
		}
	}

	public void StopVoice()
	{
		if (base.enabled && base.gameObject.activeInHierarchy && voiceOn)
		{
			voiceOn = false;
			if ((bool)indicatorObj)
			{
				indicatorObj.SetActive(value: false);
			}
			if ((bool)pttStartStopSource)
			{
				pttStartStopSource.PlayOneShot(pttStopClip);
			}
			if ((bool)VTNetworkVoice.instance)
			{
				VTNetworkVoice.instance.SetVoiceRecord(r: false);
			}
		}
	}
}

}