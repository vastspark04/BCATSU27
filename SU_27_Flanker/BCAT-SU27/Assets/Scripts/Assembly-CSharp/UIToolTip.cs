using UnityEngine;
using UnityEngine.UI;

public class UIToolTip : MonoBehaviour
{
	public Text tooltipText;

	public ContentSizeFitter sizeFitter;

	public float borderSize;

	public void Display(string text)
	{
		base.gameObject.SetActive(value: true);
		tooltipText.text = text;
		sizeFitter.SetLayoutVertical();
		sizeFitter.SetLayoutHorizontal();
		RectTransform obj = (RectTransform)base.transform;
		Rect rect = ((RectTransform)tooltipText.transform).rect;
		obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.height + borderSize);
		obj.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.width + borderSize);
		base.transform.position = Input.mousePosition;
	}

	public void Hide()
	{
		base.gameObject.SetActive(value: false);
	}
}
