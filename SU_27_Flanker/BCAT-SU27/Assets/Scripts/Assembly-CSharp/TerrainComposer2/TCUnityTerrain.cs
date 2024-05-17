using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace TerrainComposer2
{
	[Serializable]
	public class TCUnityTerrain : TC_Terrain
	{
		public bool active;
		public int index;
		public int index_old;
		public bool on_row;
		public Color color_terrain;
		public int copy_terrain;
		public bool copy_terrain_settings;
		public Transform objectsParent;
		public bool detailSettingsFoldout;
		public bool splatSettingsFoldout;
		public bool treeSettingsFoldout;
		public Terrain terrain;
		public Color[] splatColors;
		public List<TC_SplatPrototype> splatPrototypes;
		public List<TC_TreePrototype> treePrototypes;
		public List<TC_DetailPrototype> detailPrototypes;
		public int heightmapResolutionList;
		public int splatmapResolutionList;
		public int basemapResolutionList;
		public int detailResolutionPerPatchList;
		public Vector3 size;
		public int tileX;
		public int tileZ;
		public int heightmapResolution;
		public int splatmapResolution;
		public int basemapResolution;
		public int detailResolution;
		public int detailResolutionPerPatch;
		public int appliedResolutionPerPatch;
		public float grassScaleMulti;
		public float heightmapPixelError;
		public int heightmapMaximumLOD;
		public bool castShadows;
		public float basemapDistance;
		public float treeDistance;
		public float detailObjectDistance;
		public float detailObjectDensity;
		public int treeMaximumFullLODCount;
		public float treeBillboardDistance;
		public float treeCrossFadeLength;
		public bool drawTreesAndFoliage;
		public ReflectionProbeUsage reflectionProbeUsage;
		public bool bakeLightProbesForTrees;
		public float thickness;
		public float legacyShininess;
		public Color legacySpecular;
		public TC_TerrainSettings terrainSettingsScript;
		public Terrain.MaterialType materialType;
		public Material materialTemplate;
		public bool drawHeightmap;
		public bool collectDetailPatches;
		public float wavingGrassSpeed;
		public float wavingGrassAmount;
		public float wavingGrassStrength;
		public Color wavingGrassTint;
	}
}
