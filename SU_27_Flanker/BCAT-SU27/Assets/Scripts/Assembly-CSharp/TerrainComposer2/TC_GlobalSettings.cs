using UnityEngine;

namespace TerrainComposer2
{
	public class TC_GlobalSettings : ScriptableObject
	{
		public bool tooltip;
		public Vector3 defaultTerrainSize;
		public bool SavePreviewTextures;
		public Color[] previewColors;
		public Color colLayerGroup;
		public Color colLayer;
		public Color colMaskNodeGroup;
		public Color colMaskNode;
		public Color colSelectNodeGroup;
		public Color colSelectNode;
		public Color colSelectItemGroup;
		public Color colSelectItem;
		public float shelveHeight;
		public float shelveRightWidth;
		public float outputVSpace;
		public float groupVSpace;
		public float layerVSpace;
		public float layerHSpace;
		public float nodeHSpace;
		public float bracketHSpace;
		public Rect rect;
		public Rect rect2;
		public Rect rect3;
		public Rect rect4;
		public Rect rect5;
		public Rect rect6;
		public Rect rect7;
		public Rect rect8;
		public KeyCode keyZoomIn;
		public KeyCode keyZoomOut;
	}
}
