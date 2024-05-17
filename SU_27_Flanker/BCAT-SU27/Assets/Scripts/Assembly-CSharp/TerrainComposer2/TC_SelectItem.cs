using System;
using UnityEngine;

namespace TerrainComposer2
{
	public class TC_SelectItem : TC_ItemBehaviour
	{
		[Serializable]
		public class Tree
		{
			public float randomPosition;
			public float heightOffset;
			public Vector2 scaleRange;
			public float nonUniformScale;
			public float scaleMulti;
			public AnimationCurve scaleCurve;
		}

		[Serializable]
		public class SpawnObject
		{
			public GameObject go;
			public bool linkToPrefab;
			public float randomPosition;
			public Vector2 heightRange;
			public bool includeScale;
			public float heightOffset;
			public bool includeTerrainHeight;
			public Vector2 rotRangeX;
			public Vector2 rotRangeY;
			public Vector2 rotRangeZ;
			public bool isSnapRot;
			public bool isSnapRotX;
			public bool isSnapRotY;
			public bool isSnapRotZ;
			public float snapRotX;
			public float snapRotY;
			public float snapRotZ;
			public bool customScaleRange;
			public Vector2 scaleRangeX;
			public Vector2 scaleRangeY;
			public Vector2 scaleRangeZ;
			public Vector2 scaleRange;
			public float scaleMulti;
			public float nonUniformScale;
			public AnimationCurve scaleCurve;
			public Transform lookAtTarget;
			public bool lookAtX;
		}

		public Vector2 range;
		public GameObject oldSpawnObject;
		public Tree tree;
		public SpawnObject spawnObject;
		public Color color;
		public int selectIndex;
		public float[] splatCustomValues;
		public bool splatCustom;
		public float splatCustomTotal;
		public int globalListIndex;
	}
}
