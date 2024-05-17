using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_SliderSynchronizer : MonoBehaviour
	{
		[SerializeField]
		private Slider[] m_sliders;
		[SerializeField]
		private bool m_isSynchronizeOnStart;
		[SerializeField]
		private float m_value;
	}
}
