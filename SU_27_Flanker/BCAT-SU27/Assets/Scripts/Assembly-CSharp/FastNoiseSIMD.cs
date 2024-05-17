using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class FastNoiseSIMD
{
	public enum NoiseType
	{
		Value,
		ValueFractal,
		Perlin,
		PerlinFractal,
		Simplex,
		SimplexFractal,
		WhiteNoise,
		Cellular,
		Cubic,
		CubicFractal
	}

	public enum FractalType
	{
		FBM,
		Billow,
		RigidMulti
	}

	public enum CellularDistanceFunction
	{
		Euclidean,
		Manhattan,
		Natural
	}

	public enum CellularReturnType
	{
		CellValue,
		Distance,
		Distance2,
		Distance2Add,
		Distance2Sub,
		Distance2Mul,
		Distance2Div,
		NoiseLookup,
		Distance2Cave
	}

	public enum PerturbType
	{
		None,
		Gradient,
		GradientFractal,
		Normalise,
		Gradient_Normalise,
		GradientFractal_Normalise
	}

	public class VectorSet
	{
		internal readonly IntPtr nativePointer;

		public VectorSet(Vector3[] vectors, int sampleSizeX = -1, int sampleSizeY = -1, int sampleSizeZ = -1, int samplingScale = 0)
		{
			float[] array = new float[vectors.Length * 3];
			for (int i = 0; i < vectors.Length; i++)
			{
				array[i] = vectors[i].x;
				array[i + vectors.Length] = vectors[i].y;
				array[i + vectors.Length * 2] = vectors[i].z;
			}
			nativePointer = NewVectorSet(array, array.Length, samplingScale, sampleSizeX, sampleSizeY, sampleSizeZ);
		}

		~VectorSet()
		{
			NativeFreeVectorSet(nativePointer);
		}

		[DllImport("FastNoiseSIMD_CLib")]
		private static extern IntPtr NewVectorSet(float[] vectorSetArray, int arraySize, int samplingScale, int sampleSizeX, int sampleSizeY, int sampleSizeZ);

		[DllImport("FastNoiseSIMD_CLib")]
		private static extern void NativeFreeVectorSet(IntPtr nativePointer);
	}

	private readonly IntPtr nativePointer;

	private const string NATIVE_LIB = "FastNoiseSIMD_CLib";

	public FastNoiseSIMD(int seed = 1337)
	{
		nativePointer = NewFastNoiseSIMD(seed);
	}

	~FastNoiseSIMD()
	{
		NativeFree(nativePointer);
	}

	public int GetSeed()
	{
		return NativeGetSeed(nativePointer);
	}

	public void SetSeed(int seed)
	{
		NativeSetSeed(nativePointer, seed);
	}

	public void SetFrequency(float frequency)
	{
		NativeSetFrequency(nativePointer, frequency);
	}

	public void SetNoiseType(NoiseType noiseType)
	{
		NativeSetNoiseType(nativePointer, (int)noiseType);
	}

	public void SetAxisScales(float xScale, float yScale, float zScale)
	{
		NativeSetAxisScales(nativePointer, xScale, yScale, zScale);
	}

	public void SetFractalOctaves(int octaves)
	{
		NativeSetFractalOctaves(nativePointer, octaves);
	}

	public void SetFractalLacunarity(float lacunarity)
	{
		NativeSetFractalLacunarity(nativePointer, lacunarity);
	}

	public void SetFractalGain(float gain)
	{
		NativeSetFractalGain(nativePointer, gain);
	}

	public void SetFractalType(FractalType fractalType)
	{
		NativeSetFractalType(nativePointer, (int)fractalType);
	}

	public void SetCellularReturnType(CellularReturnType cellularReturnType)
	{
		NativeSetCellularReturnType(nativePointer, (int)cellularReturnType);
	}

	public void SetCellularDistanceFunction(CellularDistanceFunction cellularDistanceFunction)
	{
		NativeSetCellularDistanceFunction(nativePointer, (int)cellularDistanceFunction);
	}

	public void SetCellularNoiseLookupType(NoiseType cellularNoiseLookupType)
	{
		NativeSetCellularNoiseLookupType(nativePointer, (int)cellularNoiseLookupType);
	}

	public void SetCellularNoiseLookupFrequency(float cellularNoiseLookupFrequency)
	{
		NativeSetCellularNoiseLookupFrequency(nativePointer, cellularNoiseLookupFrequency);
	}

	public void SetCellularDistance2Indicies(int cellularDistanceIndex0, int cellularDistanceIndex1)
	{
		NativeSetCellularDistance2Indicies(nativePointer, cellularDistanceIndex0, cellularDistanceIndex1);
	}

	public void SetCellularJitter(float cellularJitter)
	{
		NativeSetCellularJitter(nativePointer, cellularJitter);
	}

	public void SetPerturbType(PerturbType perturbType)
	{
		NativeSetPerturbType(nativePointer, (int)perturbType);
	}

	public void SetPerturbFrequency(float perturbFreq)
	{
		NativeSetPerturbFrequency(nativePointer, perturbFreq);
	}

	public void SetPerturbAmp(float perturbAmp)
	{
		NativeSetPerturbAmp(nativePointer, perturbAmp);
	}

	public void SetPerturbFractalOctaves(int perturbOctaves)
	{
		NativeSetPerturbFractalOctaves(nativePointer, perturbOctaves);
	}

	public void SetPerturbFractalLacunarity(float perturbFractalLacunarity)
	{
		NativeSetPerturbFractalLacunarity(nativePointer, perturbFractalLacunarity);
	}

	public void SetPerturbFractalGain(float perturbFractalGain)
	{
		NativeSetPerturbFractalGain(nativePointer, perturbFractalGain);
	}

	public void SetPerturbNormaliseLength(float perturbNormaliseLength)
	{
		NativeSetPerturbNormaliseLength(nativePointer, perturbNormaliseLength);
	}

	public void FillNoiseSet(float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, float scaleModifier = 1f)
	{
		NativeFillNoiseSet(nativePointer, noiseSet, xStart, yStart, zStart, xSize, ySize, zSize, scaleModifier);
	}

	public void FillSampledNoiseSet(float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, int sampleScale)
	{
		NativeFillSampledNoiseSet(nativePointer, noiseSet, xStart, yStart, zStart, xSize, ySize, zSize, sampleScale);
	}

	public void FillNoiseSetVector(float[] noiseSet, VectorSet vectorSet, float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
	{
		NativeFillNoiseSetVector(nativePointer, noiseSet, vectorSet.nativePointer, xOffset, yOffset, zOffset);
	}

	public void FillSampledNoiseSetVector(float[] noiseSet, VectorSet vectorSet, float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
	{
		NativeFillSampledNoiseSetVector(nativePointer, noiseSet, vectorSet.nativePointer, xOffset, yOffset, zOffset);
	}

	public float[] GetNoiseSet(int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, float scaleModifier = 1f)
	{
		float[] emptyNoiseSet = GetEmptyNoiseSet(xSize, ySize, zSize);
		NativeFillNoiseSet(nativePointer, emptyNoiseSet, xStart, yStart, zStart, xSize, ySize, zSize, scaleModifier);
		return emptyNoiseSet;
	}

	public float[] GetSampledNoiseSet(int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, int sampleScale)
	{
		float[] emptyNoiseSet = GetEmptyNoiseSet(xSize, ySize, zSize);
		NativeFillSampledNoiseSet(nativePointer, emptyNoiseSet, xStart, yStart, zStart, xSize, ySize, zSize, sampleScale);
		return emptyNoiseSet;
	}

	public float[] GetEmptyNoiseSet(int xSize, int ySize, int zSize)
	{
		return new float[xSize * ySize * zSize];
	}

	[DllImport("FastNoiseSIMD_CLib")]
	public static extern int GetSIMDLevel();

	[DllImport("FastNoiseSIMD_CLib")]
	public static extern void SetSIMDLevel(int level);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern IntPtr NewFastNoiseSIMD(int seed);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeFree(IntPtr nativePointer);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetSeed(IntPtr nativePointer, int seed);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern int NativeGetSeed(IntPtr nativePointer);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetFrequency(IntPtr nativePointer, float freq);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetNoiseType(IntPtr nativePointer, int noiseType);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetAxisScales(IntPtr nativePointer, float xScale, float yScale, float zScale);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetFractalOctaves(IntPtr nativePointer, int octaves);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetFractalLacunarity(IntPtr nativePointer, float lacunarity);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetFractalGain(IntPtr nativePointer, float gain);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetFractalType(IntPtr nativePointer, int fractalType);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularDistanceFunction(IntPtr nativePointer, int distanceFunction);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularReturnType(IntPtr nativePointer, int returnType);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularNoiseLookupType(IntPtr nativePointer, int noiseType);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularNoiseLookupFrequency(IntPtr nativePointer, float freq);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularDistance2Indicies(IntPtr nativePointer, int cellularDistanceIndex0, int cellularDistanceIndex1);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetCellularJitter(IntPtr nativePointer, float cellularJitter);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbType(IntPtr nativePointer, int perturbType);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbFrequency(IntPtr nativePointer, float perturbFreq);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbAmp(IntPtr nativePointer, float perturbAmp);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbFractalOctaves(IntPtr nativePointer, int perturbOctaves);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbFractalLacunarity(IntPtr nativePointer, float perturbFractalLacunarity);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbFractalGain(IntPtr nativePointer, float perturbFractalGain);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeSetPerturbNormaliseLength(IntPtr nativePointer, float perturbNormaliseLength);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeFillNoiseSet(IntPtr nativePointer, float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, float scaleModifier);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeFillSampledNoiseSet(IntPtr nativePointer, float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize, int zSize, int sampleScale);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeFillNoiseSetVector(IntPtr nativePointer, float[] noiseSet, IntPtr vectorSetPointer, float xOffset, float yOffset, float zOffset);

	[DllImport("FastNoiseSIMD_CLib")]
	private static extern void NativeFillSampledNoiseSetVector(IntPtr nativePointer, float[] noiseSet, IntPtr vectorSetPointer, float xOffset, float yOffset, float zOffset);
}
