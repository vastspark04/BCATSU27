using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Platform.Samples.VrBoardGame
{
	public class MatchmakingManager : MonoBehaviour
	{
		[SerializeField]
		private GameController m_gameController;
		[SerializeField]
		private Text m_matchButtonText;
		[SerializeField]
		private Text m_infoText;
	}
}
