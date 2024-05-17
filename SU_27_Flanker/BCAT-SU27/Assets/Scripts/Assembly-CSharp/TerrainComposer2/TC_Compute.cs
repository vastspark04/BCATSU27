using UnityEngine;

namespace TerrainComposer2
{
	public class TC_Compute : MonoBehaviour
	{
		public TC_CamCapture camCapture;
		public Transform target;
		public bool run;
		public ComputeShader shader;
		public string path;
		public int collisionMask;
		public PerlinNoise m_perlin;
		public int terrainHeightKernel;
		public int terrainAngleKernel;
		public int terrainSplatmap0Kernel;
		public int terrainSplatmap1Kernel;
		public int terrainConvexityKernel;
		public int terrainCollisionHeightKernel;
		public int terrainCollisionHeightIncludeKernel;
		public int terrainCollisionMaskKernel;
		public RenderTexture[] rtsColor;
		public RenderTexture[] rtsSplatmap;
		public RenderTexture[] rtsResult;
		public RenderTexture rtResult;
		public RenderTexture rtSplatPreview;
		public Texture2D[] texGrassmaps;
		public Vector4[] splatColors;
		public Vector4[] colors;
	}
}
