using System;
using UnityEngine;

namespace OVR
{
	[Serializable]
	public class OSPProps
	{
		public bool enableSpatialization;
		public bool useFastOverride;
		public float gain;
		public bool enableInvSquare;
		public float volumetric;
		public Vector2 invSquareFalloff;
	}
}
