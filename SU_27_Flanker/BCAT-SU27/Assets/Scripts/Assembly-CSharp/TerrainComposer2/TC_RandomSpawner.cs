using UnityEngine;

namespace TerrainComposer2
{
	public class TC_RandomSpawner : MonoBehaviour
	{
		public GameObject spawnObject;
		public float posOffsetY;
		public Vector2 posRangeX;
		public Vector2 posRangeZ;
		public Vector2 rotRangeY;
		public bool spawnOnStart;
	}
}
