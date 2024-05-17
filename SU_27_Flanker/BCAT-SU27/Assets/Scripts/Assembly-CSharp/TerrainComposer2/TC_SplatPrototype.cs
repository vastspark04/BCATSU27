using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class TC_SplatPrototype
	{
		public Texture2D texture;
		public Texture2D normalMap;
		public float metallic;
		public float smoothness;
		public Vector2 tileOffset;
		public Vector2 tileSize;
	}
}
