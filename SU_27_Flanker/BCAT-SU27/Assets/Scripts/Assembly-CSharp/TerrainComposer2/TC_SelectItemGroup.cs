using System.Collections.Generic;
using UnityEngine;

namespace TerrainComposer2
{
	public class TC_SelectItemGroup : TC_GroupBehaviour
	{
		public List<TC_SelectItem> itemList;
		public bool refreshRanges;
		public SplatCustom[] splatMixBuffer;
		public ColorItem[] colorMixBuffer;
		public Vector4[] indices;
		public Transform endT;
		public Vector2 scaleMinMaxMulti;
		public float scaleMulti;
		public float mix;
		public float scale;
		public bool linkScaleToMask;
		public bool untouched;
	}
}
