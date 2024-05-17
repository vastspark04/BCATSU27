using UnityEngine;

[ExecuteInEditMode]
public class ScaleCorrectedStretchTransform : MonoBehaviour
{
	private RectTransform rectTf;

	private void OnEnable()
	{
		if (!rectTf)
		{
			rectTf = (RectTransform)base.transform;
		}
	}

	private void Update()
	{
		if ((bool)rectTf.parent)
		{
			RectTransform rectTransform = (RectTransform)rectTf.parent;
			rectTf.localPosition = new Vector3(rectTransform.rect.center.x, rectTransform.rect.center.y, 0f);
			rectTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.width / rectTf.localScale.x);
			rectTf.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.height / rectTf.localScale.y);
		}
	}
}
