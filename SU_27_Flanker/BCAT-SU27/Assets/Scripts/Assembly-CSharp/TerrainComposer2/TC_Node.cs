using System;
using UnityEngine;

namespace TerrainComposer2
{
	public class TC_Node : TC_ItemBehaviour
	{
		[Serializable]
		public class Shapes
		{
			public Vector2 topSize;
			public Vector2 bottomSize;
			public float size;
		}

		public InputKind inputKind;
		public InputTerrain inputTerrain;
		public InputNoise inputNoise;
		public InputShape inputShape;
		public InputFile inputFile;
		public InputCurrent inputCurrent;
		public InputPortal inputPortal;
		public NodeGroupType nodeType;
		public CollisionMode collisionMode;
		public CollisionDirection collisionDirection;
		public BlurMode blurMode;
		public int nodeGroupLevel;
		public ImageWrapMode wrapMode;
		public bool clamp;
		public float radius;
		public TC_RawImage rawImage;
		public TC_Image image;
		public ImageSettings imageSettings;
		public bool square;
		public int splatSelectIndex;
		public Noise noise;
		public Shapes shapes;
		public int iterations;
		public Vector2 detectRange;
		public int mipmapLevel;
		public ConvexityMode convexityMode;
		public float convexityStrength;
		public Texture stampTex;
		public string pathTexStamp;
		public bool isStampInResourcesFolder;
		public string resourcesFolder;
		public float posYOld;
		public int collisionMask;
		public bool heightDetectRange;
		public bool includeTerrainHeight;
		public Vector2 range;
		public bool useConstant;
		public Vector3 size;
	}
}
