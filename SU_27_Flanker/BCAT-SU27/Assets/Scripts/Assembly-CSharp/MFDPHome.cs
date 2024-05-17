using System.Collections.Generic;
using UnityEngine;

public class MFDPHome : MFDPortalPage
{
	public GameObject menuItemTemplate;

	public float buttonMargin;

	public void SetupButtons(MFDPortalQuarter qtr, List<MFDPortalPage> pages)
	{
		RectTransform rectTransform = (RectTransform)menuItemTemplate.transform;
		float num = rectTransform.rect.width + buttonMargin;
		float num2 = rectTransform.rect.height + buttonMargin;
		menuItemTemplate.gameObject.SetActive(value: false);
		for (int i = 0; i < pages.Count; i++)
		{
			pages[i].ApplyLocalization();
			int num3 = i % 3;
			int num4 = i / 3;
			Vector3 localPosition = rectTransform.localPosition + new Vector3((float)num3 * num, (float)(-num4) * num2, 0f);
			GameObject obj = Object.Instantiate(menuItemTemplate);
			MFDPortalPageSelectButton component = obj.GetComponent<MFDPortalPageSelectButton>();
			obj.gameObject.SetActive(value: true);
			obj.transform.SetParent(menuItemTemplate.transform.parent);
			obj.transform.localPosition = localPosition;
			component.page = pages[i];
			component.portalQtr = qtr;
			component.buttonText.text = pages[i].pageLabel;
			component.interactable.interactableName = pages[i].pageName;
		}
	}
}
