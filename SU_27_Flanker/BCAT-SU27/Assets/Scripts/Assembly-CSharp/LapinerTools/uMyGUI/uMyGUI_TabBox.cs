using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_TabBox : MonoBehaviour
	{
		public enum EAnimMode
		{
			NONE = 0,
			TAB_ONLY = 1,
			BTN_ONLY = 2,
			TAB_AND_BTN = 3,
		}

		[SerializeField]
		private RectTransform[] m_btns;
		[SerializeField]
		private RectTransform[] m_tabs;
		[SerializeField]
		private int m_selectedIndex;
		[SerializeField]
		private bool m_isSelectTabOnStart;
		[SerializeField]
		private bool m_isPlayTabAnimOnStart;
		[SerializeField]
		private bool m_isPlayBtnAnimOnStart;
		[SerializeField]
		private EAnimMode m_animMode;
		[SerializeField]
		private string m_fadeInAnimTab;
		[SerializeField]
		private string m_fadeOutAnimTab;
		[SerializeField]
		private string m_fadeInAnimBtn;
		[SerializeField]
		private string m_fadeOutAnimBtn;
		[SerializeField]
		private bool m_isSendMessage;
		[SerializeField]
		private bool m_isMoveDownInHierarchyOnSelect;
	}
}
