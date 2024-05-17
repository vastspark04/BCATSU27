using UnityEngine;
using UnityEngine.UI;

public class PilotSelectInfoUI : MonoBehaviour
{
	public Text pilotName;

	public Text pilotInfo;

	public void UpdateUI(PilotSave p)
	{
		pilotName.text = p.pilotName;
		if ((bool)pilotInfo)
		{
			pilotInfo.text = p.GetInfoString();
		}
	}
}
