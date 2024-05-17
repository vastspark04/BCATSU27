using System;

namespace TerrainComposer2
{
	[Serializable]
	public class Noise
	{
		public NoiseMode mode;
		public CellNoiseMode cellMode;
		public float frequency;
		public float lacunarity;
		public int octaves;
		public float persistence;
		public float seed;
		public float amplitude;
		public float warp0;
		public float warp;
		public float damp0;
		public float damp;
		public float dampScale;
		public int cellType;
		public int distanceFunction;
	}
}
