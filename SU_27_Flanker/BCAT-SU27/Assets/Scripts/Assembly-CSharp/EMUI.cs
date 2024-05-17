using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EMUI : MonoBehaviour
{
	public static bool UIClicked;

	public static bool UIHelpOverlay;

	private List<RaycastResult> UIRaycast()
	{
		PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
		pointerEventData.position = Input.mousePosition;
		List<RaycastResult> list = new List<RaycastResult>();
		EventSystem.current.RaycastAll(pointerEventData, list);
		return list;
	}

	public bool CheckGUI(int mouseButtonIndex, ref bool UILockInstigator)
	{
		if (Input.GetMouseButton(mouseButtonIndex))
		{
			if (UIHelpOverlay)
			{
				return false;
			}
			if (UIClicked && !UILockInstigator)
			{
				return false;
			}
			if (!UIClicked && !UILockInstigator)
			{
				List<RaycastResult> list = UIRaycast();
				if (list.Count > 0 && list[0].gameObject.layer == 5)
				{
					return false;
				}
				UILockInstigator = true;
				UIClicked = true;
				return true;
			}
			if (UIClicked & UILockInstigator)
			{
				return true;
			}
		}
		if (Input.GetMouseButtonUp(mouseButtonIndex) & UILockInstigator)
		{
			UIClicked = false;
			UILockInstigator = false;
			return false;
		}
		return false;
	}
}
