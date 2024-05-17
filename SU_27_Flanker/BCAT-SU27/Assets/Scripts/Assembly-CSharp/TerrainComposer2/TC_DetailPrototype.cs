using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class TC_DetailPrototype
	{
		public bool usePrototypeMesh;
		public float bendFactor;
		public Color dryColor;
		public Color healthyColor;
		public float maxHeight;
		public float maxWidth;
		public float minHeight;
		public float minWidth;
		public float noiseSpread;
		public GameObject prototype;
		public Texture2D prototypeTexture;
		public DetailRenderMode renderMode;
	}
}
