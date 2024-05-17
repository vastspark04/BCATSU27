using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTNetworkVoiceInputIndicator : MonoBehaviour
{
	public GameObject indicatorObj;

	private void Update()
	{
		indicatorObj.SetActive(VTNetworkVoice.VoiceInputLevel > 0.01f);
	}
}

}