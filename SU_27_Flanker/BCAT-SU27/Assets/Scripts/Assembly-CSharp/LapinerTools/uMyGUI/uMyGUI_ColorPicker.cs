using UnityEngine;
using UnityEngine.UI;

namespace LapinerTools.uMyGUI
{
	public class uMyGUI_ColorPicker : MonoBehaviour
	{
		[SerializeField]
		private Slider m_redSlider;
		[SerializeField]
		private Slider m_greenSlider;
		[SerializeField]
		private Slider m_blueSlider;
		[SerializeField]
		private Color m_pickedColor;
		[SerializeField]
		private Graphic m_colorPreview;
	}
}
