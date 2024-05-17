using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Platform.Samples.VrBoardGame
{
	public class GameController : MonoBehaviour
	{
		[SerializeField]
		private MatchmakingManager m_matchmaking;
		[SerializeField]
		private GameBoard m_board;
		[SerializeField]
		private GamePiece m_pieceA;
		[SerializeField]
		private GamePiece m_pieceB;
		[SerializeField]
		private GamePiece m_powerPiece;
		[SerializeField]
		private Color m_unusableColor;
		[SerializeField]
		private Color m_unselectedColor;
		[SerializeField]
		private Color m_selectedColor;
		[SerializeField]
		private Color m_highlightedColor;
		[SerializeField]
		private Text m_ballCountText;
		[SerializeField]
		private Text m_player0Text;
		[SerializeField]
		private Text m_player1Text;
	}
}
