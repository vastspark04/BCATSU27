using System;
using UnityEngine;

[Serializable]
public class MapGenBiome
{
	public enum Biomes
	{
		Boreal,
		Desert,
		Arctic,
		Tropical
	}

	[Serializable]
	public class BiomeTreeProfile
	{
		public Mesh[] meshes;

		public Mesh[] lowPolyMeshes;

		public Vector3[] colliderSizes;

		public Material treeMaterial;

		public Material billboardTreeMaterial;

		public int treesPerTri;

		public MinMax treeScale = new MinMax(1.5f, 2.5f);

		public float[] overrideLodSizes;
	}

	public Biomes biome;

	public Material terrainMaterial;

	public Material editorOOBMaterial;

	public WheelSurfaceMaterial defaultSurfaceMaterial;

	public BiomeTreeProfile treeProfile;
}
