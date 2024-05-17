using UnityEngine;

namespace OVRTouchSample
{
	public class Hand : MonoBehaviour
	{
		[SerializeField]
		private OVRInput.Controller m_controller;
		[SerializeField]
		private Animator m_animator;
		[SerializeField]
		private HandPose m_defaultGrabPose;
	}
}
