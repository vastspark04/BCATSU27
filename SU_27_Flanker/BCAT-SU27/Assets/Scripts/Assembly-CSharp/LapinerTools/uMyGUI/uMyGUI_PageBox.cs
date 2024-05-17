using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_PageBox : MonoBehaviour
	{
		[SerializeField]
		private Button m_previousButton;
		[SerializeField]
		private Button m_nextButton;
		[SerializeField]
		private Button m_pageButton;
		[SerializeField]
		private int m_pageCount;
		[SerializeField]
		private int m_maxPageBtnCount;
		[SerializeField]
		private int m_selectedPage;
	}
}
