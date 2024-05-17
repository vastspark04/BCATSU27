using UnityEngine;
using VTOLVR.Multiplayer;

public class GroundRadarDispatcher : MonoBehaviour
{
	public MFDRadarUI radarUI;

	public float mapScale = 10f;

	public float maxAltitude = 3500f;

	public Transform radarTransform;

	public Camera radarCamera;

	public Transform referenceTransform;

	public ComputeShader radarComputeShader;

	public int rtSize;

	public float dissipationRate;

	public float lockedDissipationRate = 0.2f;

	public RenderTexture outputRT;

	public RenderTexture radarRT;

	public RenderTexture radarLockedRT;

	public bool isLocked;

	public float lockedViewSize = 2f;

	private int mainKernelIdx;

	private int blurKernelIdx;

	private int clearKernelIdx;

	public bool blitOutput;

	public MinMax defaultCameraRanges = new MinMax(12f, 30000f);

	public MinMax lockedTargetRangeOffsets = new MinMax(-1000f, 1000f);

	private const string radarCamPositionRefName = "_GR_PositionReferencePoint";

	private int radarCamPositionRefID;

	private bool initialized;

	private bool doUpdates;

	public HPEquipRadar radarEq;

	private MultiUserVehicleSync muvs;

	private int mapScaleID;

	private int rtSizeID;

	private int viewZeroPositionID;

	private int radarPositionID;

	private int lockedModeID;

	private int radarDirectionID;

	private int originOffsetID;

	private int maxAltitudeID;

	private int dissipationRateID;

	private int deltaTimeID;

	private int viewRotationID;

	private int ResultID;

	private int RadarTexID;

	private static RenderTexture rwRt;

	private void Awake()
	{
		mapScaleID = Shader.PropertyToID("mapScale");
		rtSizeID = Shader.PropertyToID("rtSize");
		radarPositionID = Shader.PropertyToID("radarPosition");
		viewZeroPositionID = Shader.PropertyToID("viewZeroPosition");
		radarDirectionID = Shader.PropertyToID("radarDirection");
		originOffsetID = Shader.PropertyToID("originOffset");
		maxAltitudeID = Shader.PropertyToID("maxAltitude");
		dissipationRateID = Shader.PropertyToID("dissipationRate");
		deltaTimeID = Shader.PropertyToID("deltaTime");
		viewRotationID = Shader.PropertyToID("viewRotation");
		ResultID = Shader.PropertyToID("Result");
		RadarTexID = Shader.PropertyToID("RadarTex");
		lockedModeID = Shader.PropertyToID("lockedMode");
		if ((bool)radarEq)
		{
			if ((bool)radarEq.weaponManager)
			{
				RadarEq_OnEquipped();
			}
			else
			{
				radarEq.OnEquipped += RadarEq_OnEquipped;
			}
		}
		if (!VTOLMPUtils.IsMultiplayer())
		{
			doUpdates = true;
			Initialize();
		}
	}

	private void Initialize()
	{
		if (initialized)
		{
			return;
		}
		radarCamPositionRefID = Shader.PropertyToID("_GR_PositionReferencePoint");
		if (!outputRT)
		{
			outputRT = new RenderTexture(rtSize, rtSize, 16);
			outputRT.format = RenderTextureFormat.ARGBFloat;
			outputRT.filterMode = FilterMode.Point;
			outputRT.enableRandomWrite = true;
			outputRT.Create();
		}
		else
		{
			if (!rwRt)
			{
				rwRt = new RenderTexture(outputRT);
				rwRt.enableRandomWrite = true;
				rwRt.Create();
			}
			rtSize = outputRT.width;
			outputRT = rwRt;
		}
		mainKernelIdx = radarComputeShader.FindKernel("CSMain");
		blurKernelIdx = radarComputeShader.FindKernel("Blur");
		clearKernelIdx = radarComputeShader.FindKernel("Clear");
		initialized = true;
	}

	private void RadarEq_OnEquipped()
	{
		radarUI = base.transform.root.GetComponentInChildren<MFDRadarUI>(includeInactive: true);
		if ((bool)radarUI)
		{
			if (radarUI.wm.actor.isPlayer)
			{
				Initialize();
				doUpdates = true;
			}
			if ((bool)radarUI && (bool)radarUI.groundRadarImage)
			{
				radarUI.groundRadarImage.texture = outputRT;
			}
			muvs = radarEq.weaponManager.gameObject.GetComponent<MultiUserVehicleSync>();
			if ((bool)muvs)
			{
				muvs.OnOccupantEntered += OccupantChanged;
				muvs.OnOccupantLeft += OccupantChanged;
			}
		}
		else
		{
			base.enabled = false;
			if ((bool)radarCamera)
			{
				radarCamera.enabled = false;
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)muvs)
		{
			muvs.OnOccupantEntered -= OccupantChanged;
			muvs.OnOccupantLeft -= OccupantChanged;
		}
	}

