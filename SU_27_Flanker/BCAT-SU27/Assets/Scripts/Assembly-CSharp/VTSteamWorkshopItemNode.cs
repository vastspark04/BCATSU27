using LapinerTools.Steam.UI;
using UnityEngine.UI;
using UnityEngine;

public class VTSteamWorkshopItemNode : SteamWorkshopItemNode
{
	[SerializeField]
	protected Text m_ownerName;
	public string author;
	public bool allowRetrieval;
	public GameObject retrieveButton;
}
