using UnityEngine;

namespace TerrainComposer2
{
	public class TC_Image : MonoBehaviour
	{
		public RenderTexture rt;
		public int referenceCount;
		public bool isDestroyed;
		public bool callDestroy;
	}
}