	private void OccupantChanged(int seatIdx, ulong user)
	{
		doUpdates = muvs.IsLocalPlayerSeated();
		if (muvs.IsLocalPlayerSeated())
		{
			Initialize();
		}
		if (!radarUI)
		{
			radarUI = base.transform.root.GetComponentInChildren<MFDRadarUI>(includeInactive: true);
		}
		if ((bool)radarUI && (bool)radarUI.groundRadarImage)
		{
			radarUI.groundRadarImage.texture = outputRT;
		}
	}

	private void Start()
	{
		if ((bool)radarUI && (bool)radarUI.groundRadarImage)
		{
			radarUI.groundRadarImage.texture = outputRT;
		}
		ClearImage();
	}

	private void Update()
	{
		if (!initialized)
		{
			return;
		}
		if (!doUpdates)
		{
			radarCamera.enabled = false;
			return;
		}
		Vector3 position = radarTransform.position;
		Vector3 forward = referenceTransform.forward;
		radarCamera.nearClipPlane = defaultCameraRanges.min;
		radarCamera.farClipPlane = defaultCameraRanges.max;
		if ((bool)radarUI)
		{
			if (isLocked && (bool)radarUI.currentLockedActor)
			{
				position = radarUI.currentLockedActor.position;
				mapScale = radarUI.currentLockedActor.physicalRadius * 2f * lockedViewSize;
				forward = position - radarTransform.position;
				float magnitude = (radarUI.currentLockedActor.position - radarCamera.transform.position).magnitude;
				float nearClipPlane = Mathf.Max(defaultCameraRanges.min, magnitude + lockedTargetRangeOffsets.min);
				float farClipPlane = Mathf.Min(defaultCameraRanges.max, magnitude + lockedTargetRangeOffsets.max);
				radarCamera.nearClipPlane = nearClipPlane;
				radarCamera.farClipPlane = farClipPlane;
			}
			else
			{
				mapScale = radarUI.viewRange;
			}
			radarComputeShader.SetInt(lockedModeID, isLocked ? 1 : 0);
		}
		Shader.SetGlobalVector(radarCamPositionRefID, isLocked ? position : radarCamera.transform.position);
		radarCamera.targetTexture = (isLocked ? radarLockedRT : radarRT);
		radarComputeShader.SetFloat(mapScaleID, mapScale);
		radarComputeShader.SetInt(rtSizeID, rtSize);
		radarComputeShader.SetVector(radarPositionID, radarTransform.position);
		radarComputeShader.SetVector(viewZeroPositionID, position);
		radarComputeShader.SetVector(radarDirectionID, radarTransform.forward);
		radarComputeShader.SetVector(originOffsetID, FloatingOrigin.accumOffset.toVector3);
		radarComputeShader.SetFloat(maxAltitudeID, maxAltitude);
		radarComputeShader.SetFloat(dissipationRateID, isLocked ? lockedDissipationRate : dissipationRate);
		radarComputeShader.SetFloat(deltaTimeID, Mathf.Min(Time.deltaTime, 0.011f));
		forward.y = 0f;
		forward.Normalize();
		radarComputeShader.SetMatrix(viewRotationID, Matrix4x4.TRS(Vector3.zero, Quaternion.Inverse(Quaternion.LookRotation(forward)), Vector3.one));
		radarComputeShader.SetTexture(blurKernelIdx, ResultID, outputRT);
		radarComputeShader.Dispatch(blurKernelIdx, rtSize / 8, rtSize / 8, 1);
		RenderTexture renderTexture = (isLocked ? radarLockedRT : radarRT);
		radarComputeShader.SetTexture(mainKernelIdx, RadarTexID, renderTexture);
		radarComputeShader.SetTexture(mainKernelIdx, ResultID, outputRT);
		radarComputeShader.Dispatch(mainKernelIdx, renderTexture.width / 4, renderTexture.height / 256, 1);
	}

	public void ClearImage()
	{
		if (initialized)
		{
			radarComputeShader.SetTexture(clearKernelIdx, ResultID, outputRT);
			radarComputeShader.Dispatch(clearKernelIdx, rtSize / 8, rtSize / 8, 1);
		}
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (blitOutput)
		{
			Graphics.Blit(outputRT, destination);
		}
	}
}
