using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_PopupButtons : uMyGUI_Popup
	{
		[SerializeField]
		protected RectTransform[] m_buttons;
		[SerializeField]
		protected string[] m_buttonNames;
		[SerializeField]
		protected bool m_improveNavigationFocus;
	}
}
