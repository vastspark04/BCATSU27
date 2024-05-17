using UnityEngine;
using UnityEngine.Rendering;

public class DashMapHeightDisplay : MonoBehaviour
{
	public Camera hmCam;

	public Renderer mapRenderer;

	public Transform myTransform;

	public FlightInfo flightInfo;

	public DashMapDisplay dmp;

	public RectTransform rtRectTf;

	private MaterialPropertyBlock props;

	private Texture2D currTex;

	public float viewRange = 10000f;

	public float scaleAdjust = 1.0454545f;

	private VTMapGenerator.EdgeModes currEdgeMode = (VTMapGenerator.EdgeModes)(-1);

	private int _ViewOffsetX;

	private int _ViewOffsetY;

	private int _ViewScale;

	private float currOobScale = -1f;

	private void Awake()
	{
		if (!hmCam)
		{
			hmCam = GetComponentInChildren<Camera>();
		}
		hmCam.enabled = true;
		hmCam.useOcclusionCulling = false;
		hmCam.allowHDR = false;
		hmCam.allowMSAA = false;
		mapRenderer.shadowCastingMode = ShadowCastingMode.Off;
		mapRenderer.receiveShadows = false;
		mapRenderer.lightProbeUsage = LightProbeUsage.Off;
		mapRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
		mapRenderer.allowOcclusionWhenDynamic = false;
		_ViewOffsetX = Shader.PropertyToID("_ViewOffsetX");
		_ViewOffsetY = Shader.PropertyToID("_ViewOffsetY");
		_ViewScale = Shader.PropertyToID("_ViewScale");
	}

	private void SetEdgeMode(VTMapGenerator.EdgeModes edgeMode, CardinalDirections coastSide)
	{
		mapRenderer.sharedMaterial.DisableKeyword("HILLS_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("WATER_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("COAST_BORDER");
		mapRenderer.sharedMaterial.DisableKeyword("NORTH_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("EAST_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("SOUTH_COAST");
		mapRenderer.sharedMaterial.DisableKeyword("WEST_COAST");
		currEdgeMode = edgeMode;
		switch (currEdgeMode)
		{
		case VTMapGenerator.EdgeModes.Hills:
			mapRenderer.sharedMaterial.EnableKeyword("HILLS_BORDER");
			break;
		case VTMapGenerator.EdgeModes.Water:
			mapRenderer.sharedMaterial.EnableKeyword("WATER_BORDER");
			break;
		case VTMapGenerator.EdgeModes.Coast:
			mapRenderer.sharedMaterial.EnableKeyword("COAST_BORDER");
			switch (coastSide)
			{
			case CardinalDirections.North:
				mapRenderer.sharedMaterial.EnableKeyword("NORTH_COAST");
				break;
			case CardinalDirections.East:
				mapRenderer.sharedMaterial.EnableKeyword("EAST_COAST");
				break;
			case CardinalDirections.South:
				mapRenderer.sharedMaterial.EnableKeyword("SOUTH_COAST");
				break;
			case CardinalDirections.West:
				mapRenderer.sharedMaterial.EnableKeyword("WEST_COAST");
				break;
			}
			break;
		}
	}

	public void UpdateDisplay()
	{
		if (props == null)
		{
			props = new MaterialPropertyBlock();
		}
		VTMapGenerator fetch = VTMapGenerator.fetch;
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(myTransform.position);
		float z = flightInfo.heading;
		if ((bool)dmp)
		{
			if (dmp.northUp)
			{
				z = 0f;
			}
			vector3D = dmp.GetGlobalFocusPoint();
			float height = rtRectTf.rect.height;
			viewRange = height / dmp.mapScale * 50f;
		}
		float num = 1f;
		if ((bool)fetch)
		{
			if (currEdgeMode != fetch.edgeMode)
			{
				SetEdgeMode(fetch.edgeMode, fetch.coastSide);
			}
			if (currTex != fetch.heightMap)
			{
				currTex = fetch.heightMap;
				props.SetTexture("_MainTex", currTex);
			}
			float chunkSize = fetch.chunkSize;
			num = (float)fetch.gridSize * chunkSize;
			if (fetch.oobProfile != null && currOobScale != (float)fetch.oobProfile.repeatScale)
			{
				currOobScale = fetch.oobProfile.repeatScale;
				float value = num / (chunkSize * currOobScale);
				props.SetFloat("_OOBTexScale", value);
			}
		}
		else if ((bool)VTMapManager.fetch && (bool)VTMapManager.fetch.fallbackHeightmap)
		{
			if (currEdgeMode != 0)
			{
				SetEdgeMode(VTMapGenerator.EdgeModes.Water, CardinalDirections.East);
			}
			if (currTex != VTMapManager.fetch.fallbackHeightmap)
			{
				currTex = VTMapManager.fetch.fallbackHeightmap;
				props.SetTexture("_MainTex", currTex);
			}
			num = VTMapManager.fetch.fallbackHeightmapTotalSize;
			vector3D.x += VTMapManager.fetch.fallbackHeightmapOffset.x;
			vector3D.z += VTMapManager.fetch.fallbackHeightmapOffset.y;
		}
		Vector2 vector = new Vector2((float)(vector3D.x / (double)num), (float)(vector3D.z / (double)num));
		float num2 = viewRange / num / 0.70710677f;
		props.SetFloat(_ViewOffsetX, vector.x);
		props.SetFloat(_ViewOffsetY, vector.y);
		props.SetFloat(_ViewScale, num2 * scaleAdjust);
		mapRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, z);
		mapRenderer.SetPropertyBlock(props);
	}
}
