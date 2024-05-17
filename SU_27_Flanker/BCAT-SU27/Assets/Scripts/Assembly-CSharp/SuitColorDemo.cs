using UnityEngine;

public class SuitColorDemo : MonoBehaviour
{
	private struct CyclicColor
	{
		private Color c;

		public CyclicColor(float tOffset, float speed, float saturation)
		{
			float num = Mathf.Ceil((Time.time * speed + tOffset) * 2f) / 2f;
			float r = (VectorUtils.Triangle(num) + 2f) / 4f;
			float g = (VectorUtils.Triangle(num * 0.79f + 5.32f) + 2f) / 4f;
			float b = (VectorUtils.Triangle(num * 0.83f + 8.96f) + 2f) / 4f;
			c = new Color(r, g, b, 1f);
			c = Color.Lerp(c.grayscale * Color.white, c, saturation);
		}

		public Color GetColor()
		{
			return c;
		}
	}

	public Renderer suitRenderer;

	[Range(0f, 1f)]
	public float saturation;

	public float speed = 1f;

	private MaterialPropertyBlock props;

	private int idBase;

	private int idR;

	private int idG;

	private int idB;

	private void Start()
	{
		props = new MaterialPropertyBlock();
		idBase = Shader.PropertyToID("_BaseColor");
		idR = Shader.PropertyToID("_ColorR");
		idG = Shader.PropertyToID("_ColorG");
		idB = Shader.PropertyToID("_ColorB");
	}

	private void Update()
	{
		Color color = new CyclicColor(0f, speed, saturation).GetColor();
		Color color2 = new CyclicColor(1.235f, speed, saturation).GetColor();
		Color color3 = new CyclicColor(9.345f, speed, saturation).GetColor();
		Color color4 = new CyclicColor(6.321f, speed, saturation).GetColor();
		props.SetColor(idBase, color);
		props.SetColor(idR, color2);
		props.SetColor(idG, color3);
		props.SetColor(idB, color4);
		suitRenderer.SetPropertyBlock(props);
	}
}
