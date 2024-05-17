using UnityEngine;

namespace Oculus.Platform.Samples.VrBoardGame
{
	public class GameBoard : MonoBehaviour
	{
		[SerializeField]
		private Color[] m_playerColors;
		[SerializeField]
		private Color m_proposedMoveColor;
		[SerializeField]
		private BoardPosition[] m_positions;
	}
}
