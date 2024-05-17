using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class SDFCloudMesh : MonoBehaviour
{
	public string sdfTexturePath;

	public int resolution = 128;

	[ContextMenu("Set Textures")]
	private void SetTextures()
	{
		Texture3D noiseTexture = new Texture3D(resolution, resolution, resolution, DefaultFormat.LDR, TextureCreationFlags.None);
		CloudNoiseGen.LoadNoise(ref noiseTexture, sdfTexturePath, resolution);
		GetComponent<MeshRenderer>().sharedMaterial.SetTexture("_CloudSDF", noiseTexture);
	}
}
