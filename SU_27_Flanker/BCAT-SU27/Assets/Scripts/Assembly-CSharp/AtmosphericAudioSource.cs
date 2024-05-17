using UnityEngine;

public class AtmosphericAudioSource : MonoBehaviour
{
	public FlightInfo flightInfo;

	public bool directional;

	public float minMinDist;

	public float maxMinDist;

	public float minMaxDist;

	public float maxMaxDist;

	public float dotExp = 1f;

	public float lagAudioFactorMul = 1f;

	public float maxLagAudioFactor = 4f;

	private AudioSource audioSource;

	private float origMinDist = 1f;

	private float origMaxDist = 1f;

	private float modMinDist = 10f;

	private float modMaxDist = 10000f;

	private bool flybyMode;

	public bool specCamOnly = true;

	private const float effectMaxDistance = 15000f;

	private bool disabledOutOfRange;

	private void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		if (specCamOnly)
		{
			FlybyCameraMFDPage.OnBeginSpectatorCam += OnBeginSpecCam;
			FlybyCameraMFDPage.OnEndSpectatorCam += OnEndSpecCam;
		}
	}

	private void OnDestroy()
	{
		FlybyCameraMFDPage.OnBeginSpectatorCam -= OnBeginSpecCam;
		FlybyCameraMFDPage.OnEndSpectatorCam -= OnEndSpecCam;
	}

	private void Start()
	{
		if (!audioSource)
		{
			Object.Destroy(this);
			return;
		}
		if (!flightInfo)
		{
			flightInfo = GetComponentInParent<FlightInfo>();
		}
		origMinDist = audioSource.minDistance;
		origMaxDist = audioSource.maxDistance;
		modMinDist = origMinDist;
		modMaxDist = origMaxDist;
	}

	private float SpeedOfSound()
	{
		return 1f / MeasurementManager.SpeedToMach(1f, WaterPhysics.GetAltitude(base.transform.position));
	}

	private void LateUpdate()
	{
		Vector3 audioListenerPosition = AudioListenerPosition.GetAudioListenerPosition();
		if ((audioListenerPosition - base.transform.position).sqrMagnitude < 225000000f)
		{
			if (directional)
			{
				float num = Vector3.Dot((audioListenerPosition - base.transform.position).normalized, base.transform.forward);
				num = (num + 1f) / 2f;
				num = Mathf.Sign(num) * Mathf.Pow(num, dotExp);
				audioSource.minDistance = Mathf.Lerp(minMinDist, maxMinDist, num);
				modMinDist = audioSource.minDistance;
				audioSource.maxDistance = Mathf.Lerp(minMaxDist, maxMaxDist, num);
				modMaxDist = audioSource.maxDistance;
			}
			if (specCamOnly && !flybyMode)
			{
				return;
			}
			if (!flightInfo || !flightInfo.rb)
			{
				base.enabled = false;
				OnEndSpecCam();
				return;
			}
			float value = Vector3.Angle(flightInfo.rb.velocity, audioListenerPosition - base.transform.position);
			value = Mathf.Clamp(value, 1f, 180f);
			float airspeed = flightInfo.airspeed;
			airspeed = Mathf.Min(airspeed, 550f);
			float num2 = 75000f / (Vector3.Distance(base.transform.position, audioListenerPosition) * airspeed * value / 90f);
			num2 = Mathf.Clamp(num2 * num2 * num2, 0f, 4f);
			num2 += airspeed / 230f;
			float num3 = 3.67f * value / airspeed;
			num3 = Mathf.Clamp(num3 * num3 * num3, 0f, 2f);
			float num4 = SpeedOfSound();
			if (flightInfo.airspeed > num4)
			{
				float num5 = 3.67f;
				num3 = ((airspeed / value < num5) ? (num3 + airspeed / num4 * num3) : 0f);
			}
			num2 *= num3;
			num2 = Mathf.Min(num2 * lagAudioFactorMul, maxLagAudioFactor);
			audioSource.minDistance = Mathf.Max(minMinDist, Mathf.Lerp(origMinDist, modMinDist * num2, Mathf.Clamp01(airspeed / 30f)));
			audioSource.maxDistance = Mathf.Lerp(origMaxDist, Mathf.Clamp(modMaxDist * num2, audioSource.minDistance, 96000f), Mathf.Clamp01(airspeed / 30f));
			disabledOutOfRange = false;
		}
		else if (disabledOutOfRange)
		{
			disabledOutOfRange = true;
			audioSource.minDistance = origMinDist;
			audioSource.maxDistance = origMaxDist;
		}
	}

	private void OnBeginSpecCam()
	{
		if (FlybyCameraMFDPage.instance.finalBehavior == FlybyCameraMFDPage.SpectatorBehaviors.Stationary && FlybyCameraMFDPage.instance.cameraAudio)
		{
			flybyMode = true;
		}
		else
		{
			OnEndSpecCam();
		}
	}

	private void OnEndSpecCam()
	{
		flybyMode = false;
		if (!directional && (bool)audioSource)
		{
			audioSource.minDistance = origMinDist;
			audioSource.maxDistance = origMaxDist;
		}
	}
}
