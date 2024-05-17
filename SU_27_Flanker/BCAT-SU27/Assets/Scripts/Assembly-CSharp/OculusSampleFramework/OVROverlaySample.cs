using UnityEngine;

namespace OculusSampleFramework
{
	public class OVROverlaySample : MonoBehaviour
	{
		public GameObject mainCamera;
		public GameObject uiCamera;
		public GameObject uiGeoParent;
		public GameObject worldspaceGeoParent;
		public OVROverlay cameraRenderOverlay;
		public OVROverlay renderingLabelOverlay;
		public Texture applicationLabelTexture;
		public Texture compositorLabelTexture;
		public GameObject prefabForLevelLoadSim;
		public OVROverlay cubemapOverlay;
		public OVROverlay loadingTextQuadOverlay;
		public float distanceFromCamToLoadText;
		public float cubeSpawnRadius;
		public float heightBetweenItems;
		public int numObjectsPerLevel;
		public int numLevels;
		public int numLoopsTrigger;
	}
}
