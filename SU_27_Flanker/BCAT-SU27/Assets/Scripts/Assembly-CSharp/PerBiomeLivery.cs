using System;
using UnityEngine;
using VTOLVR.Multiplayer;

public class PerBiomeLivery : MonoBehaviour
{
	[Serializable]
	public class BiomeLivery
	{
		public MapGenBiome.Biomes biome;

		public Texture2D livery;
	}

	public AircraftLiveryApplicator applicator;

	public BiomeLivery[] liveries;

	private void Start()
	{
		if (!VTOLMPUtils.IsMultiplayer() && (bool)VTMapGenerator.fetch)
		{
			SetBiome(VTMapGenerator.fetch.biome);
		}
	}

	public void SetBiome(MapGenBiome.Biomes biome)
	{
		BiomeLivery[] array = liveries;
		foreach (BiomeLivery biomeLivery in array)
		{
			if (biomeLivery.biome == biome)
			{
				applicator.ApplyLivery(biomeLivery.livery);
			}
		}
	}
}
