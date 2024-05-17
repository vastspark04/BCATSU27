using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_TreeBrowser : MonoBehaviour
	{
		[SerializeField]
		private GameObject m_innerNodePrefab;
		[SerializeField]
		private GameObject m_leafNodePrefab;
		[SerializeField]
		private float m_offsetStart;
		[SerializeField]
		private float m_offsetEnd;
		[SerializeField]
		private float m_padding;
		[SerializeField]
		private float m_indentSize;
		[SerializeField]
		private float m_forcedEntryHeight;
		[SerializeField]
		private bool m_useExplicitNavigation;
		[SerializeField]
		private float m_navScrollSpeed;
		[SerializeField]
		private float m_navScrollSmooth;
	}
}
