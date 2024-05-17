using UnityEngine.UI;
using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_PopupText : uMyGUI_PopupButtons
	{
		[SerializeField]
		protected Text m_header;
		[SerializeField]
		protected Text m_body;
		[SerializeField]
		protected bool m_useExplicitNavigation;
	}
}
