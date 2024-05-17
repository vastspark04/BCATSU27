using UnityEngine;
using System.Collections.Generic;

namespace TerrainComposer2
{
	public class TC_Settings : MonoBehaviour
	{
		public float version;
		public Vector2 scrollOffset;
		public Vector2 scrollAdd;
		public float scale;
		public bool drawDefaultInspector;
		public bool debugMode;
		public bool hideTerrainGroup;
		public bool useTCRuntime;
		public bool showFps;
		public bool hideMenuBar;
		public Transform dustbinT;
		public Transform selectionOld;
		public Terrain masterTerrain;
		public bool hasMasterTerrain;
		public PresetMode presetMode;
		public float seed;
		public string lastPath;
		public bool preview;
		public int previewResolution;
		public List<TC_RawImage> rawFiles;
		public List<TC_Image> imageList;
		public TC_GlobalSettings global;
		public string pathHeightmap;
	}
}
