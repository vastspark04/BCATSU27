using System;
using System.Collections;
using System.IO;
using System.Threading;
using OC;
using UnityEngine;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class FlybyCameraMFDPage : MonoBehaviour, IPersistentVehicleData, IQSVehicleComponent, ILocalizationUser
{
	public enum SpectatorBehaviors
	{
		Stationary,
		FlyAlong,
		Fixed,
		Chase,
		PresetViews,
		SmoothLook,
		Camcorder
	}

	public enum CamTgtModes
	{
		SELF,
		MSSL,
		TGT
	}

	public class SCamNVGController : MonoBehaviour
	{
		public NightVisionGoggles nvg;

		public ScreenMaskedColorRamp specCamNVG;

		public bool doIllum;

		private bool illumEnabled;

		private int nvgScaleID;

		private bool hidNvg;

		private void Awake()
		{
			nvgScaleID = Shader.PropertyToID("_NVGEffectScale");
		}

		private void OnPreCull()
		{
			if ((bool)specCamNVG && (bool)nvg && specCamNVG.enabled)
			{
				if (doIllum)
				{
					illumEnabled = true;
					nvg.EnableIlluminator();
				}
			}
			else
			{
				hidNvg = true;
				Shader.SetGlobalFloat(nvgScaleID, 0f);
			}
		}

		private void OnPostRender()
		{
			if (illumEnabled)
			{
				illumEnabled = false;
				nvg.DisableIlluminator();
			}
			if (hidNvg)
			{
				Shader.SetGlobalFloat(nvgScaleID, NightVisionGoggles.nvgEffectScale);
			}
		}
	}

	private struct SS_Save
	{
		public byte[] png;

		public string dir;
	}

	public Camera flybyCam;

	public GameObject cameraModel;

	public Camera hmcsCam;

	public AudioListener cameraAudioListener;

	private AudioListenerPosition cameraALP;

	public AudioListener playerAudioListener;

	public Rigidbody shipRb;

	public Rigidbody seatRb;

	private Rigidbody targetRb;

	public Transform[] fixedTransforms;

	private int fixedCamIdx;

	public WeaponManager weaponManager;

	private Vector3 lookOffset = Vector3.zero;

	public RenderTexture previewRt;

	public GameObject previewObject;

	private GameObject overcloudPreviewCamObj;

	private Camera overcloudPreviewCam;

	private bool previewEnabled;

	private float lastRenderTime;

	private float frameRate = 8f;

	private Transform origCamParent;

	private bool flyCamEnabled;

	private bool followTarget = true;

	private Vector3 camVelocity = Vector3.zero;

	private float cameraStartTime;

	private bool isFixedCam;

	private float obstructTimer;

	private bool randomModes;

	public SpectatorBehaviors finalBehavior;

	public Text cameraAudioModeText;

	public CameraShadowSettings exteriorShadowSettings;

	private float origShadowDist;

	private CameraFogSettings fogSettings;

	private float origFogDensity;

	[Header("Handheld Mode")]
	public bool handheldMode = true;

	public Vector3 handHeldRotRate;

	public Vector3 handHeldRotOffset;

	public float handHeldPosRate = 1f;

	public float handHeldPosLimit = 3f;

	private Vector3 cameraShakeRate = new Vector3(8f, 8f, 8f);

	private float camShakeDamping = 3f;

	private Quaternion handheldRotation;

	private Transform presetViewTf;

	private Rigidbody presetViewRb;

	[Header("FOV")]
	public Text fovText;

	private bool autoZoom;

	private int fovIdx;

	private float[] fovs = new float[6] { 60f, 40f, 25f, 15f, 5f, 100f };

	private float[] autoZoomRadii = new float[6] { 40f, 30f, 20f, 10f, 60f, 50f };

	[Header("AutoReset")]
	public Text resetModeText;

	public Text resetTimeText;

	private bool autoReset = true;

	private int resetTimeIdx = 3;

	private float[] resetTimes = new float[4] { 8f, 10f, 12f, 6f };

	[Header("Behaviors")]
	public Text behaviorText;

	[Header("NightVision")]
	public ScreenMaskedColorRamp playerNVG;

	public ScreenMaskedColorRamp specCamNVG;

	private string[] specBehaviorLabels = new string[7] { "Stationary", "FlyAlong", "Fixed", "Chase", "PresetViews", "SmoothLook", "Camcorder" };

	private SpectatorBehaviors behavior;

	private int numBehaviors;

	public float smoothLookRate = 4f;

	private Vector3 chaseOffset;

	private int camCullMask;

	private float nearClip;

	private string[] camTgtModeLabels = new string[3] { "SELF", "MSSL", "TGT" };

	public Text missileModeText;

	private CamTgtModes camTgtMode;

	private bool hadMissile;

	private bool tgtViewingSelf;

	private float timeHadMissile;

	private Vector3 missileLookDir;

	private FixedPoint missileTargetPos;

	private Vector3 missileTargetVel;

	private FixedPoint cameraFixedPoint;

	private Quaternion lastRotation;

	public GameObject camcorderObj;

	private bool hasSetCamcorderPos;

	private Vector3 localCamcorderPos;

	private Vector3 localCamcorderFwd;

	private Vector3 localCamcorderUp;

	private const string NODE_NAME = "SpectatorCamera";

	private bool persistentStart;

	private string s_on = "ON";

	private string s_off = "OFF";

	private string s_autoReset = "Auto-Reset";

	private string s_auto = "Auto";

	private string s_audio = "AUDIO";

	private string s_random = "Random";

	private bool draggingCamcorder;

	[Header("Screenshots")]
	public Camera[] ssCameraStack;

	private Coroutine ssRoutine;

	private float _camShakeAmt;

	public static FlybyCameraMFDPage instance { get; private set; }

	public bool isCamEnabled => flyCamEnabled;

	public bool cameraAudio { get; private set; }

	public bool isInterior { get; private set; }

	private float currentFov => fovs[fovIdx];

	private float currentAutoZoomRadius => autoZoomRadii[fovIdx];

	private float currentResetTime => resetTimes[resetTimeIdx];

	public SpectatorBehaviors currentBehavior => behavior;

	public static event Action OnBeginSpectatorCam;

	public static event Action OnEndSpectatorCam;

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		ConfigNode configNode = vDataNode.AddOrGetNode("SpectatorCamera");
		configNode.SetValue("fovIdx", fovIdx);
		configNode.SetValue("resetTimeIdx", resetTimeIdx);
		configNode.SetValue("camTgtMode", camTgtMode);
		configNode.SetValue("behavior", behavior);
		configNode.SetValue("autoReset", autoReset);
		configNode.SetValue("randomModes", randomModes);
		configNode.SetValue("cameraAudio", cameraAudio);
		configNode.SetValue("autoZoom", autoZoom);
		configNode.SetValue("previewEnabled", previewEnabled);
		configNode.SetValue("flyCamEnabled", flyCamEnabled);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (vDataNode.HasNode("SpectatorCamera"))
		{
			ConfigNode node = vDataNode.GetNode("SpectatorCamera");
			ConfigNodeUtils.TryParseValue(node, "fovIdx", ref fovIdx);
			ConfigNodeUtils.TryParseValue(node, "resetTimeIdx", ref resetTimeIdx);
			ConfigNodeUtils.TryParseValue(node, "camTgtMode", ref camTgtMode);
			ConfigNodeUtils.TryParseValue(node, "behavior", ref behavior);
			ConfigNodeUtils.TryParseValue(node, "autoReset", ref autoReset);
			ConfigNodeUtils.TryParseValue(node, "randomModes", ref randomModes);
			bool target = false;
			ConfigNodeUtils.TryParseValue(node, "previewEnabled", ref target);
			if (target && !previewEnabled)
			{
				TogglePreview();
			}
			bool target2 = true;
			ConfigNodeUtils.TryParseValue(node, "cameraAudio", ref target2);
			cameraAudio = target2;
			ConfigNodeUtils.TryParseValue(node, "autoZoom", ref autoZoom);
			UpdateAllLabels();
			if (!QuicksaveManager.isQuickload && GameSettings.CurrentSettings.GetBoolSetting("PERSISTENT_S_CAM"))
			{
				ConfigNodeUtils.TryParseValue(node, "flyCamEnabled", ref persistentStart);
			}
		}
	}

	private void Awake()
	{
		ApplyLocalization();
		flybyCam.gameObject.SetActive(value: false);
		numBehaviors = Enum.GetValues(typeof(SpectatorBehaviors)).Length;
		instance = this;
		cameraALP = cameraAudioListener.GetComponent<AudioListenerPosition>();
		origShadowDist = exteriorShadowSettings.shadowDistance;
		fogSettings = flybyCam.GetComponent<CameraFogSettings>();
		origFogDensity = fogSettings.density;
	}

	private void OnDestroy()
	{
		flyCamEnabled = false;
		if (FloatingOrigin.instance != null)
		{
			FloatingOrigin.instance.OnOriginShift -= OnCamOriginShift;
		}
		if (flybyCam != null && flybyCam.gameObject != null)
		{
			UnityEngine.Object.Destroy(flybyCam.gameObject);
		}
		if ((bool)AudioController.instance && VTOLMPUtils.IsMine(base.gameObject))
		{
			AudioController.instance.SetExteriorOpening("s-cam", 0f);
		}
	}

	private void Start()
	{
		camCullMask = flybyCam.cullingMask;
		nearClip = flybyCam.nearClipPlane;
		origCamParent = flybyCam.transform.parent;
		previewObject.SetActive(value: false);
		UpdateAllLabels();
		cameraFixedPoint = default(FixedPoint);
		missileTargetPos = default(FixedPoint);
		if (GameSettings.CurrentSettings.GetBoolSetting("MULTI_DISPLAY"))
		{
			int num = 1;
			if (GameSettings.TryGetGameSettingValue<int>("MULTI_DISPLAY_INDEX", out var val))
			{
				num = val;
			}
			if (Display.displays.Length > num)
			{
				Display display = Display.displays[num];
				if (!display.active)
				{
					display.Activate();
					Debug.LogFormat("S-Cam: activating second monitor ({4}). Rendering W:{0} H:{1}, System W:{2} H:{3}", display.renderingWidth, display.renderingHeight, display.systemWidth, display.systemHeight, num);
					if (GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_WIDTH", out var val2) && GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_HEIGHT", out var val3))
					{
						Debug.LogFormat("- Overriding resolution: W:{0} H:{1}", val2, val3);
						display.SetRenderingResolution(val2, val3);
					}
					if (GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_SIZE_WIDTH", out var val4) && GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_SIZE_HEIGHT", out var val5) && GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_POS_X", out var val6) && GameSettings.TryGetGameSettingValue<int>("MD_OVERRIDE_POS_Y", out var val7))
					{
						Debug.LogFormat("- Overriding size and position: W:{0} H:{1} PosX:{2} PosY:{3}", val4, val5, val6, val7);
						display.SetParams(val4, val5, val6, val7);
					}
				}
				Camera[] componentsInChildren = flybyCam.GetComponentsInChildren<Camera>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].targetDisplay = num;
				}
			}
		}
		if (persistentStart)
		{
			StartCoroutine(PStartRoutine());
		}
		if (!specCamNVG)
		{
			return;
		}
		SCamNVGController sCamNVGController = flybyCam.gameObject.AddComponent<SCamNVGController>();
		if ((bool)playerNVG)
		{
			sCamNVGController.nvg = playerNVG.transform.parent.GetComponentInChildren<NightVisionGoggles>();
		}
		sCamNVGController.specCamNVG = specCamNVG;
		if (GameSettings.CurrentSettings.GetBoolSetting("FULLSCREEN_NVG"))
		{
			specCamNVG.maskTex = null;
			CameraSetGlobalTexture component = specCamNVG.GetComponent<CameraSetGlobalTexture>();
			if ((bool)component)
			{
				component.texture = null;
			}
		}
	}

	public void SetPlayerNVG(ScreenMaskedColorRamp pNvg)
	{
		SCamNVGController sCamNVGController = flybyCam.gameObject.GetComponent<SCamNVGController>();
		if (!sCamNVGController)
		{
			sCamNVGController = flybyCam.gameObject.AddComponent<SCamNVGController>();
		}
		playerNVG = pNvg;
		sCamNVGController.nvg = playerNVG.transform.parent.GetComponentInChildren<NightVisionGoggles>();
		sCamNVGController.specCamNVG = specCamNVG;
		if (GameSettings.CurrentSettings.GetBoolSetting("FULLSCREEN_NVG"))
		{
			specCamNVG.maskTex = null;
			CameraSetGlobalTexture component = specCamNVG.GetComponent<CameraSetGlobalTexture>();
			if ((bool)component)
			{
				component.texture = null;
			}
		}
	}

	private IEnumerator PStartRoutine()
	{
		yield return null;
		EnableCamera();
	}

	public void ApplyLocalization()
	{
		s_on = VTLocalizationManager.GetString("ON");
		s_off = VTLocalizationManager.GetString("OFF");
		s_autoReset = VTLocalizationManager.GetString("sCam_autoReset", "Auto-Reset", "S-Cam auto reset view label.");
		s_auto = VTLocalizationManager.GetString("sCam_autoZoomPrefix", "Auto", "S-Cam auto zoom prefix (Auto 40, etc)");
		s_audio = VTLocalizationManager.GetString("sCam_audio", "AUDIO", "S-Cam audio on/off label");
		for (int i = 0; i < camTgtModeLabels.Length; i++)
		{
			string[] array = camTgtModeLabels;
			int num = i;
			string key = $"sCam_tgtMode_{i}";
			CamTgtModes camTgtModes = (CamTgtModes)i;
			array[num] = VTLocalizationManager.GetString(key, camTgtModes.ToString(), "S-Cam target mode");
		}
		s_random = VTLocalizationManager.GetString("sCam_random", "Random", "S-Cam random view mode");
		for (int j = 0; j < specBehaviorLabels.Length; j++)
		{
			string[] array2 = specBehaviorLabels;
			int num2 = j;
			string key2 = $"sCam_behavior_{j}";
			SpectatorBehaviors spectatorBehaviors = (SpectatorBehaviors)j;
			array2[num2] = VTLocalizationManager.GetString(key2, spectatorBehaviors.ToString(), "S-Cam behavior");
		}
	}

	private void UpdateAllLabels()
	{
		UpdateBehaviorText();
		resetModeText.text = $"{s_autoReset}\n{(autoReset ? s_on : s_off)}";
		resetTimeText.text = $"{currentResetTime}s";
		missileModeText.text = camTgtModeLabels[(int)camTgtMode];
		if (autoZoom)
		{
			fovText.text = $"{s_auto} {currentAutoZoomRadius}";
		}
		else
		{
			flybyCam.fieldOfView = currentFov;
			hmcsCam.fieldOfView = currentFov;
			fovText.text = currentFov.ToString();
		}
		if ((bool)cameraAudioModeText)
		{
			cameraAudioModeText.text = $"{s_audio}\n{(cameraAudio ? s_on : s_off)}";
		}
	}

	private Transform GetTgtCamTf(out Actor tgtActor)
	{
		Transform result = null;
		tgtActor = null;
		if (camTgtMode == CamTgtModes.TGT)
		{
			if ((bool)weaponManager.tsc && weaponManager.tsc.GetCurrentSelectionActor() != null)
			{
				tgtActor = weaponManager.tsc.GetCurrentSelectionActor();
				result = tgtActor.transform;
			}
			else if ((bool)weaponManager.lockingRadar && weaponManager.lockingRadar.IsLocked())
			{
				tgtActor = weaponManager.lockingRadar.currentLock.actor;
				result = weaponManager.lockingRadar.currentLock.actor.transform;
			}
			else if ((bool)weaponManager.opticalTargeter && weaponManager.opticalTargeter.locked)
			{
				result = weaponManager.opticalTargeter.lockTransform;
				tgtActor = weaponManager.opticalTargeter.lockedActor;
			}
		}
		return result;
	}

	private void LateUpdate()
	{
		if (!flyCamEnabled)
		{
			return;
		}
		Vector3 vector = Vector3.zero;
		if (finalBehavior == SpectatorBehaviors.PresetViews && (bool)presetViewTf)
		{
			cameraFixedPoint.point = presetViewTf.position;
		}
		if (followTarget)
		{
			Rigidbody rigidbody = ((!seatRb || seatRb.isKinematic) ? shipRb : seatRb);
			if (camTgtMode == CamTgtModes.MSSL)
			{
				if ((bool)weaponManager.lastFiredMissile && (bool)weaponManager.lastFiredMissile.rb)
				{
					rigidbody = weaponManager.lastFiredMissile.rb;
					hadMissile = true;
					timeHadMissile = Time.time;
				}
			}
			else
			{
				hadMissile = false;
			}
			if (targetRb == null)
			{
				targetRb = rigidbody;
			}
			if (rigidbody.GetInstanceID() != targetRb.GetInstanceID())
			{
				lookOffset = targetRb.transform.TransformPoint(targetRb.centerOfMass) + lookOffset - rigidbody.transform.TransformPoint(rigidbody.centerOfMass);
				chaseOffset = targetRb.transform.TransformPoint(targetRb.centerOfMass) + chaseOffset - rigidbody.transform.TransformPoint(rigidbody.centerOfMass);
				targetRb = rigidbody;
			}
			else
			{
				targetRb = rigidbody;
				flybyCam.transform.position = cameraFixedPoint.point;
				flybyCam.transform.rotation = lastRotation;
			}
			vector = targetRb.transform.TransformPoint(targetRb.centerOfMass);
			flybyCam.transform.position += camVelocity * Time.deltaTime;
			Actor tgtActor;
			Transform tgtCamTf = GetTgtCamTf(out tgtActor);
			if (finalBehavior == SpectatorBehaviors.Chase)
			{
				bool flag = false;
				if (!hadMissile)
				{
					flag = true;
					if (tgtCamTf != null)
					{
						Vector3 rhs = tgtCamTf.position - targetRb.transform.position;
						Vector3 b = -40f * rhs.normalized + 5f * Vector3.up + 8f * Vector3.Cross(Vector3.up, rhs).normalized;
						chaseOffset = Vector3.Lerp(chaseOffset, b, 5f * Time.deltaTime);
						lookOffset = Vector3.Lerp(lookOffset, 40f * rhs.normalized, 4f * Time.deltaTime);
					}
					else if (targetRb.velocity.sqrMagnitude > 400f)
					{
						Vector3 b2 = -(40f * targetRb.velocity.normalized) + 6f * Vector3.up;
						chaseOffset = Vector3.Lerp(chaseOffset, b2, 5f * Time.deltaTime);
						lookOffset = Vector3.Lerp(lookOffset, 2f * targetRb.velocity, 2f * Time.deltaTime);
					}
					else
					{
						Vector3 b3 = -(40f * targetRb.transform.forward) + 6f * Vector3.up;
						chaseOffset = Vector3.Lerp(chaseOffset, b3, 5f * Time.deltaTime);
						lookOffset = Vector3.Lerp(lookOffset, 40f * targetRb.transform.forward, 2f * Time.deltaTime);
					}
					camVelocity = targetRb.velocity;
				}
				else if ((bool)weaponManager.lastFiredMissile && weaponManager.lastFiredMissile.timeToImpact > 1f)
				{
					flag = true;
					Vector3 b4;
					Vector3 vector2;
					if (weaponManager.lastFiredMissile.hasTarget)
					{
						Vector3 normalized = (weaponManager.lastFiredMissile.estTargetPos - weaponManager.lastFiredMissile.transform.position).normalized;
						b4 = -normalized * 10f + Vector3.Cross(normalized, Vector3.up) * 2f;
						vector2 = normalized;
						missileTargetPos.point = weaponManager.lastFiredMissile.estTargetPos;
						missileTargetVel = weaponManager.lastFiredMissile.estTargetVel;
					}
					else
					{
						Vector3 normalized2 = weaponManager.lastFiredMissile.rb.velocity.normalized;
						b4 = -normalized2 * 10f + Vector3.Cross(normalized2, Vector3.up) * 2f;
						vector2 = normalized2;
						missileTargetPos.point = weaponManager.lastFiredMissile.transform.position;
						missileTargetVel = weaponManager.lastFiredMissile.rb.velocity;
					}
					chaseOffset = Vector3.Lerp(chaseOffset, b4, 5f * Time.deltaTime);
					lookOffset = Vector3.Lerp(lookOffset, vector2 * 100f, 2f * Time.deltaTime);
					camVelocity = weaponManager.lastFiredMissile.rb.velocity;
				}
				if (flag)
				{
					flybyCam.transform.position = vector + chaseOffset;
					Vector3 vector3 = vector + lookOffset;
					flybyCam.transform.LookAt(vector3);
					missileLookDir = vector3 - flybyCam.transform.position;
				}
				else if (Time.time - timeHadMissile < 5f)
				{
					Vector3 vector4 = missileTargetPos.point + missileTargetVel * (Time.time - timeHadMissile);
					flybyCam.transform.rotation = Quaternion.Slerp(flybyCam.transform.rotation, Quaternion.LookRotation(vector4 - flybyCam.transform.position, flybyCam.transform.up), 5f * Time.deltaTime);
					camVelocity -= 2f * Time.deltaTime * camVelocity;
				}
				else
				{
					SetupFlybyPosition();
				}
			}
			else if (camTgtMode == CamTgtModes.MSSL && hadMissile && !weaponManager.lastFiredMissile)
			{
				if (Time.time - timeHadMissile < 5f)
				{
					flybyCam.transform.rotation = Quaternion.LookRotation(missileLookDir);
					camVelocity -= 2f * Time.deltaTime * camVelocity;
				}
				else
				{
					SetupFlybyPosition();
				}
			}
			else
			{
				if (camTgtMode == CamTgtModes.TGT && (bool)tgtCamTf && (finalBehavior == SpectatorBehaviors.Stationary || finalBehavior == SpectatorBehaviors.FlyAlong))
				{
					if (Vector3.Angle(vector - flybyCam.transform.position, tgtCamTf.position - flybyCam.transform.position) > currentFov / 2f)
					{
						if (tgtViewingSelf)
						{
							lookOffset = vector - tgtCamTf.position;
							tgtViewingSelf = false;
						}
						vector = tgtCamTf.position;
					}
					else if (!tgtViewingSelf)
					{
						lookOffset = tgtCamTf.position - vector;
						tgtViewingSelf = true;
					}
				}
				lookOffset = Vector3.Lerp(lookOffset, Vector3.zero, 3f * Time.deltaTime);
				Vector3 vector5 = vector + lookOffset;
				if (finalBehavior == SpectatorBehaviors.PresetViews && (bool)presetViewTf)
				{
					flybyCam.transform.rotation = Quaternion.LookRotation(vector5 - flybyCam.transform.position, presetViewTf.up);
				}
				else
				{
					flybyCam.transform.LookAt(vector5);
				}
				missileLookDir = vector5 - flybyCam.transform.position;
			}
			if ((bool)exteriorShadowSettings)
			{
				if (flybyCam.transform.position.sqrMagnitude > 25000000f)
				{
					exteriorShadowSettings.shadowDistance = 0f;
				}
				else
				{
					exteriorShadowSettings.shadowDistance = origShadowDist;
				}
			}
			if (randomModes || (autoReset && behavior != SpectatorBehaviors.Chase))
			{
				if (Physics.Linecast(targetRb.position, flybyCam.transform.position, 1, QueryTriggerInteraction.Ignore))
				{
					obstructTimer += Time.deltaTime;
					if (obstructTimer > 1f)
					{
						SetupFlybyPosition();
					}
				}
				else
				{
					obstructTimer = 0f;
				}
			}
			cameraFixedPoint.point = flybyCam.transform.position;
			lastRotation = flybyCam.transform.rotation;
			if ((bool)specCamNVG)
			{
				specCamNVG.enabled = false;
			}
		}
		else
		{
			if (finalBehavior == SpectatorBehaviors.PresetViews && (bool)presetViewTf)
			{
				flybyCam.transform.rotation = Quaternion.LookRotation(presetViewTf.forward, presetViewTf.up);
			}
			if (finalBehavior == SpectatorBehaviors.SmoothLook)
			{
				if ((bool)playerNVG && (bool)specCamNVG)
				{
					specCamNVG.enabled = playerNVG.enabled;
				}
			}
			else if ((bool)specCamNVG)
			{
				specCamNVG.enabled = false;
			}
		}
		if (finalBehavior != SpectatorBehaviors.SmoothLook && handheldMode)
		{
			UpdateHandheldRotation();
			if (finalBehavior == SpectatorBehaviors.Fixed)
			{
				flybyCam.transform.localRotation = handheldRotation;
			}
			else if (finalBehavior == SpectatorBehaviors.Camcorder && !draggingCamcorder)
			{
				flybyCam.transform.localRotation = handheldRotation * Quaternion.LookRotation(localCamcorderFwd, localCamcorderUp);
			}
			else
			{
				flybyCam.transform.rotation = handheldRotation * flybyCam.transform.rotation;
			}
		}
		if (autoZoom)
		{
			if (followTarget && finalBehavior != SpectatorBehaviors.Chase)
			{
				float value = Vector3.Angle(vector - flybyCam.transform.position, vector + currentAutoZoomRadius * flybyCam.transform.right - flybyCam.transform.position);
				float num = 3f;
				if (handheldMode)
				{
					num = 0.5f + 3f * Mathf.PerlinNoise(912.23f, Time.time * 0.35f);
				}
				flybyCam.fieldOfView = Mathf.Lerp(flybyCam.fieldOfView, Mathf.Clamp(value, 0.3f, 90f), num * Time.deltaTime);
			}
			else if (finalBehavior == SpectatorBehaviors.SmoothLook)
			{
				flybyCam.fieldOfView = 80f;
				hmcsCam.fieldOfView = 80f;
			}
			else
			{
				flybyCam.fieldOfView = 60f;
				hmcsCam.fieldOfView = 60f;
			}
		}
		if (finalBehavior == SpectatorBehaviors.SmoothLook)
		{
			camVelocity = ((!seatRb || seatRb.isKinematic) ? shipRb.velocity : seatRb.velocity);
			flybyCam.transform.localPosition = Vector3.Lerp(flybyCam.transform.localPosition, VRHead.instance.transform.localPosition, smoothLookRate * Time.deltaTime);
			flybyCam.transform.localRotation = Quaternion.Slerp(flybyCam.transform.localRotation, VRHead.instance.transform.localRotation, smoothLookRate * Time.deltaTime);
		}
		if (randomModes || (autoReset && behavior != SpectatorBehaviors.Chase))
		{
			if (Time.time - cameraStartTime > currentResetTime)
			{
				SetupFlybyPosition();
			}
			if (!isFixedCam && camVelocity != Vector3.zero && (WaterPhysics.GetAltitude(flybyCam.transform.position) < 1f || Physics.Linecast(flybyCam.transform.position, flybyCam.transform.position + camVelocity, 1, QueryTriggerInteraction.Ignore)))
			{
				if (behavior == SpectatorBehaviors.FlyAlong && camTgtMode == CamTgtModes.MSSL && (bool)targetRb && Physics.Linecast(targetRb.position, targetRb.position + targetRb.velocity * 2f, 1, QueryTriggerInteraction.Ignore))
				{
					camVelocity = Vector3.Lerp(camVelocity, Vector3.zero, 3f * Time.deltaTime);
				}
				else
				{
					SetupFlybyPosition();
				}
			}
		}
		if (isFixedCam)
		{
			if (behavior == SpectatorBehaviors.PresetViews)
			{
				if ((bool)presetViewRb && (bool)cameraALP)
				{
					cameraALP.SetManualVelocity(presetViewRb.GetPointVelocity(flybyCam.transform.position));
				}
			}
			else if ((bool)cameraALP)
			{
				cameraALP.SetManualVelocity(shipRb.velocity);
			}
			if (!flybyCam.transform.parent || !flybyCam.transform.parent.gameObject.activeInHierarchy)
			{
				SetupFlybyPosition();
			}
		}
		else if ((bool)cameraALP)
		{
			cameraALP.SetManualVelocity(camVelocity);
		}
		if (previewEnabled && Time.time - lastRenderTime > 1f / frameRate && !VTResources.useOverCloud && previewObject.activeSelf)
		{
			lastRenderTime = Time.time;
			flybyCam.enabled = false;
			flybyCam.targetTexture = previewRt;
			flybyCam.Render();
			flybyCam.targetTexture = null;
			flybyCam.enabled = true;
		}
		if (previewEnabled && (bool)overcloudPreviewCam)
		{
			overcloudPreviewCam.fieldOfView = flybyCam.fieldOfView;
		}
	}

	public void TogglePreview()
	{
		previewEnabled = !previewEnabled;
		if (flyCamEnabled)
		{
			previewObject.SetActive(previewEnabled);
		}
		if (VTResources.useOverCloud)
		{
			if (!overcloudPreviewCamObj)
			{
				overcloudPreviewCamObj = new GameObject("previewCam");
				overcloudPreviewCamObj.transform.parent = flybyCam.transform;
				overcloudPreviewCamObj.transform.localPosition = Vector3.zero;
				overcloudPreviewCamObj.transform.localRotation = Quaternion.identity;
				Camera camera = (overcloudPreviewCam = overcloudPreviewCamObj.AddComponent<Camera>());
				camera.targetTexture = previewRt;
				camera.fieldOfView = flybyCam.fieldOfView;
				OverCloudCamera overCloudCamera = overcloudPreviewCamObj.AddComponent<OverCloudCamera>();
				overCloudCamera.downsampleFactor = DownSampleFactor.Eight;
				overCloudCamera.renderVolumetricClouds = false;
				overCloudCamera.lightSampleCount = SampleCount.Low;
				overCloudCamera.highQualityClouds = false;
				overCloudCamera.downsample2DClouds = true;
				overCloudCamera.renderAtmosphere = true;
				overCloudCamera.renderScatteringMask = false;
				overCloudCamera.renderRainMask = false;
			}
			overcloudPreviewCamObj.SetActive(previewEnabled);
		}
	}

	public void ToggleCameraAudio()
	{
		cameraAudio = !cameraAudio;
		if (flyCamEnabled)
		{
			playerAudioListener.enabled = !cameraAudio;
			cameraAudioListener.enabled = cameraAudio;
			if (cameraAudio && (finalBehavior == SpectatorBehaviors.FlyAlong || finalBehavior == SpectatorBehaviors.Stationary || finalBehavior == SpectatorBehaviors.PresetViews || finalBehavior == SpectatorBehaviors.Chase || (finalBehavior == SpectatorBehaviors.Fixed && !fixedTransforms[fixedCamIdx].gameObject.name.Contains("int"))))
			{
				AudioController.instance.SetExteriorOpening("s-cam", 1f);
			}
			else
			{
				AudioController.instance.SetExteriorOpening("s-cam", 0f);
			}
		}
		if ((bool)cameraAudioModeText)
		{
			cameraAudioModeText.text = $"{s_audio}\n{(cameraAudio ? s_on : s_off)}";
		}
	}

	public void SetPlayerAudioListener(AudioListener al)
	{
		playerAudioListener = al;
		if ((bool)playerAudioListener)
		{
			if (flyCamEnabled)
			{
				playerAudioListener.enabled = !cameraAudio;
				cameraAudioListener.enabled = cameraAudio;
			}
			else
			{
				playerAudioListener.enabled = true;
			}
		}
		else
		{
			Debug.LogError("SetPlayerAudioListener(null)!!");
		}
	}

	public void ToggleRandom()
	{
		randomModes = !randomModes;
		UpdateBehaviorText();
		if (flyCamEnabled)
		{
			SetupFlybyPosition();
		}
	}

	public void NextMode()
	{
		if (!randomModes)
		{
			randomModes = false;
			behavior = (SpectatorBehaviors)((int)(behavior + 1) % numBehaviors);
			UpdateBehaviorText();
			if (flyCamEnabled)
			{
				SetupFlybyPosition();
			}
		}
	}

	public void PrevMode()
	{
		if (!randomModes)
		{
			randomModes = false;
			int num = (int)behavior;
			num--;
			if (num < 0)
			{
				num = numBehaviors - 1;
			}
			behavior = (SpectatorBehaviors)num;
			UpdateBehaviorText();
			if (flyCamEnabled)
			{
				SetupFlybyPosition();
			}
		}
	}

	public void ToggleMissileMode()
	{
		camTgtMode = (CamTgtModes)((int)(camTgtMode + 1) % 3);
		missileModeText.text = camTgtModeLabels[(int)camTgtMode];
	}

	public void ToggleAutoZoom()
	{
		autoZoom = !autoZoom;
		if (autoZoom)
		{
			fovText.text = $"{s_auto} {currentAutoZoomRadius}";
			return;
		}
		flybyCam.fieldOfView = currentFov;
		hmcsCam.fieldOfView = currentFov;
		fovText.text = currentFov.ToString();
	}

	public void CycleFovs()
	{
		fovIdx = (fovIdx + 1) % fovs.Length;
		if (autoZoom)
		{
			fovText.text = $"{s_auto} {currentAutoZoomRadius}";
		}
		else
		{
			fovText.text = currentFov.ToString();
		}
		flybyCam.fieldOfView = currentFov;
		hmcsCam.fieldOfView = currentFov;
	}

	public void ToggleAutoReset()
	{
		autoReset = !autoReset;
		resetModeText.text = $"{s_autoReset}\n{(autoReset ? s_on : s_off)}";
	}

	public void CycleResetTimes()
	{
		resetTimeIdx = (resetTimeIdx + 1) % resetTimes.Length;
		resetTimeText.text = $"{currentResetTime}s";
	}

	public void ToggleCamera()
	{
		if (flyCamEnabled)
		{
			DisableCamera();
		}
		else
		{
			EnableCamera();
		}
	}

	private void UpdateBehaviorText()
	{
		if (randomModes)
		{
			behaviorText.text = s_random;
			return;
		}
		int num = (int)behavior;
		if (num < 0)
		{
			behaviorText.text = "ERR";
		}
		else if (num >= specBehaviorLabels.Length)
		{
			behaviorText.text = behavior.ToString();
		}
		else
		{
			behaviorText.text = specBehaviorLabels[(int)behavior];
		}
	}

	public void EnableCamera()
	{
		if (flyCamEnabled)
		{
			SetupFlybyPosition();
			return;
		}
		if (previewEnabled)
		{
			previewObject.SetActive(value: true);
		}
		flyCamEnabled = true;
		cameraStartTime = Time.time;
		flybyCam.gameObject.SetActive(value: true);
		flybyCam.transform.parent = null;
		playerAudioListener.enabled = !cameraAudio;
		cameraAudioListener.enabled = cameraAudio;
		FloatingOrigin.instance.OnOriginShift += OnCamOriginShift;
		SetupFlybyPosition();
	}

	public void DisableCamera()
	{
		if (flyCamEnabled)
		{
			previewObject.SetActive(value: false);
			flyCamEnabled = false;
			flybyCam.gameObject.SetActive(value: false);
			flybyCam.transform.parent = origCamParent;
			cameraAudioListener.enabled = false;
			playerAudioListener.enabled = true;
			AudioController.instance.SetExteriorOpening("s-cam", 0f);
			FloatingOrigin.instance.OnOriginShift -= OnCamOriginShift;
			if (FlybyCameraMFDPage.OnEndSpectatorCam != null)
			{
				FlybyCameraMFDPage.OnEndSpectatorCam();
			}
		}
	}

	private void SetupFlybyPosition(SpectatorBehaviors forcedRandomMode = (SpectatorBehaviors)(-1))
	{
		if ((bool)camcorderObj)
		{
			camcorderObj.SetActive(value: false);
		}
		Actor tgtActor;
		Transform tgtCamTf = GetTgtCamTf(out tgtActor);
		if (cameraAudio)
		{
			AudioController.instance.SetExteriorOpening("s-cam", 1f);
		}
		isInterior = false;
		cameraModel.SetActive(value: true);
		flybyCam.transform.parent = null;
		flybyCam.cullingMask = camCullMask;
		flybyCam.nearClipPlane = nearClip;
		isFixedCam = false;
		obstructTimer = 0f;
		cameraStartTime = Time.time;
		hadMissile = false;
		tgtViewingSelf = false;
		_camShakeAmt = 0f;
		hmcsCam.gameObject.SetActive(value: false);
		if ((bool)exteriorShadowSettings)
		{
			exteriorShadowSettings.enabled = true;
			exteriorShadowSettings.shadowDistance = origShadowDist;
		}
		fogSettings.density = origFogDensity;
		targetRb = ((!seatRb || seatRb.isKinematic) ? shipRb : seatRb);
		if (camTgtMode == CamTgtModes.MSSL && (bool)weaponManager.lastFiredMissile)
		{
			targetRb = weaponManager.lastFiredMissile.rb;
		}
		if ((bool)tgtActor)
		{
			Rigidbody component = tgtActor.GetComponent<Rigidbody>();
			if ((bool)component)
			{
				targetRb = component;
			}
		}
		finalBehavior = behavior;
		if (randomModes)
		{
			if (forcedRandomMode >= SpectatorBehaviors.Stationary)
			{
				finalBehavior = forcedRandomMode;
			}
			else
			{
				finalBehavior = (SpectatorBehaviors)UnityEngine.Random.Range(0, numBehaviors);
			}
		}
		switch (finalBehavior)
		{
		case SpectatorBehaviors.Stationary:
		{
			Vector3 vector = (tgtCamTf ? (tgtCamTf.position + 2f * Vector3.up) : targetRb.position);
			float num4 = (tgtActor ? tgtActor.velocity.magnitude : targetRb.velocity.magnitude);
			float num5 = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
			float maxInclusive = Mathf.Max(25f, num4 * 2f);
			float num6 = UnityEngine.Random.Range(15f, maxInclusive);
			float num7 = UnityEngine.Random.Range(-50f, 50f);
			Vector3 vector2 = targetRb.transform.forward * 145f;
			flybyCam.transform.LookAt(targetRb.transform);
			if ((bool)tgtActor)
			{
				vector2 = ((!(tgtActor.velocity.sqrMagnitude > 900f)) ? (tgtActor.transform.forward * UnityEngine.Random.Range(20f, 145f)) : (tgtActor.velocity * (currentResetTime / 2f)));
				flybyCam.transform.LookAt(tgtActor.transform);
			}
			else if ((bool)tgtCamTf)
			{
				vector2 = Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, Vector3.up).normalized * 145f;
				flybyCam.transform.LookAt(tgtCamTf);
			}
			else if (targetRb.velocity.sqrMagnitude > 900f)
			{
				vector2 = targetRb.velocity * (currentResetTime / 2f);
			}
			Vector3 normalized = Vector3.Cross(Vector3.up, vector2).normalized;
			Vector3 normalized2 = Vector3.Cross(vector2, normalized).normalized;
			Vector3 vector3 = vector + vector2 + normalized * num6 * num5 + num7 * normalized2;
			Vector3 vector4 = vector3 - vector;
			if ((bool)tgtCamTf)
			{
				vector4.y = Mathf.Max(vector4.y, 0f);
				vector3 = vector + vector4;
			}
			if (Physics.Raycast(vector, vector4, out var hitInfo2, vector4.magnitude, 1, QueryTriggerInteraction.Ignore))
			{
				vector3 = hitInfo2.point + 5f * hitInfo2.normal;
			}
			if ((bool)WaterPhysics.instance && vector3.y < WaterPhysics.instance.height)
			{
				vector3.y = WaterPhysics.instance.height + 2f;
			}
			followTarget = true;
			flybyCam.transform.position = vector3;
			camVelocity = Vector3.zero;
			break;
		}
		case SpectatorBehaviors.FlyAlong:
		{
			Vector3 vector5 = (tgtCamTf ? (tgtCamTf.position + 2f * Vector3.up) : targetRb.position);
			Vector3 vector6 = vector5 + Vector3.ProjectOnPlane(UnityEngine.Random.onUnitSphere, Vector3.up).normalized * UnityEngine.Random.Range(15f, 50f) + UnityEngine.Random.Range(-2f, 10f) * Vector3.up;
			Vector3 rhs = targetRb.transform.forward;
			if ((bool)tgtActor && tgtActor.velocity.sqrMagnitude > 900f)
			{
				rhs = tgtActor.velocity.normalized;
			}
			Vector3 vector7 = Vector3.Cross(Vector3.up, rhs);
			Vector3 vector8 = vector6 - vector5;
			flybyCam.transform.LookAt(targetRb.transform);
			if ((bool)tgtCamTf)
			{
				flybyCam.transform.LookAt(tgtCamTf);
				vector8.y = Mathf.Max(vector8.y, 0f);
				vector6 = vector5 + vector8;
			}
			if (Physics.Raycast(vector5, vector8, out var hitInfo3, vector8.magnitude, 1, QueryTriggerInteraction.Ignore))
			{
				vector6 = hitInfo3.point + 5f * hitInfo3.normal;
			}
			if ((bool)WaterPhysics.instance && vector6.y < WaterPhysics.instance.height)
			{
				vector6.y = WaterPhysics.instance.height + 2f;
			}
			followTarget = true;
			flybyCam.transform.position = vector6;
			camVelocity = targetRb.velocity;
			if ((bool)tgtCamTf)
			{
				if ((bool)tgtActor)
				{
					camVelocity = tgtActor.velocity;
				}
				else
				{
					camVelocity = Vector3.zero;
				}
			}
			camVelocity += vector7 * UnityEngine.Random.Range(0f, 5f);
			if ((bool)tgtCamTf)
			{
				camVelocity.y = Mathf.Max(camVelocity.y, 0f);
			}
			break;
		}
		case SpectatorBehaviors.Fixed:
		{
			isFixedCam = true;
			int num = fixedCamIdx;
			fixedCamIdx = (fixedCamIdx + 1) % fixedTransforms.Length;
			while (fixedCamIdx != num && !fixedTransforms[fixedCamIdx].gameObject.activeInHierarchy)
			{
				fixedCamIdx = (fixedCamIdx + 1) % fixedTransforms.Length;
			}
			flybyCam.transform.parent = fixedTransforms[fixedCamIdx];
			flybyCam.transform.localPosition = Vector3.zero;
			flybyCam.transform.localRotation = Quaternion.identity;
			followTarget = false;
			camVelocity = Vector3.zero;
			if (fixedTransforms[fixedCamIdx].gameObject.name.Contains("int"))
			{
				AudioController.instance.SetExteriorOpening("s-cam", 0f);
				isInterior = true;
				flybyCam.nearClipPlane = 0.04f;
				if ((bool)exteriorShadowSettings)
				{
					exteriorShadowSettings.enabled = false;
				}
				fogSettings.density = 9E-05f;
			}
			if (fixedTransforms[fixedCamIdx].gameObject.name.Contains("fpv"))
			{
				flybyCam.cullingMask |= 4194304;
				flybyCam.nearClipPlane = 0.02f;
				hmcsCam.gameObject.SetActive(value: true);
				isInterior = true;
				AudioController.instance.SetExteriorOpening("s-cam", 0f);
				if ((bool)exteriorShadowSettings)
				{
					exteriorShadowSettings.enabled = false;
				}
			}
			break;
		}
		case SpectatorBehaviors.Chase:
			isFixedCam = false;
			followTarget = true;
			camVelocity = Vector3.zero;
			if (camTgtMode == CamTgtModes.MSSL != hadMissile)
			{
				flybyCam.transform.position = targetRb.transform.position - 40f * targetRb.transform.forward + 6f * Vector3.up;
				flybyCam.transform.LookAt(targetRb.transform.position + 2f * targetRb.velocity, Vector3.up);
			}
			break;
		case SpectatorBehaviors.PresetViews:
		{
			isFixedCam = true;
			followTarget = true;
			camVelocity = Vector3.zero;
			SpectatorPresetPosition spectatorPresetPosition = null;
			float num2 = 16000000f;
			for (int i = 0; i < SpectatorPresetPosition.presetPositions.Count; i++)
			{
				if (SpectatorPresetPosition.presetPositions[i].gameObject.activeInHierarchy)
				{
					float num3 = Vector3.SqrMagnitude(targetRb.transform.position - SpectatorPresetPosition.presetPositions[i].transform.position);
					if (num3 < num2)
					{
						spectatorPresetPosition = SpectatorPresetPosition.presetPositions[i];
						num2 = num3;
					}
				}
			}
			if ((bool)spectatorPresetPosition)
			{
				flybyCam.transform.parent = spectatorPresetPosition.transform;
				flybyCam.transform.localPosition = Vector3.zero;
				presetViewRb = spectatorPresetPosition.GetComponentInParent<Rigidbody>();
				if (spectatorPresetPosition.fixedView)
				{
					followTarget = false;
				}
			}
			else
			{
				if (randomModes)
				{
					SetupFlybyPosition(SpectatorBehaviors.Stationary);
					return;
				}
				isFixedCam = false;
				Ray ray = new Ray(direction: new Vector3(UnityEngine.Random.Range(-1f, 1f), -0.2f, UnityEngine.Random.Range(-1f, 1f)), origin: targetRb.position);
				if (Physics.Raycast(ray, out var hitInfo, 5000f, 1))
				{
					flybyCam.transform.position = hitInfo.point + 1.8f * hitInfo.normal;
				}
				else
				{
					flybyCam.transform.position = ray.GetPoint(5000f);
				}
				if (flybyCam.transform.position.y < WaterPhysics.instance.height)
				{
					Vector3 position2 = flybyCam.transform.position;
					position2.y = WaterPhysics.instance.height + 5f;
					flybyCam.transform.position = position2;
				}
				presetViewRb = null;
			}
			if ((bool)spectatorPresetPosition)
			{
				presetViewTf = spectatorPresetPosition.transform;
			}
			else
			{
				presetViewTf = null;
			}
			break;
		}
		case SpectatorBehaviors.SmoothLook:
			flybyCam.transform.parent = VRHead.instance.transform.parent;
			flybyCam.nearClipPlane = 0.045f;
			flybyCam.cullingMask |= 4194304;
			flybyCam.cullingMask ^= 2097152;
			flybyCam.cullingMask ^= 1048576;
			hmcsCam.gameObject.SetActive(value: true);
			followTarget = false;
			isFixedCam = true;
			cameraModel.SetActive(value: false);
			camVelocity = Vector3.zero;
			AudioController.instance.SetExteriorOpening("s-cam", 0f);
			isInterior = true;
			if ((bool)exteriorShadowSettings)
			{
				exteriorShadowSettings.enabled = false;
			}
			break;
		case SpectatorBehaviors.Camcorder:
			if (!camcorderObj)
			{
				behavior = SpectatorBehaviors.Stationary;
				SetupFlybyPosition(SpectatorBehaviors.Stationary);
				return;
			}
			flybyCam.transform.parent = FlightSceneManager.instance.playerActor.transform;
			isFixedCam = true;
			followTarget = false;
			if (!hasSetCamcorderPos)
			{
				Transform parent = flybyCam.transform.parent;
				Vector3 position = VRHead.instance.transform.position + parent.forward * 0.4f;
				Vector3 direction = -parent.forward;
				Vector3 up = parent.up;
				localCamcorderPos = parent.InverseTransformPoint(position);
				localCamcorderFwd = parent.InverseTransformDirection(direction);
				localCamcorderUp = parent.InverseTransformDirection(up);
				hasSetCamcorderPos = true;
			}
			flybyCam.transform.localPosition = localCamcorderPos;
			flybyCam.transform.localRotation = Quaternion.LookRotation(localCamcorderFwd, localCamcorderUp);
			isInterior = true;
			AudioController.instance.SetExteriorOpening("s-cam", 0f);
			camVelocity = Vector3.zero;
			if ((bool)exteriorShadowSettings)
			{
				exteriorShadowSettings.enabled = false;
			}
			flybyCam.nearClipPlane = 0.045f;
			fogSettings.density = 9E-05f;
			if (behavior == SpectatorBehaviors.Camcorder)
			{
				camcorderObj.SetActive(value: true);
			}
			break;
		}
		if (followTarget && (bool)targetRb)
		{
			cameraFixedPoint.point = flybyCam.transform.position;
			lastRotation = flybyCam.transform.rotation;
		}
		if (FlybyCameraMFDPage.OnBeginSpectatorCam != null)
		{
			FlybyCameraMFDPage.OnBeginSpectatorCam();
		}
	}

	public void StartDraggingCamcorder(VRInteractable interactable)
	{
		StartCoroutine(CamcorderDraggingRoutine(interactable.activeController));
	}

	private IEnumerator CamcorderDraggingRoutine(VRHandController controller)
	{
		draggingCamcorder = true;
		VRInteractable interactable = controller.activeInteractable;
		flybyCam.transform.parent = controller.transform;
		while (base.enabled && behavior == SpectatorBehaviors.Camcorder && controller.activeInteractable == interactable)
		{
			cameraStartTime = Time.time;
			yield return null;
		}
		draggingCamcorder = false;
		if (behavior == SpectatorBehaviors.Camcorder && flybyCam.transform.parent == controller.transform)
		{
			flybyCam.transform.parent = FlightSceneManager.instance.playerActor.transform;
			Transform parent = flybyCam.transform.parent;
			Vector3 position = flybyCam.transform.position;
			Vector3 forward = flybyCam.transform.forward;
			Vector3 up = flybyCam.transform.up;
			localCamcorderPos = parent.InverseTransformPoint(position);
			localCamcorderFwd = parent.InverseTransformDirection(forward);
			localCamcorderUp = parent.InverseTransformDirection(up);
		}
	}

	private void OnCamOriginShift(Vector3 offset)
	{
		if (!isFixedCam)
		{
			flybyCam.transform.position += offset;
		}
	}

	public void HiResScreenshot()
	{
		if (flyCamEnabled)
		{
			if (ssRoutine != null)
			{
				StopCoroutine(ssRoutine);
			}
			ssRoutine = StartCoroutine(ScreenshotRoutine());
		}
	}

	private IEnumerator ScreenshotRoutine()
	{
		if (isInterior)
		{
			yield return new WaitForSeconds(3f);
		}
		yield return new WaitForEndOfFrame();
		int num = 3840;
		int num2 = num / 16 * 9;
		previewObject.SetActive(value: false);
		RenderTexture temporary = RenderTexture.GetTemporary(num, num2, 32);
		temporary.antiAliasing = 8;
		for (int i = 0; i < ssCameraStack.Length; i++)
		{
			Camera camera = ssCameraStack[i];
			if (camera.gameObject.activeSelf)
			{
				RenderTexture targetTexture = camera.targetTexture;
				camera.targetTexture = temporary;
				camera.Render();
				camera.targetTexture = targetTexture;
			}
		}
		Texture2D texture2D = new Texture2D(num, num2, TextureFormat.RGB24, mipChain: false);
		RenderTexture.active = temporary;
		texture2D.ReadPixels(new Rect(0f, 0f, num, num2), 0, 0);
		RenderTexture.ReleaseTemporary(temporary);
		string dir = Path.Combine(VTResources.gameRootDirectory, "Screenshots");
		byte[] png = texture2D.EncodeToPNG();
		ThreadPool.QueueUserWorkItem(T_Save4kSS, new SS_Save
		{
			png = png,
			dir = dir
		});
		UnityEngine.Object.Destroy(texture2D);
		previewObject.SetActive(previewEnabled);
	}

	private void T_Save4kSS(object o)
	{
		SS_Save sS_Save = (SS_Save)o;
		File.WriteAllBytes(GetNewScreenshotFilepath(sS_Save.dir), sS_Save.png);
	}

	private string GetNewScreenshotFilepath(string dir)
	{
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}
		string text = ".png";
		string text2 = "screenshot";
		int num = 0;
		string path = text2 + num + text;
		string text3 = Path.Combine(dir, path);
		while (File.Exists(text3))
		{
			num++;
			path = text2 + num + text;
			text3 = Path.Combine(dir, path);
		}
		return text3;
	}

	private void UpdateHandheldRotation()
	{
		Vector3 forward = flybyCam.transform.forward;
		Vector3 right = flybyCam.transform.right;
		Vector3 up = flybyCam.transform.up;
		Vector3 vector = handHeldRotRate;
		Vector3 vector2 = handHeldRotOffset;
		if (finalBehavior == SpectatorBehaviors.Fixed || finalBehavior == SpectatorBehaviors.Camcorder || (finalBehavior == SpectatorBehaviors.PresetViews && !followTarget))
		{
			vector *= 2f;
			vector2 /= 2f;
			handheldRotation = Quaternion.identity;
		}
		else
		{
			float angle = VectorUtils.FullRangePerlinNoise(12.62f, vector.x * Time.time) * vector2.x;
			float angle2 = VectorUtils.FullRangePerlinNoise(134.142f, vector.y * Time.time) * vector2.y;
			float angle3 = VectorUtils.FullRangePerlinNoise(1502.235f, vector.z * Time.time) * vector2.z;
			handheldRotation = Quaternion.AngleAxis(angle3, forward) * Quaternion.AngleAxis(angle, right) * Quaternion.AngleAxis(angle2, up);
		}
		if ((double)_camShakeAmt > 0.001)
		{
			float angle4 = VectorUtils.FullRangePerlinNoise(12.62f, cameraShakeRate.x * Time.time) * vector2.x * _camShakeAmt;
			float angle5 = VectorUtils.FullRangePerlinNoise(134.142f, cameraShakeRate.y * Time.time) * vector2.y * _camShakeAmt;
			float angle6 = VectorUtils.FullRangePerlinNoise(1502.235f, cameraShakeRate.z * Time.time) * vector2.z * _camShakeAmt;
			_camShakeAmt = Mathf.Lerp(_camShakeAmt, 0f, camShakeDamping * Time.deltaTime);
			handheldRotation = Quaternion.AngleAxis(angle6, forward) * Quaternion.AngleAxis(angle4, right) * Quaternion.AngleAxis(angle5, up) * handheldRotation;
		}
	}

	public static void ShakeSpectatorCamera(float magnitude)
	{
		if ((bool)instance && instance.isCamEnabled)
		{
			instance.ShakeCamera(magnitude);
		}
	}

	public void ShakeCamera(float magnitude)
	{
		if (flyCamEnabled)
		{
			_camShakeAmt = Mathf.Max(_camShakeAmt, magnitude);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("SpectatorCamera");
		configNode.SetValue("flyCamEnabled", flyCamEnabled);
		if (flyCamEnabled)
		{
			configNode.SetValue("behavior", behavior);
			configNode.SetValue("camPosition", VTMapManager.WorldToGlobalPoint(flybyCam.transform.position));
			configNode.SetValue("camRotation", flybyCam.transform.rotation.eulerAngles);
			configNode.SetValue("camVelocity", camVelocity);
			configNode.SetValue("fixedCamIdx", fixedCamIdx);
			configNode.SetValue("fovIdx", fovIdx);
			configNode.SetValue("resetTimeIdx", resetTimeIdx);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("SpectatorCamera");
		if (node != null && node.GetValue<bool>("flyCamEnabled"))
		{
			SpectatorBehaviors value = node.GetValue<SpectatorBehaviors>("behavior");
			while (behavior != value)
			{
				NextMode();
			}
			fixedCamIdx = node.GetValue<int>("fixedCamIdx");
			int value2 = node.GetValue<int>("fovIdx");
			while (value2 != fovIdx)
			{
				CycleFovs();
			}
			int value3 = node.GetValue<int>("resetTimeIdx");
			while (value3 != resetTimeIdx)
			{
				CycleResetTimes();
			}
			EnableCamera();
			flybyCam.transform.position = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("camPosition"));
			cameraFixedPoint = new FixedPoint(flybyCam.transform.position);
			flybyCam.transform.rotation = Quaternion.Euler(node.GetValue<Vector3>("camRotation"));
			camVelocity = node.GetValue<Vector3>("camVelocity");
		}
	}
}
