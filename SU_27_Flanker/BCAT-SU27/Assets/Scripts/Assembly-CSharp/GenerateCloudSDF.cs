using UnityEngine;

public class GenerateCloudSDF : MonoBehaviour
{
	public string noiseFolder;

	public int resolution;

	public IntVector2 zRange;

	public float volumeThreshold = 0.35f;

	public float maxDist = 0.2f;

	public string outputPath;
}
