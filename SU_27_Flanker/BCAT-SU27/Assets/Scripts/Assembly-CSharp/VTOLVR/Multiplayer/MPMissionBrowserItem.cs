using System;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.Multiplayer{

public class MPMissionBrowserItem : MonoBehaviour
{
	public Text nameText;

	public Text authorText;

	public Text countText;

	public RawImage thumbnail;

	public Action OnSelect;

	public GameObject selectedObj;

	public VTCampaignInfo campaign;

	public VTScenarioInfo mission;

	public void Select()
	{
		OnSelect?.Invoke();
	}
}

}