using UnityEngine;

public class VTMapEdObjectSelectItem : MonoBehaviour
{
	public GameObject baseEditButton;

	public int idx;

	public int lineIdx;

	public VTMapEdObjectsTab objTab;

	private float lastClickTime;

	public void OnClick()
	{
		objTab.SelectItem(idx, lineIdx);
		if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
		{
			objTab.GoToObject(idx);
		}
		else
		{
			lastClickTime = Time.unscaledTime;
		}
	}

	public void OnClickSettingsButton()
	{
		objTab.EditBaseName(idx);
	}
}
