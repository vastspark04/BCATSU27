using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VTEdImageListItem : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
{
	public Text nameText;

	public RawImage image;

	private int index;

	private Action<int> OnClick;

	public void Setup(int index, string text, Texture image, Action<int> onClick)
	{
		this.index = index;
		nameText.text = text;
		this.image.texture = image;
		OnClick = onClick;
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (OnClick != null)
		{
			OnClick(index);
		}
	}
}
