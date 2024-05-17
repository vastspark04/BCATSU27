using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;
using VTOLVR.Multiplayer;

public class BlackoutEffect : MonoBehaviour
{
	public float gTolerance = 7f;

	public float negGTolerance = 5f;

	public float gRecovery = 0.15f;

	public float blackoutRate = 0.025f;

	public float maxGAccum = 20f;

	public float aFactor = 0.4f;

	public AudioMixer audioMixer;

	public AnimationCurve audioCurve;

	public FlightInfo flightInfo;

	public float instantaneousGDeath = 100f;

	public UnityEvent OnAccelDeath;

	public MeshRenderer quadRenderer;

	private MaterialPropertyBlock quadProps;

	private int colorID;

	private Image[] images;

	private float gAccum;

	private float alpha;

	public bool useFlightInfo = true;

	public Rigidbody rb;

	private Vector3 lastV;

	public bool debug;

	public float debugGs;

	private NightVisionGoggles nvg;

	public Color redoutColor = Color.red;

	public Color nvgRedoutColor = Color.red;

	private bool addedG;

	public static BlackoutEffect instance { get; private set; }

	public bool accelDied { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		images = GetComponentsInChildren<Image>();
		if ((bool)quadRenderer)
		{
			Image[] array = images;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.SetActive(value: false);
			}
			quadProps = new MaterialPropertyBlock();
			colorID = Shader.PropertyToID("_TintColor");
		}
		nvg = GetComponentInParent<NightVisionGoggles>();
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
		VehicleMaster componentInParent = GetComponentInParent<VehicleMaster>();
		if ((bool)componentInParent)
		{
			componentInParent.OnPilotDied += AccelDie;
		}
	}

	private void LateUpdate()
	{
		float num = Mathf.Abs(gAccum) * aFactor;
		alpha = Mathf.Lerp(alpha, num, 20f * Time.deltaTime);
		Color color = (((bool)nvg && nvg.IsNVGVisible()) ? nvgRedoutColor : redoutColor);
		color *= RenderSettings.ambientIntensity;
		Color color2 = ((gAccum >= 0f) ? Color.black : color);
		color2.a = alpha * alpha;
		if ((bool)quadRenderer)
		{
			if (alpha > 0.001f)
			{
				quadRenderer.enabled = true;
				quadProps.SetColor(colorID, color2);
				quadRenderer.SetPropertyBlock(quadProps);
			}
			else
			{
				quadRenderer.enabled = false;
			}
		}
		else
		{
			for (int i = 0; i < images.Length; i++)
			{
				images[i].color = color2;
			}
		}
		if ((bool)audioMixer)
		{
			float time = Mathf.Clamp01(3f * num);
			float time2 = Mathf.Clamp01(1.8f - 1.8f * Mathf.Clamp01(num));
			if ((bool)FlybyCameraMFDPage.instance && FlybyCameraMFDPage.instance.isCamEnabled && FlybyCameraMFDPage.instance.cameraAudioListener.enabled && FlybyCameraMFDPage.instance.finalBehavior != FlybyCameraMFDPage.SpectatorBehaviors.SmoothLook)
			{
				time2 = AudioController.instance.steamPauseVolumeMultiplier;
				time = 0f;
			}
			else
			{
				time = audioCurve.Evaluate(time) * AudioController.instance.steamPauseVolumeMultiplier;
				time2 = audioCurve.Evaluate(time2) * AudioController.instance.steamPauseVolumeMultiplier;
			}
			audioMixer.SetFloat("ConsciousVolume", time2 * 80f - 80f);
			audioMixer.SetFloat("BlackoutVolume", time * 80f - 80f);
		}
		if (!accelDied && useFlightInfo && (bool)flightInfo && flightInfo.maxInstantaneousG > instantaneousGDeath)
		{
			FlightLogger.Log("Died by instantaneous G-force (" + flightInfo.maxInstantaneousG.ToString("0.0") + ")");
			AccelDie();
		}
	}

	public void AccelDie()
	{
		if (accelDied)
		{
			return;
		}
		accelDied = true;
		if (!VTOLMPUtils.IsMultiplayer())
		{
			EndMission.AddText(VTLStaticStrings.mission_killedGForces, red: true);
		}
		SetGAccum(maxGAccum);
		try
		{
			if ((bool)this && (bool)base.gameObject && (bool)base.transform)
			{
				VehicleMaster componentInParent = GetComponentInParent<VehicleMaster>();
				if ((bool)componentInParent)
				{
					componentInParent.KillPilot();
				}
			}
		}
		catch (Exception ex)
		{
			VTNetUtils.SendExceptionReport(ex.ToString());
		}
		try
		{
			if (OnAccelDeath != null)
			{
				OnAccelDeath.Invoke();
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Exception when calling BlackoutEffect.OnAccelDeath: {arg}");
		}
	}

	private void FixedUpdate()
	{
		float num = 0f;
		if (!accelDied)
		{
			if (debug)
			{
				num = debugGs;
			}
			else if (useFlightInfo)
			{
				num = flightInfo.playerGs;
				lastV = flightInfo.rb.velocity;
			}
			else
			{
				Vector3 vector = (rb.velocity - lastV) / Time.fixedDeltaTime;
				Vector3 vector2 = vector;
				vector = Vector3.Project(vector - Physics.gravity, rb.transform.up);
				num = vector.magnitude / 9.81f;
				num *= Mathf.Sign(Vector3.Dot(vector, rb.transform.up));
				lastV = rb.velocity;
				float num2 = vector2.magnitude / 9.81f;
				if (num2 > instantaneousGDeath)
				{
					FlightLogger.Log("Died by instantaneous G-force (" + num2.ToString("0.0") + ")");
					AccelDie();
				}
			}
		}
		if (num > 0f)
		{
			gAccum += Mathf.Max(Mathf.Abs(num) - gTolerance, 0f) * blackoutRate * Time.fixedDeltaTime;
		}
		else
		{
			gAccum += (0f - Mathf.Max(Mathf.Abs(num) - negGTolerance, 0f)) * blackoutRate * Time.fixedDeltaTime;
		}
		gAccum = Mathf.Clamp(gAccum, 0f - maxGAccum, maxGAccum);
		if (addedG)
		{
			addedG = false;
		}
		else if (num < gTolerance && num > 0f - negGTolerance)
		{
			float num3 = ((num > 0f) ? gTolerance : negGTolerance);
			float num4 = Mathf.Max(0f, (num3 - Mathf.Abs(num)) * gRecovery);
			gAccum = Mathf.MoveTowards(gAccum, 0f, num4 * Time.fixedDeltaTime);
		}
	}

	public void AddG(float g)
	{
		gAccum += g;
		addedG = true;
	}

	public void SetGAccum(float g)
	{
		gAccum = g;
	}

	public void SetForCamera(Transform cameraTf, float dist, float scale, int layer)
	{
		Transform parent = base.transform.parent;
		quadRenderer.gameObject.layer = layer;
		parent.localScale = new Vector3(scale, scale, scale);
		parent.position = cameraTf.position + cameraTf.forward * dist;
		parent.rotation = cameraTf.rotation;
	}
}
