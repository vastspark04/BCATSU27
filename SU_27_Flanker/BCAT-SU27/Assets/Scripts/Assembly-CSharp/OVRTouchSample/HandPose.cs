using UnityEngine;

namespace OVRTouchSample
{
	public class HandPose : MonoBehaviour
	{
		[SerializeField]
		private bool m_allowPointing;
		[SerializeField]
		private bool m_allowThumbsUp;
		[SerializeField]
		private HandPoseId m_poseId;
	}
}
