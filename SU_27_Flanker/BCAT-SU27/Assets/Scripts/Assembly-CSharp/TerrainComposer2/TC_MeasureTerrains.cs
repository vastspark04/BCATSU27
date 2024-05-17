using UnityEngine;

namespace TerrainComposer2
{
	public class TC_MeasureTerrains : MonoBehaviour
	{
		public bool locked;
		public Terrain terrain;
		public MeshRenderer mr;
		public float normalizedHeight;
		public float height;
		public float angle;
		public int textureSize;
		public Vector3 size;
		public int splatResolution;
		public Vector2 splatConversion;
		public Vector2 localPos;
		public int grassResolution;
		public Vector2 grassConversion;
		public Vector2 grassLocalPos;
		public bool drawSplat;
		public bool drawGrass;
	}
}
