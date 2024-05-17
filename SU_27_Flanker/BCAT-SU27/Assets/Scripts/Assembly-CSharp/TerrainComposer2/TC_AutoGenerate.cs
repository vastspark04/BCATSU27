using UnityEngine;

namespace TerrainComposer2
{
	public class TC_AutoGenerate : MonoBehaviour
	{
		public CachedTransform cT;
		public bool generateOnEnable;
		public bool generateOnDisable;
		public bool instantGenerate;
		public bool waitForEndOfFrame;
	}
}
