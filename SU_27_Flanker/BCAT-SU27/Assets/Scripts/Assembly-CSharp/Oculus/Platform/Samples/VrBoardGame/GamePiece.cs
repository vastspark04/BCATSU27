using UnityEngine;

namespace Oculus.Platform.Samples.VrBoardGame
{
	public class GamePiece : MonoBehaviour
	{
		public enum Piece
		{
			A = 0,
			B = 1,
			PowerBall = 2,
		}

		[SerializeField]
		private Piece m_type;
		[SerializeField]
		private GameObject m_prefabA;
		[SerializeField]
		private GameObject m_prefabB;
		[SerializeField]
		private GameObject m_prefabPower;
	}
}
