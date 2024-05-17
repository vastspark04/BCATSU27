using UnityEngine;

namespace OC.ExampleContent{

[ExecuteInEditMode]
[ImageEffectAllowedInSceneView]
public class ScreenDroplets : MonoBehaviour
{
	[SerializeField]
	private OverCloudProbe overCloudProbe;

	[SerializeField]
	private Shader shader;

	[SerializeField]
	private Shader blurShader;

	[SerializeField]
	private float blurAmount = 1f;

	[SerializeField]
	private Texture2D blurMask;

	private Material _material;

	private Material _blurMaterial;

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

	private Material blurMaterial
	{
		get
		{
			if (!_blurMaterial)
			{
				_blurMaterial = new Material(blurShader);
			}
			return _blurMaterial;
		}
	}

	protected void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!material || !blurMaterial || !overCloudProbe)
		{
			Graphics.Blit(source, destination);
			return;
		}
		RenderTextureDescriptor descriptor = source.descriptor;
		descriptor.width /= 4;
		descriptor.height /= 4;
		RenderTexture temporary = RenderTexture.GetTemporary(descriptor);
		RenderTexture temporary2 = RenderTexture.GetTemporary(descriptor);
		Shader.SetGlobalVector("_PixelSize", new Vector2(1f / (float)temporary.width, 1f / (float)temporary.height));
		Shader.SetGlobalFloat("_BlurAmount", blurAmount);
		Graphics.Blit(source, temporary, blurMaterial, 0);
		Graphics.Blit(temporary, temporary2, blurMaterial, 1);
		material.SetTexture("_MainTexBlurred", temporary2);
		material.SetTexture("_BlurMask", blurMask);
		material.SetFloat("_Intensity", Application.isPlaying ? Mathf.Max(overCloudProbe.density, overCloudProbe.rain) : 0f);
		Graphics.Blit(source, destination, material, 0);
		RenderTexture.ReleaseTemporary(temporary);
		RenderTexture.ReleaseTemporary(temporary2);
	}
}
}