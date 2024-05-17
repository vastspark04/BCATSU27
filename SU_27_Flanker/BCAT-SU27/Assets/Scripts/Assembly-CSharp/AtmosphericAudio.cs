using UnityEngine;
using UnityEngine.Audio;

public class AtmosphericAudio : MonoBehaviour
{
	public bool onlyOnFlybyCam = true;

	public AudioSource windAudioSource;

	public AudioSource windHowlAudioSource;

	public AudioSource windTearAudioSource;

	public AudioSource sonicBoomSource;

	public FlightInfo flightInfo;

	private const float effectMaxDistance = 15000f;

	private bool disabledOutOfRange;

	private bool playedBoom;

	private bool audioPlaying;

	private float timePlayedBoom;

	private float minBoomInterval = 5f;

	private void Start()
	{
		AudioSource audioSource = windTearAudioSource;
		AudioSource audioSource2 = sonicBoomSource;
		AudioSource audioSource3 = windHowlAudioSource;
		AudioVelocityUpdateMode audioVelocityUpdateMode2 = (windAudioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic);
		AudioVelocityUpdateMode audioVelocityUpdateMode4 = (audioSource3.velocityUpdateMode = audioVelocityUpdateMode2);
		AudioVelocityUpdateMode audioVelocityUpdateMode7 = (audioSource.velocityUpdateMode = (audioSource2.velocityUpdateMode = audioVelocityUpdateMode4));
		if (onlyOnFlybyCam)
		{
			FlybyCameraMFDPage.OnBeginSpectatorCam += OnBeginSpecCam;
			FlybyCameraMFDPage.OnEndSpectatorCam += OnEndSpecCam;
		}
		else
		{
			OnBeginSpecCam();
			playedBoom = false;
		}
		sonicBoomSource.gameObject.AddComponent<FloatingOriginTransform>();
		AudioMixerGroup exteriorMixerGroup = VTResources.GetExteriorMixerGroup();
		AudioSource audioSource4 = windAudioSource;
		AudioSource audioSource5 = windHowlAudioSource;
		AudioSource audioSource6 = windTearAudioSource;
		AudioMixerGroup audioMixerGroup2 = (sonicBoomSource.outputAudioMixerGroup = exteriorMixerGroup);
		AudioMixerGroup audioMixerGroup4 = (audioSource6.outputAudioMixerGroup = audioMixerGroup2);
		AudioMixerGroup audioMixerGroup7 = (audioSource4.outputAudioMixerGroup = (audioSource5.outputAudioMixerGroup = audioMixerGroup4));
	}

	private void OnDestroy()
	{
		FlybyCameraMFDPage.OnBeginSpectatorCam -= OnBeginSpecCam;
		FlybyCameraMFDPage.OnEndSpectatorCam -= OnEndSpecCam;
		if ((bool)sonicBoomSource)
		{
			Object.Destroy(sonicBoomSource.gameObject);
		}
	}

	private float SpeedOfSound()
	{
		return 1f / MeasurementManager.SpeedToMach(1f, WaterPhysics.GetAltitude(base.transform.position));
	}

