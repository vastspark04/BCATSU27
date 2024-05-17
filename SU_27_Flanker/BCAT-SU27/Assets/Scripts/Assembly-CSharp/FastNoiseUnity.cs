using UnityEngine;

[AddComponentMenu("FastNoise/FastNoise Unity", 1)]
public class FastNoiseUnity : MonoBehaviour
{
	public FastNoise fastNoise = new FastNoise();

	public string noiseName = "Default Noise";

	public int seed = 1337;

	public float frequency = 0.01f;

	public FastNoise.Interp interp = FastNoise.Interp.Quintic;

	public FastNoise.NoiseType noiseType = FastNoise.NoiseType.Simplex;

	public int octaves = 3;

	public float lacunarity = 2f;

	public float gain = 0.5f;

	public FastNoise.FractalType fractalType;

	public FastNoise.CellularDistanceFunction cellularDistanceFunction;

	public FastNoise.CellularReturnType cellularReturnType;

	public FastNoiseUnity cellularNoiseLookup;

	public int cellularDistanceIndex0;

	public int cellularDistanceIndex1 = 1;

	public float cellularJitter = 0.45f;

	public float gradientPerturbAmp = 1f;

	private void Awake()
	{
		SaveSettings();
	}

	public void SaveSettings()
	{
		fastNoise.SetSeed(seed);
		fastNoise.SetFrequency(frequency);
		fastNoise.SetInterp(interp);
		fastNoise.SetNoiseType(noiseType);
		fastNoise.SetFractalOctaves(octaves);
		fastNoise.SetFractalLacunarity(lacunarity);
		fastNoise.SetFractalGain(gain);
		fastNoise.SetFractalType(fractalType);
		fastNoise.SetCellularDistanceFunction(cellularDistanceFunction);
		fastNoise.SetCellularReturnType(cellularReturnType);
		fastNoise.SetCellularJitter(cellularJitter);
		fastNoise.SetCellularDistance2Indicies(cellularDistanceIndex0, cellularDistanceIndex1);
		if ((bool)cellularNoiseLookup)
		{
			fastNoise.SetCellularNoiseLookup(cellularNoiseLookup.fastNoise);
		}
		fastNoise.SetGradientPerturbAmp(gradientPerturbAmp);
	}
}
