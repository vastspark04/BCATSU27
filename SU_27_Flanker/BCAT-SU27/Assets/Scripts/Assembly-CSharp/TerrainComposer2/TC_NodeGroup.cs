using UnityEngine;

namespace TerrainComposer2
{
	public class TC_NodeGroup : TC_GroupBehaviour
	{
		public int nodeGroupLevel;
		public NodeGroupType type;
		public RenderTexture rtColorPreview;
		public bool useConstant;
		public float seed;
	}
}
