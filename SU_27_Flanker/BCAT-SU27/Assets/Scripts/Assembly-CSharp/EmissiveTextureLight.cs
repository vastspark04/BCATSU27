using UnityEngine;

public class EmissiveTextureLight : ElectronicComponent
{
	public float lerpRate = 10f;

	public Renderer[] mrs;

	public string propName = "_EmissionColor";

	private MaterialPropertyBlock properties;

	public Color startColor = Color.black;

	public Color toggleColor = Color.green;

	private Color currentColor;

	private bool colorDirty;

	private Color targetColor;

	private int propID;

	private bool toggledOn;

	private bool wasPowered = true;

	private float brightness = 1f;

	private bool colorWasSet;

	private void Awake()
	{
		currentColor = startColor;
		if (!colorWasSet)
		{
			targetColor = startColor;
		}
		colorDirty = true;
		OnAwake();
	}

	protected virtual void OnAwake()
	{
		if (mrs == null || mrs.Length == 0)
		{
			mrs = new Renderer[1] { GetComponent<MeshRenderer>() };
		}
		properties = new MaterialPropertyBlock();
		propID = Shader.PropertyToID(propName);
	}

	private void Update()
	{
		bool flag = !battery || DrainElectricity(0.001f);
		Color b = (flag ? Color.Lerp(startColor, targetColor, brightness) : startColor);
		if (wasPowered != flag)
		{
			colorDirty = true;
			wasPowered = flag;
		}
		if (colorDirty)
		{
			currentColor = Color.Lerp(currentColor, b, lerpRate * Time.deltaTime);
			SetRendererColor(currentColor);
			if (currentColor == targetColor)
			{
				colorDirty = false;
			}
		}
	}

	protected virtual void SetRendererColor(Color c)
	{
		properties.SetColor(propID, c);
		for (int i = 0; i < mrs.Length; i++)
		{
			if ((bool)mrs[i])
			{
				mrs[i].SetPropertyBlock(properties);
			}
		}
	}

	public void SetColor(Color c)
	{
		colorWasSet = true;
		targetColor = c;
		colorDirty = true;
	}

	public void SetBrightness(float b)
	{
		brightness = b;
		colorDirty = true;
	}

	public void SetStatus(int st)
	{
		if (st > 0 && !toggledOn)
		{
			toggledOn = true;
			SetColor(toggleColor);
		}
		else if (st == 0 && toggledOn)
		{
			toggledOn = false;
			SetColor(startColor);
		}
	}

	public void Toggle()
	{
		if (toggledOn)
		{
			SetColor(startColor);
		}
		else
		{
			SetColor(toggleColor);
		}
		toggledOn = !toggledOn;
	}
}
