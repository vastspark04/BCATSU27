using UnityEngine;

public class GenerateCloudTexture : MonoBehaviour
{
	public CloudNoiseGen.NoiseSettings perlin;

	public CloudNoiseGen.NoiseSettings worley;

	public int resolution;

	[Header("Resources/3DNoise/")]
	public string folderName;
}
