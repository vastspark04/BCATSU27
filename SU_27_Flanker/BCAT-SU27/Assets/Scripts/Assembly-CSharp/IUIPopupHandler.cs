using UnityEngine;

public interface IUIPopupHandler
{
	void OnDisplayPopup(Transform popupTransform);

	void OnClosePopup(Transform popupTransform);
}
