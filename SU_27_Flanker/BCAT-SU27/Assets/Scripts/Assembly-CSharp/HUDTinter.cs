using UnityEngine;

public class HUDTinter : MonoBehaviour
{
	public float minTint;

	public float maxTint;

	public string colorPropertyName;

	public Renderer hudRenderer;

	private MaterialPropertyBlock properties;

	private int propertyID;

	private Color currentColor;

	private float currTint = -1f;

	private float tgtTint;

	private void Start()
	{
		currentColor = hudRenderer.sharedMaterial.GetColor(colorPropertyName);
		currTint = currentColor.a;
		properties = new MaterialPropertyBlock();
		hudRenderer.GetPropertyBlock(properties);
		propertyID = Shader.PropertyToID(colorPropertyName);
	}

	public void SetNormalizedTint(float nTint)
	{
		tgtTint = nTint;
	}

	private void Update()
	{
		if (currTint != tgtTint)
		{
			currTint = Mathf.Lerp(currTint, tgtTint, 8f * Time.deltaTime);
			float a = Mathf.Lerp(minTint, maxTint, currTint);
			currentColor.a = a;
			SetTint();
		}
	}

	private void SetTint()
	{
		properties.SetColor(propertyID, currentColor);
		hudRenderer.SetPropertyBlock(properties);
	}
}
