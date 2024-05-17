using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_Draggable : MonoBehaviour
	{
		[SerializeField]
		private bool m_isResetRotationWhenDragged;
		[SerializeField]
		private bool m_isSnapBackOnEndDrag;
		[SerializeField]
		private bool m_isTopInHierarchyWhenDragged;
		[SerializeField]
		private CanvasGroup m_disableBlocksRaycastsOnDrag;
	}
}
