using UnityEngine;

namespace VTOLVR.Multiplayer{

public class MultiplayerObjectSwitch : MonoBehaviour
{
	public GameObject[] singlePlayerObjs;

	public GameObject[] multiplayerObjs;

	private void Awake()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			singlePlayerObjs.SetActive(active: false);
			multiplayerObjs.SetActive(active: true);
		}
		else
		{
			singlePlayerObjs.SetActive(active: true);
			multiplayerObjs.SetActive(active: false);
		}
	}
}

}