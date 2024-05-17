using UnityEngine;
using UnityEngine.UI;

public static class VTUIExtensions
{
	public static void ClampVertical(this ScrollRect scrollRect)
	{
		scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
	}

	public static void ViewContent(this ScrollRect scrollRect, RectTransform targetContent)
	{
		float height = scrollRect.content.rect.height;
		float height2 = scrollRect.viewport.rect.height;
		_ = height2 / height;
		float num = scrollRect.transform.InverseTransformPoint(targetContent.transform.position).y - scrollRect.transform.InverseTransformPoint(scrollRect.viewport.transform.position).y;
		if (num > 0f)
		{
			scrollRect.content.localPosition -= new Vector3(0f, num, 0f);
		}
		else if (num < 0f - height2 + targetContent.rect.height)
		{
			scrollRect.content.localPosition += new Vector3(0f, targetContent.rect.height + (0f - height2 - num), 0f);
		}
	}
}
