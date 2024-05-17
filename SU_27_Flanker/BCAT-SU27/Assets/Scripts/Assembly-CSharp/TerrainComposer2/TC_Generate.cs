using UnityEngine;
using System;
using System.Collections.Generic;

namespace TerrainComposer2
{
	public class TC_Generate : MonoBehaviour
	{
		[Serializable]
		public class GenerateStack
		{
			public TCUnityTerrain tcTerrain;
			public int outputId;
			public bool assignTerrainHeightmap;
		}

		[Serializable]
		public class GenerateStackEntry
		{
			public List<TC_Generate.GenerateStack> stack;
			public int frame;
		}

		public float globalScale;
		public TC_Area2D area2D;
		public bool assignTerrainHeightmap;
		public bool hideHierarchy;
		public bool generate;
		public bool generateSplat;
		public bool generateSplatSingle;
		public bool generateTree;
		public bool generateObject;
		public bool generateGrass;
		public bool generateColor;
		public bool resetTrees;
		public bool generateSingle;
		public int threadActive;
		public bool isMesh;
		public bool resetObjects;
		public bool autoGenerate;
		public bool cmdGenerate;
		public bool generateNextFrame;
		public int generateDone;
		public int generateDoneOld;
		public int treesCount;
		public int objectsCount;
		public bool isGeneratingHeight;
		public int jobs;
		public List<TC_Generate.GenerateStackEntry> stackEntry;
		public Transform objectParent;
	}
}
