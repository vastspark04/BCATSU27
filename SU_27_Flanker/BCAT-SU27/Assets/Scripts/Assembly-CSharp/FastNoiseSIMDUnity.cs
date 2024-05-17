using UnityEngine;

[AddComponentMenu("FastNoise/FastNoise SIMD Unity", 2)]
public class FastNoiseSIMDUnity : MonoBehaviour
{
	public FastNoiseSIMD fastNoiseSIMD = new FastNoiseSIMD();

	public string noiseName = "Default Noise";

	public int seed = 1337;

	public float frequency = 0.01f;

	public FastNoiseSIMD.NoiseType noiseType = FastNoiseSIMD.NoiseType.Simplex;

	public Vector3 axisScales = Vector3.one;

	public int octaves = 3;

	public float lacunarity = 2f;

	public float gain = 0.5f;

	public FastNoiseSIMD.FractalType fractalType;

	public FastNoiseSIMD.CellularDistanceFunction cellularDistanceFunction;

	public FastNoiseSIMD.CellularReturnType cellularReturnType = FastNoiseSIMD.CellularReturnType.Distance;

	public FastNoiseSIMD.NoiseType cellularNoiseLookupType = FastNoiseSIMD.NoiseType.Simplex;

	public float cellularNoiseLookupFrequency = 0.2f;

	public int cellularDistanceIndex0;

	public int cellularDistanceIndex1 = 1;

	public float cellularJitter = 0.45f;

	public FastNoiseSIMD.PerturbType perturbType;

	public float perturbAmp = 1f;

	public float perturbFrequency = 0.5f;

	public float perturbNormaliseLength = 1f;

	public int perturbOctaves = 3;

	public float perturbLacunarity = 2f;

	public float perturbGain = 0.5f;

	private void Awake()
	{
		SaveSettings();
	}

	public void SaveSettings()
	{
		fastNoiseSIMD.SetSeed(seed);
		fastNoiseSIMD.SetFrequency(frequency);
		fastNoiseSIMD.SetNoiseType(noiseType);
		fastNoiseSIMD.SetAxisScales(axisScales.x, axisScales.y, axisScales.z);
		fastNoiseSIMD.SetFractalOctaves(octaves);
		fastNoiseSIMD.SetFractalLacunarity(lacunarity);
		fastNoiseSIMD.SetFractalGain(gain);
		fastNoiseSIMD.SetFractalType(fractalType);
		fastNoiseSIMD.SetCellularDistanceFunction(cellularDistanceFunction);
		fastNoiseSIMD.SetCellularReturnType(cellularReturnType);
		fastNoiseSIMD.SetCellularNoiseLookupType(cellularNoiseLookupType);
		fastNoiseSIMD.SetCellularNoiseLookupFrequency(cellularNoiseLookupFrequency);
		fastNoiseSIMD.SetCellularDistance2Indicies(cellularDistanceIndex0, cellularDistanceIndex1);
		fastNoiseSIMD.SetCellularJitter(cellularJitter);
		fastNoiseSIMD.SetPerturbType(perturbType);
		fastNoiseSIMD.SetPerturbFrequency(perturbFrequency);
		fastNoiseSIMD.SetPerturbAmp(perturbAmp);
		fastNoiseSIMD.SetPerturbFractalOctaves(perturbOctaves);
		fastNoiseSIMD.SetPerturbFractalLacunarity(perturbLacunarity);
		fastNoiseSIMD.SetPerturbFractalGain(perturbGain);
		fastNoiseSIMD.SetPerturbNormaliseLength(perturbNormaliseLength);
	}
}
