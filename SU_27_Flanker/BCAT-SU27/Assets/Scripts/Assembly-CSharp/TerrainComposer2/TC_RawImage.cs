using UnityEngine;

namespace TerrainComposer2
{
	public class TC_RawImage : MonoBehaviour
	{
		public enum ByteOrder
		{
			Windows = 0,
			Mac = 1,
		}

		public bool isResourcesFolder;
		public string path;
		public string filename;
		public int referenceCount;
		public Int2 resolution;
		public bool squareResolution;
		public ByteOrder byteOrder;
		public Texture2D tex;
		public bool isDestroyed;
		public bool callDestroy;
	}
}
