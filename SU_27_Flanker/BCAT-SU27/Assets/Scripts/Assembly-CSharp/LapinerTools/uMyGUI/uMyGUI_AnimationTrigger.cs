using UnityEngine;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_AnimationTrigger : MonoBehaviour
	{
		public enum ETriggerMode
		{
			ON_ENABLE = 0,
			ON_DISABLE = 1,
			ON_UMYGUI_ACTIVATETAB = 2,
			ON_UMYGUI_DEACTIVATETAB = 3,
			REDIRECT_ONMYGUI_EVENTS = 4,
		}

		[SerializeField]
		private Animation m_animation;
		[SerializeField]
		private string m_clipName;
		[SerializeField]
		private ETriggerMode m_condition;
		[SerializeField]
		private bool m_isActivateOnAnimStart;
		[SerializeField]
		private bool m_isDeactivateOnAnimEnd;
		[SerializeField]
		private MonoBehaviour m_alternativeCoroutineWorker;
		[SerializeField]
		private GameObject m_redirectDestination;
	}
}
