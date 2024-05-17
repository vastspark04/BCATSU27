using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_PopupManager : MonoBehaviour
	{
		[SerializeField]
		private uMyGUI_Popup[] m_popups;
		[SerializeField]
		private string[] m_popupNames;
		[SerializeField]
		private CanvasGroup[] m_deactivatedElementsWhenPopupIsShown;
	}
}
