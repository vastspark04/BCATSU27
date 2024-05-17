using UnityEngine;
using UnityEngine.UI;

public class LocalizationEditorEntry : MonoBehaviour
{
	public Text keyText;

	public UIMaskedTextScroller descriptionScroller;

	public Text descriptionText;

	public Text englishText;

	public ScrollRect englishScroll;

	public InputField langInput;

	public ScrollRect langScroll;

	public void Setup(string key, string desc, string english, string existingTranslation)
	{
		keyText.text = key;
		englishText.text = english;
		descriptionText.text = desc;
		descriptionScroller.Refresh();
		englishText.GetComponent<ContentSizeFitter>().SetLayoutVertical();
		englishScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, englishText.rectTransform.rect.height);
		langInput.text = existingTranslation;
	}
}
