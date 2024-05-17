using UnityEngine;

namespace TerrainComposer2
{
	public class TC_Area2D : MonoBehaviour
	{
		public TC_TerrainArea[] terrainAreas;
		public TC_TerrainMeshArea[] meshTerrainAreas;
		public TC_TerrainArea currentTerrainArea;
		public TC_PreviewArea previewArea;
		public Rect area;
		public Rect totalArea;
		public TC_TerrainLayer layerLevelC;
		public TC_TerrainLayer terrainLayer;
		public Vector2 resolution;
		public Vector2 resolutionPM;
		public Vector2 resToPreview;
		public Vector2 worldPos;
		public Vector2 localPos;
		public Vector2 pos;
		public Vector2 localNPos;
		public Vector2 previewPos;
		public Vector2 snapOffsetUV;
		public Vector2 outputOffsetV2;
		public Vector3 startPos;
		public Vector3 terrainSize;
		public Vector3 outputOffsetV3;
		public Bounds bounds;
		public Int2 intResolution;
		public float heightN;
		public float height;
		public float angle;
		public int splatLength;
		public int splatmapLength;
		public float frames;
		public float[] splatTotal;
		public float terrainsToDo;
		public float terrainsDone;
		public float progress;
		public bool showProgressBar;
		public int previewResolution;
		public Vector2 terrainHeightRange;
	}
}
