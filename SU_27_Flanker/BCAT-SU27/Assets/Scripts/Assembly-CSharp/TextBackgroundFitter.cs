using UnityEngine;
using UnityEngine.UI;

public class TextBackgroundFitter : MonoBehaviour
{
	[SerializeField]
	private Text text;

	[SerializeField]
	private ContentSizeFitter textFitter;

	public float margin;

	public void SetText(string txt)
	{
		text.text = txt;
		textFitter.SetLayoutHorizontal();
		textFitter.SetLayoutVertical();
		float height = ((RectTransform)text.transform).rect.height;
		((RectTransform)base.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + 2f * margin);
	}
}
