using System;
using UnityEngine;

namespace TerrainComposer2
{
	[Serializable]
	public class TC_Terrain
	{
		public Transform t;
		public Vector3 newPosition;
		public int tasks;
		public TC_Node[] nodes;
		public Material rtpMat;
		public RenderTexture rtHeight;
		public Texture2D texHeight;
		public Texture2D texColormap;
	}
}
