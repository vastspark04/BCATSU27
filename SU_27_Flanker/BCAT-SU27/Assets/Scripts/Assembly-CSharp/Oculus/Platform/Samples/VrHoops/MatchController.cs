using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Platform.Samples.VrHoops
{
	public class MatchController : MonoBehaviour
	{
		[SerializeField]
		private Text m_timerText;
		[SerializeField]
		private Camera m_camera;
		[SerializeField]
		private Transform m_idleCameraTransform;
		[SerializeField]
		private Text m_matchmakeButtonText;
		[SerializeField]
		private PlayerArea[] m_playerAreas;
		[SerializeField]
		private uint PRACTICE_WARMUP_TIME;
		[SerializeField]
		private uint MATCH_WARMUP_TIME;
		[SerializeField]
		private uint MATCH_TIME;
		[SerializeField]
		private uint MATCH_COOLDOWN_TIME;
		[SerializeField]
		private GameObject m_mostWinsLeaderboard;
		[SerializeField]
		private GameObject m_highestScoresLeaderboard;
		[SerializeField]
		private GameObject m_leaderboardEntryPrefab;
		[SerializeField]
		private GameObject m_flytext;
	}
}
