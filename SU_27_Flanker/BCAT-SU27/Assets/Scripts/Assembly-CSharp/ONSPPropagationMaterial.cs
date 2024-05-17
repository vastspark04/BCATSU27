using UnityEngine;
using System;
using System.Collections.Generic;

public class ONSPPropagationMaterial : MonoBehaviour
{
	[Serializable]
	public class Point
	{
		public float frequency;
		public float data;
	}

	[Serializable]
	public class Spectrum
	{
		public int selection;
		public List<ONSPPropagationMaterial.Point> points;
	}

	public enum Preset
	{
		Custom = 0,
		AcousticTile = 1,
		Brick = 2,
		BrickPainted = 3,
		Carpet = 4,
		CarpetHeavy = 5,
		CarpetHeavyPadded = 6,
		CeramicTile = 7,
		Concrete = 8,
		ConcreteRough = 9,
		ConcreteBlock = 10,
		ConcreteBlockPainted = 11,
		Curtain = 12,
		Foliage = 13,
		Glass = 14,
		GlassHeavy = 15,
		Grass = 16,
		Gravel = 17,
		GypsumBoard = 18,
		PlasterOnBrick = 19,
		PlasterOnConcreteBlock = 20,
		Soil = 21,
		SoundProof = 22,
		Snow = 23,
		Steel = 24,
		Water = 25,
		WoodThin = 26,
		WoodThick = 27,
		WoodFloor = 28,
		WoodOnConcrete = 29,
	}

	public Spectrum absorption;
	public Spectrum transmission;
	public Spectrum scattering;
	[SerializeField]
	private Preset preset_;
}
