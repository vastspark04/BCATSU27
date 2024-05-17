using UnityEngine;

namespace OC.ExampleContent{

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class Tonemap : MonoBehaviour
{
	[SerializeField]
	private Shader shader;

	[SerializeField]
	private float exposure = 1f;

	[SerializeField]
	private float gamma = 2f;

	private Material _material;

	private Material material
	{
		get
		{
			if (!_material)
			{
				_material = new Material(shader);
			}
			return _material;
		}
	}

	protected void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!material)
		{
			Graphics.Blit(source, destination);
			return;
		}
		material.SetFloat("_Exposure", exposure);
		material.SetFloat("_Gamma", gamma);
		Graphics.Blit(source, destination, material);
	}
}
}