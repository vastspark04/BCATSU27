using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPicker : EMUI
{
	private Texture2D m_ColorField;

	private RectTransform m_RectTransform;

	private Rect m_Rect;

	private Canvas m_Canvas;

	private Slider m_IntensitySlider;

	private bool m_UILockInstigator;

	[SerializeField]
	private LightRig m_LightRig;

	[SerializeField]
	private Image m_KnobImage;

	[SerializeField]
	private RectTransform m_KnobTransform;

	private void Start()
	{
		m_ColorField = base.gameObject.GetComponent<Image>().sprite.texture;
		m_RectTransform = base.gameObject.GetComponent<RectTransform>();
		m_Rect = m_RectTransform.rect;
		m_Canvas = GetComponentInParent<Canvas>();
		m_IntensitySlider = GetComponentInChildren<Slider>();
		SetCurrentColor();
		SetCurrentIntensity();
	}

	private void Update()
	{
		if (Input.GetMouseButton(0) && !EMUI.UIHelpOverlay && (!EMUI.UIClicked || (EMUI.UIClicked && m_UILockInstigator)))
		{
			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.position = Input.mousePosition;
			List<RaycastResult> list = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointerEventData, list);
			if (list.Count > 0 && list[0].gameObject == base.gameObject)
			{
				Vector2 vector = (list[0].screenPosition - new Vector2(m_RectTransform.position.x, m_RectTransform.position.y)) / m_Canvas.scaleFactor;
				m_KnobImage.color = m_ColorField.GetPixel((int)(vector.x / m_Rect.width * (float)m_ColorField.width), (int)(vector.y / m_Rect.height * (float)m_ColorField.height));
				m_KnobTransform.localPosition = new Vector3(vector.x, vector.y, m_KnobTransform.localPosition.z);
				SetCurrentColor();
			}
			EMUI.UIClicked = true;
			m_UILockInstigator = true;
		}
		if (Input.GetMouseButtonUp(0) && m_UILockInstigator)
		{
			EMUI.UIClicked = false;
			m_UILockInstigator = false;
		}
	}

	public Color GetCurrentColor()
	{
		if ((bool)m_KnobImage)
		{
			return m_KnobImage.color;
		}
		return Color.white;
	}

	public void SetCurrentColor()
	{
		if ((bool)m_LightRig && (bool)m_KnobImage)
		{
			for (int i = 0; i < m_LightRig.m_Lights.Length; i++)
			{
				m_LightRig.m_Lights[i].color = m_KnobImage.color;
			}
		}
	}

	public float GetCurrentIntensity()
	{
		if ((bool)m_IntensitySlider)
		{
			return m_IntensitySlider.value;
		}
		return 1f;
	}

	public void SetCurrentIntensity()
	{
		if ((bool)m_LightRig && (bool)m_IntensitySlider)
		{
			for (int i = 0; i < m_LightRig.m_Lights.Length; i++)
			{
				m_LightRig.m_Lights[i].intensity = m_IntensitySlider.value;
			}
		}
	}

	private bool CheckGUI()
	{
		bool result = false;
		if (Input.GetMouseButton(2))
		{
			PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
			pointerEventData.position = Input.mousePosition;
			List<RaycastResult> list = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointerEventData, list);
			result = list.Count <= 0 || list[0].gameObject.layer != 5;
		}
		return result;
	}
}
