using UnityEngine;
using UnityEngine.UI;

public class VTMapEdRoadListItem : MonoBehaviour
{
	public Text nameText;

	public Text descriptionText;

	public RawImage thumbImage;

	[HideInInspector]
	public int idx;

	[HideInInspector]
	public VTMapEdRoadsPanel roadPanel;

	public void OnClick()
	{
		roadPanel.SelectRoadSet(idx);
	}
}
