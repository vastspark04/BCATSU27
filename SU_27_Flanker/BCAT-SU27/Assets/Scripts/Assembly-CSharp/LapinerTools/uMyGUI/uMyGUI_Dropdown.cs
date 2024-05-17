using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_Dropdown : MonoBehaviour
	{
		[SerializeField]
		private Button m_button;
		[SerializeField]
		private Text m_text;
		[SerializeField]
		private RectTransform m_entriesRoot;
		[SerializeField]
		private RectTransform m_entriesBG;
		[SerializeField]
		private Scrollbar m_entriesScrollbar;
		[SerializeField]
		private Button m_entryButton;
		[SerializeField]
		private int m_entrySpacing;
		[SerializeField]
		private string m_staticText;
		[SerializeField]
		private string m_nothingSelectedText;
		[SerializeField]
		protected bool m_improveNavigationFocus;
		[SerializeField]
		private string[] m_entries;
		[SerializeField]
		private int m_selectedIndex;
	}
}
