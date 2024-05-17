using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class CachedTransform
	{
		public Vector3 position;
		public Vector3 posOffset;
		public Quaternion rotation;
		public Vector3 scale;
		public float positionYOld;
	}
}
