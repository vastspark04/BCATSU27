using UnityEngine;
using UnityEngine.UI;

public class VTEdBaseListItem : MonoBehaviour
{
	public int baseID;

	public VTSBasesWindow basesWindow;

	public Image selectorButtonImage;

	public Text baseNameText;

	private float lastClickTime;

	public void ClickButton()
	{
		basesWindow.OpenBaseEditor(baseID);
		if (Time.unscaledTime - lastClickTime < VTOLVRConstants.DOUBLE_CLICK_TIME)
		{
			basesWindow.editor.editorCamera.FocusOnPoint(basesWindow.editor.currentScenario.bases.baseInfos[baseID].basePrefab.transform.position);
		}
		lastClickTime = Time.unscaledTime;
	}
}
