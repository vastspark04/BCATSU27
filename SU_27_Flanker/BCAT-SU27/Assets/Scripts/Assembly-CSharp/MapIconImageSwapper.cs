using UnityEngine;
using UnityEngine.UI;

public class MapIconImageSwapper : MonoBehaviour
{
	public Image singleImage;

	public GameObject groupedImageObj;

	public void SetGrouped(bool grouped)
	{
		singleImage.enabled = !grouped;
		groupedImageObj.SetActive(grouped);
	}
}