	private void LateUpdate()
	{
		if (!audioPlaying)
		{
			return;
		}
		if (onlyOnFlybyCam && (!FlybyCameraMFDPage.instance.isCamEnabled || !FlybyCameraMFDPage.instance.cameraAudio || FlybyCameraMFDPage.instance.isInterior))
		{
			EndAudio();
		}
		else if ((AudioListenerPosition.GetAudioListenerPosition() - base.transform.position).sqrMagnitude < 225000000f)
		{
			Vector3 vector = flightInfo.rb.velocity - AudioListenerPosition.velocity;
			float magnitude = vector.magnitude;
			float num = 0.005f * AerodynamicsController.fetch.AtmosDensityAtPosition(flightInfo.transform.position) * magnitude * magnitude;
			Vector3 audioListenerPosition = AudioListenerPosition.GetAudioListenerPosition();
			Vector3 position = flightInfo.transform.position;
			magnitude = Mathf.Min(magnitude, 550f);
			float value = Vector3.Angle(vector, audioListenerPosition - position);
			value = Mathf.Clamp(value, 1f, 180f);
			float num2 = 75000f / (Vector3.Distance(position, audioListenerPosition) * magnitude * value / 90f);
			num2 = Mathf.Clamp(num2 * num2 * num2, 0f, 4f);
			num2 += magnitude / 230f;
			float num3 = 3.67f * value / magnitude;
			num3 = Mathf.Clamp(num3 * num3 * num3, 0f, 2f);
			float num4 = SpeedOfSound();
			if (flightInfo.airspeed > num4)
			{
				float num5 = (playedBoom ? 3.68f : 3.67f);
				num3 = ((magnitude / value < num5) ? (num3 + magnitude / num4 * num3) : 0f);
				if (num3 > 0f && magnitude > num4)
				{
					if (!playedBoom && Time.time - timePlayedBoom > minBoomInterval)
					{
						timePlayedBoom = Time.time;
						sonicBoomSource.transform.position = position - vector;
						sonicBoomSource.PlayOneShot(sonicBoomSource.clip);
						FlybyCameraMFDPage.ShakeSpectatorCamera(magnitude * magnitude / (audioListenerPosition - position).sqrMagnitude);
					}
					playedBoom = true;
				}
			}
			else if (num4 / value < 3.67f)
			{
				playedBoom = true;
			}
			if (playedBoom && value < 90f)
			{
				playedBoom = false;
			}
			num2 *= num3;
			float sqrMagnitude = flightInfo.acceleration.sqrMagnitude;
			if (!windAudioSource.isPlaying)
			{
				windAudioSource.Play();
			}
			float num6 = Mathf.Clamp01(num / 50f);
			float num7 = Mathf.Clamp01(flightInfo.rb.mass / 60f);
			float num8 = Mathf.Clamp(sqrMagnitude / 225f, 0f, 1.5f);
			windAudioSource.volume = num7 * num6 * num8 * num2;
			if (!windHowlAudioSource.isPlaying)
			{
				windHowlAudioSource.Play();
			}
			float num9 = Mathf.Clamp01(num / 20f);
			float num10 = Mathf.Clamp01(flightInfo.rb.mass / 30f);
			windHowlAudioSource.volume = num9 * num10 * num2;
			windHowlAudioSource.maxDistance = Mathf.Clamp(num2 * 2500f, windTearAudioSource.minDistance, 16000f);
			if (!windTearAudioSource.isPlaying)
			{
				windTearAudioSource.Play();
			}
			float num11 = Mathf.Clamp01(num / 40f);
			float num12 = Mathf.Clamp01(flightInfo.rb.mass / 10f);
			windTearAudioSource.volume = num11 * num12;
			windTearAudioSource.minDistance = num2 * 1f;
			windTearAudioSource.maxDistance = Mathf.Clamp(num2 * 2500f, windTearAudioSource.minDistance, 16000f);
			disabledOutOfRange = false;
		}
		else if (!disabledOutOfRange)
		{
			disabledOutOfRange = true;
			EndAudio();
			audioPlaying = true;
		}
	}

	private void OnBeginSpecCam()
	{
		if (!onlyOnFlybyCam || FlybyCameraMFDPage.instance.finalBehavior == FlybyCameraMFDPage.SpectatorBehaviors.Stationary)
		{
			BeginAudio();
		}
		else
		{
			EndAudio();
		}
	}

	private void OnEndSpecCam()
	{
		EndAudio();
	}

	private void BeginAudio()
	{
		float value = Vector3.Angle(flightInfo.rb.velocity, AudioListenerPosition.GetAudioListenerPosition() - flightInfo.transform.position);
		value = Mathf.Clamp(value, 1f, 180f);
		if (flightInfo.airspeed / value < 3.67f)
		{
			playedBoom = true;
		}
		else
		{
			playedBoom = false;
		}
		windAudioSource.Play();
		windTearAudioSource.Play();
		windHowlAudioSource.Play();
		sonicBoomSource.transform.parent = null;
		audioPlaying = true;
	}

	private void EndAudio()
	{
		audioPlaying = false;
		windAudioSource.Stop();
		windTearAudioSource.Stop();
		windHowlAudioSource.Stop();
		sonicBoomSource.Stop();
		sonicBoomSource.transform.parent = base.transform;
	}
}
