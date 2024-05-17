using UnityEngine;

namespace TerrainComposer2
{
	public class TC_ItemBehaviour : MonoBehaviour
	{
		public float versionNumber;
		public bool defaultPreset;
		public bool isLocked;
		public bool autoGenerate;
		public bool visible;
		public bool active;
		public int foldout;
		public bool nodeFoldout;
		public int outputId;
		public int terrainLevel;
		public int level;
		public string notes;
		public int listIndex;
		public bool firstLoad;
		public TexturePreview preview;
		public RenderTexture rtDisplay;
		public RenderTexture rtPreview;
		public Method method;
		public float opacity;
		public bool abs;
		public Curve localCurve;
		public Curve worldCurve;
		public Transform t;
		public Transform parentOld;
		public int siblingIndexOld;
		public CachedTransform ct;
		public CachedTransform ctOld;
		public Bounds bounds;
		public bool lockTransform;
		public bool lockPosParent;
		public bool lockPosChildren;
		public bool lockPosX;
		public bool lockPosY;
		public bool lockPosZ;
		public bool lockRotY;
		public bool lockScaleX;
		public bool lockScaleY;
		public bool lockScaleZ;
		public PositionMode positionMode;
		public float posY;
		public Vector3 posOffset;
		public bool controlDown;
		[SerializeField]
		private int instanceID;
	}
}
