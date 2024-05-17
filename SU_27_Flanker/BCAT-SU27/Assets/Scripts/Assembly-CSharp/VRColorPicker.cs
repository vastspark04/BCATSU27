using UnityEngine;
using UnityEngine.UI;

public class VRColorPicker : MonoBehaviour
{
	public Image previewImage;

	public RawImage hueImage;

	public RawImage saturationImage;

	public RawImage valueImage;

	public Transform hueSelectTf;

	public Transform satSelectTf;

	public Transform valSelectTf;

	private float barWidth;

	private float hue;

	private float saturation = 1f;

	private float value = 1f;

	private Color finalColor;

	public float adjustStep = 0.15f;

	private bool colorDirty;

	private float lastUpdateTime;

	public ColorChangedEvent onColorChanged;

	private Texture2D hueTex;

	private Texture2D saturationTex;

	private Texture2D valueTex;

	private void Awake()
	{
		barWidth = hueImage.rectTransform.rect.width;
		SetupHue();
		SetupSaturation();
		SetupValue();
		SetColor(Color.magenta);
	}

	public void HueUp()
	{
		hue += adjustStep * Time.deltaTime;
		hue = Mathf.Repeat(hue, 1f);
		UpdateSelectors();
		colorDirty = true;
	}

	public void HueDown()
	{
		hue -= adjustStep * Time.deltaTime;
		hue = Mathf.Repeat(hue, 1f);
		UpdateSelectors();
		colorDirty = true;
	}

	public void SaturationUp()
	{
		saturation = Mathf.Clamp01(saturation + adjustStep * Time.deltaTime);
		UpdateSelectors();
		colorDirty = true;
	}

	public void SaturationDown()
	{
		saturation = Mathf.Clamp01(saturation - adjustStep * Time.deltaTime);
		UpdateSelectors();
		colorDirty = true;
	}

	public void ValueUp()
	{
		value = Mathf.Clamp01(value + adjustStep * Time.deltaTime);
		UpdateSelectors();
		colorDirty = true;
	}

	public void ValueDown()
	{
		value = Mathf.Clamp01(value - adjustStep * Time.deltaTime);
		UpdateSelectors();
		colorDirty = true;
	}

	private void UpdateSelectors()
	{
		hueSelectTf.localPosition = new Vector3(hue * barWidth, 0f, 0f);
		satSelectTf.localPosition = new Vector3(saturation * barWidth, 0f, 0f);
		valSelectTf.localPosition = new Vector3(value * barWidth, 0f, 0f);
	}

	private void Update()
	{
		if (colorDirty && Time.time - lastUpdateTime > 0.2f)
		{
			lastUpdateTime = Time.time;
			colorDirty = false;
			UpdateColor();
			if (onColorChanged != null)
			{
				onColorChanged.Invoke(finalColor);
			}
		}
	}

	public void SetColor(Color c)
	{
		Color.RGBToHSV(c, out hue, out saturation, out value);
		UpdateColor();
		UpdateSelectors();
	}

	private void UpdateColor()
	{
		finalColor = Color.HSVToRGB(hue, saturation, value);
		previewImage.color = finalColor;
		UpdateSaturationTex();
		UpdateValueTex();
	}

	public Color GetFinalColor()
	{
		return finalColor;
	}

	private void SetupHue()
	{
		int num = 100;
		hueTex = new Texture2D(num, 1);
		for (int i = 0; i < num; i++)
		{
			Color color = Color.HSVToRGB((float)i / (float)num, 1f, 1f);
			hueTex.SetPixel(i, 0, color);
		}
		hueTex.Apply();
		hueImage.texture = hueTex;
	}

	private void SetupSaturation()
	{
		saturationTex = new Texture2D(100, 1);
		saturationImage.texture = saturationTex;
	}

	private void UpdateSaturationTex()
	{
		float num = saturationTex.width;
		for (int i = 0; (float)i < num; i++)
		{
			float s = (float)i / num;
			Color color = Color.HSVToRGB(hue, s, value);
			saturationTex.SetPixel(i, 0, color);
		}
		saturationTex.Apply();
	}

	private void SetupValue()
	{
		valueTex = new Texture2D(100, 1);
		valueImage.texture = valueTex;
	}

	private void UpdateValueTex()
	{
		float num = valueTex.width;
		for (int i = 0; (float)i < num; i++)
		{
			float v = (float)i / num;
			Color color = Color.HSVToRGB(hue, saturation, v);
			valueTex.SetPixel(i, 0, color);
		}
		valueTex.Apply();
	}
}
