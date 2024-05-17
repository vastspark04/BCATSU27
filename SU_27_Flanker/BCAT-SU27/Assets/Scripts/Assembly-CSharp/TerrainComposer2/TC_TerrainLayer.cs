using System.Collections.Generic;
using UnityEngine;

namespace TerrainComposer2
{
	public class TC_TerrainLayer : TC_ItemBehaviour
	{
		public List<TC_SelectItem> objectSelectItems;
		public List<TC_SelectItem> treeSelectItems;
		public float treeResolutionPM;
		public float objectResolutionPM;
		public Vector2 objectAreaSize;
		public Transform objectTransform;
		public int colormapResolution;
		public int meshResolution;
		public float seedChild;
	}
}
