using System;
using UnityEngine;
using UnityEngine.UI;

namespace VTOLVR.SteamWorkshop{

public class VTWorkshopItemInfoPage : MonoBehaviour
{
	public RawImage image;

	public Texture2D noImage;

	public Text itemNameText;

	public Text itemDescriptionText;

	public ContentSizeFitter descriptionFitter;

	public ScrollRect descriptionScrollRect;

	public Text byLineText;

	private Action onClose;

	public void SetItem(VTSWorkshopItemInfo item, Action onClose)
	{
		base.gameObject.SetActive(value: true);
		this.onClose = onClose;
		if ((bool)item.previewImage)
		{
			image.texture = item.previewImage;
		}
		else
		{
			image.texture = noImage;
		}
		itemNameText.text = item.title;
		itemDescriptionText.text = item.description;
		descriptionFitter.SetLayoutVertical();
		float num = itemDescriptionText.rectTransform.rect.height * itemDescriptionText.transform.localScale.y;
		descriptionScrollRect.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num + 10f);
		descriptionScrollRect.verticalNormalizedPosition = 1f;
		byLineText.text = (string.IsNullOrEmpty(item.ownerName) ? string.Empty : ("by " + item.ownerName));
	}

	public void CloseButton()
	{
		if (onClose != null)
		{
			onClose();
		}
		base.gameObject.SetActive(value: false);
	}
}

}