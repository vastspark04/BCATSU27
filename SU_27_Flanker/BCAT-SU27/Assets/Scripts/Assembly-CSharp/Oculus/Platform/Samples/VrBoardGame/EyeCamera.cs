using UnityEngine;
using UnityEngine.EventSystems;

namespace Oculus.Platform.Samples.VrBoardGame
{
	public class EyeCamera : MonoBehaviour
	{
		[SerializeField]
		private EventSystem m_eventSystem;
		[SerializeField]
		private GameController m_gameController;
		[SerializeField]
		private SphereCollider m_gazeTracker;
	}
}
