using UnityEngine;
using UnityEngine.UI;

public class VTMapEdPrefabListItem : MonoBehaviour
{
	[Header("Components")]
	public RawImage thumbImage;

	public Text nameText;

	public Text descriptionText;

	[Header("Set at runtime")]
	public int idx;

	public VTMapEdPrefabSelector selector;

	public void OnClick()
	{
		selector.SelectPrefab(idx);
	}
}
