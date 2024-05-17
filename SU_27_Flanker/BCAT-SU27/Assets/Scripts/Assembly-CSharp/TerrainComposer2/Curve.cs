using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class Curve
	{
		public bool active;
		public Vector2 range;
		public AnimationCurve curve;
		public Vector4[] c;
		public float[] curveKeys;
		public int length;
	}
}
