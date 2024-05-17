using UnityEngine;
using UnityEngine.UI;

public class VTMapPreviewSetup : MonoBehaviour
{
	public Material borealMat;

	public Material desertMat;

	public Material arcticMat;

	public RawImage rawImg;

	public Camera mapCam;

	public Texture2D ShootMap(VTMapCustom map, int size)
	{
		Material material = map.biome switch
		{
			MapGenBiome.Biomes.Arctic => arcticMat, 
			MapGenBiome.Biomes.Desert => desertMat, 
			_ => borealMat, 
		};
		rawImg.texture = map.heightMap;
		rawImg.material = material;
		RenderTexture temporary = RenderTexture.GetTemporary(size, size, 16);
		mapCam.targetTexture = temporary;
		mapCam.Render();
		mapCam.targetTexture = null;
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		Texture2D texture2D = new Texture2D(size, size);
		texture2D.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
		RenderTexture.active = active;
		temporary.Release();
		return texture2D;
	}
}
