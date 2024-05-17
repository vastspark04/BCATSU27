using UnityEngine;
using System.Collections.Generic;

namespace TerrainComposer2
{
	public class TC_TerrainArea : MonoBehaviour
	{
		public RenderTexture[] rtSplatmaps;
		public RenderTexture rtColormap;
		public bool loaded;
		public ApplyChanges applyChanges;
		public Int2 totalHeightmapResolution;
		public Int2 heightTexResolution;
		public List<TCUnityTerrain> terrains;
		public bool createTerrainTab;
		public bool active;
		public Color color;
		public int index;
		public bool terrains_active;
		public bool terrains_scene_active;
		public bool terrains_foldout;
		public bool sizeTab;
		public bool resolutionsTab;
		public bool settingsTab;
		public bool splatTab;
		public bool treeTab;
		public bool grassTab;
		public bool resetTab;
		public Int2 tiles;
		public Int2 selectTiles;
		public bool tileLink;
		public Rect area;
		public Vector3 terrainSize;
		public Vector3 center;
		public Rect menuRect;
		public bool display_short;
		public string terrainDataPath;
		public Transform parent;
		public string terrainName;
		public bool copy_settings;
		public int copy_terrain;
		public int terrainSelect;
		public bool settingsEditor;
	}
}
