using UnityEngine;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteAlways]
	[AddComponentMenu("Image Effects/Color Adjustments/Grayscale")]
	public class Grayscale : ImageEffectBase
	{
		public Texture textureRamp;

		[Range(-1f, 1f)]
		public float rampOffset;

		private void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
		
		}
	}
}
