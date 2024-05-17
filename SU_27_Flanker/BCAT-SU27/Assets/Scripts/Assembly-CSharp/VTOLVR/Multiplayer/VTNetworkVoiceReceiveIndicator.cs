using System.Collections;
using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTNetworkVoiceReceiveIndicator : MonoBehaviour
{
	public GameObject indicatorObj;

	public SteamId id;

	private void OnEnable()
	{
		StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(0.2f);
		while (base.enabled)
		{
			if ((ulong)id != 0L)
			{
				indicatorObj.SetActive(VTNetworkVoice.instance.IsUserTransmittingVoice(id));
			}
			else
			{
				indicatorObj.SetActive(value: false);
			}
			yield return wait;
		}
	}
}

}