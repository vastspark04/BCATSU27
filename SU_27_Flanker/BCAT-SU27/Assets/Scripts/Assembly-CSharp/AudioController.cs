using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Valve.VR;

public class AudioController : MonoBehaviour
{
	public AudioMixer environmentMixer;

	public AudioMixerGroup exteriorChannel;

	public AudioMixerGroup exteriorAttachedChannel;

	public AudioMixerGroup interiorChannel;

	public AudioMixerGroup dashWarningsChannel;

	public float interiorLowpassFreq = 350f;

	public float exteriorVolumeFromInterior;

	private Dictionary<string, float> openingDic = new Dictionary<string, float>();

	private float _openings;

	private float lowpass;

	private float volume;

	[HideInInspector]
	public float steamPauseVolumeMultiplier = 1f;

	private Stack<AudioSource> audioPool;

	public static AudioController instance { get; private set; }

	public float exteriorLevel => _openings;

	[ContextMenu("Print exterior openings")]
	public void PrintExtOpenings()
	{
		foreach (KeyValuePair<string, float> item in openingDic)
		{
			Debug.Log(item.Key + ": " + item.Value);
		}
	}

	private void Awake()
	{
		instance = this;
		CreateAudioPool();
	}

	private void Start()
	{
		environmentMixer.SetFloat("LowpassFreq", interiorLowpassFreq);
		environmentMixer.SetFloat("ExteriorVolume", exteriorVolumeFromInterior);
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += ClearAllOpenings;
		}
	}

	public void ClearAllOpenings()
	{
		openingDic.Clear();
		UpdateExteriorLevels();
	}

	private void OnEnable()
	{
		SteamVR_Events.InputFocus.AddListener(OnInputFocus);
	}

	private void OnDisable()
	{
		SteamVR_Events.InputFocus.RemoveListener(OnInputFocus);
	}

	public void MP_SetNearVoiceAtten(float nrmAtten)
	{
		environmentMixer.SetFloat("VTVoiceAtten", Mathf.Lerp(-80f, 0f, nrmAtten));
	}

	private void CreateAudioPool()
	{
		audioPool = new Stack<AudioSource>();
		for (int i = 0; i < 200; i++)
		{
			AddNewSourceToPool();
		}
	}

	private void AddNewSourceToPool()
	{
		GameObject obj = new GameObject("PooledAudio");
		obj.transform.parent = base.transform;
		AudioSource audioSource = obj.AddComponent<AudioSource>();
		audioSource.spatialBlend = 1f;
		audioSource.dopplerLevel = 0f;
		audioPool.Push(audioSource);
	}

	public void PlayOneShot(AudioClip clip, Vector3 position, float pitch, float volume, float minDist, float maxDist, bool exterior = true, int priority = 128, Transform parentTf = null)
	{
		if (audioPool.Count != 0)
		{
			StartCoroutine(OneShotRoutine(clip, position, pitch, volume, minDist, maxDist, exterior, priority, parentTf));
		}
	}

	private IEnumerator OneShotRoutine(AudioClip clip, Vector3 position, float pitch, float volume, float minDist, float maxDist, bool exterior, int priority, Transform parentTf)
	{
		AudioSource source = audioPool.Pop();
		if ((bool)parentTf)
		{
			source.transform.parent = parentTf;
		}
		else
		{
			FloatingOrigin.instance.AddTransform(source.transform);
		}
		source.transform.position = position;
		source.pitch = pitch;
		source.volume = volume;
		source.minDistance = minDist;
		source.maxDistance = maxDist;
		source.priority = priority;
		if (exterior)
		{
			source.outputAudioMixerGroup = exteriorChannel;
		}
		else
		{
			source.outputAudioMixerGroup = interiorChannel;
		}
		source.PlayOneShot(clip);
		yield return new WaitForSeconds(clip.length);
		if ((bool)source)
		{
			audioPool.Push(source);
			if ((bool)parentTf)
			{
				source.transform.parent = null;
			}
			else
			{
				FloatingOrigin.instance.RemoveTransform(source.transform);
			}
		}
		else
		{
			Debug.LogError("An audio pool source was destroyed!");
			AddNewSourceToPool();
		}
	}

	private void OnInputFocus(bool hasFocus)
	{
		steamPauseVolumeMultiplier = (hasFocus ? 1 : 0);
	}

	public void AddExteriorOpening(string openingName, float amount)
	{
		if (!openingDic.ContainsKey(openingName))
		{
			openingDic.Add(openingName, Mathf.Clamp01(amount));
		}
		else
		{
			openingDic[openingName] = Mathf.Clamp01(openingDic[openingName] + amount);
		}
		UpdateExteriorLevels();
	}

	public void RemoveExteriorOpening(string openingName, float amount)
	{
		if (openingDic.ContainsKey(openingName))
		{
			openingDic[openingName] = Mathf.Clamp01(openingDic[openingName] - amount);
		}
		UpdateExteriorLevels();
	}

	public void SetExteriorOpening(string openingName, float amount)
	{
		if (!openingDic.ContainsKey(openingName))
		{
			openingDic.Add(openingName, Mathf.Clamp01(amount));
		}
		else
		{
			openingDic[openingName] = Mathf.Clamp01(amount);
		}
		UpdateExteriorLevels();
	}

	private void UpdateExteriorLevels()
	{
		float num = 0f;
		foreach (float value3 in openingDic.Values)
		{
			num += value3;
		}
		num = (_openings = Mathf.Clamp01(num));
		float value = Mathf.Lerp(interiorLowpassFreq, 22000f, num);
		float value2 = Mathf.Lerp(exteriorVolumeFromInterior, 0f, num);
		lowpass = value;
		volume = value2;
		environmentMixer.SetFloat("LowpassFreq", value);
		environmentMixer.SetFloat("ExteriorVolume", value2);
		environmentMixer.SetFloat("ExtAttachDistortSend", Mathf.Lerp(-17f, -33f, num));
		environmentMixer.SetFloat("ExtAttachDrySend", Mathf.Lerp(-33f, 0f, num));
	}
}
