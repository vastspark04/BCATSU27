using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class BGMManager : MonoBehaviour
{
	private static BGMManager instance;

	public AudioSource[] audioSources;

	private int sourceIdx;

	public AudioMixer mixer;

	public float minAtten;

	public float maxAtten;

	public AnimationCurve attenFadeCurve;

	private float currAttenT;

	private string currID;

	private Coroutine fadeToRoutine;

	private Coroutine fadeRoutine;

	private AudioSource currentSource => audioSources[sourceIdx];

	private AudioSource otherSource => audioSources[(sourceIdx + 1) % 2];

	public static bool isLoopingBGM { get; private set; }

	public static string currentID => instance.currID;

	public static bool isPlaying
	{
		get
		{
			if ((bool)instance && (bool)instance.currentSource)
			{
				return instance.currentSource.isPlaying;
			}
			return false;
		}
	}

	public static float currentTime
	{
		get
		{
			if (!isPlaying)
			{
				return -1f;
			}
			return instance.currentSource.time;
		}
		set
		{
			if (isPlaying)
			{
				instance.currentSource.time = value;
			}
		}
	}

	private void SwapSources()
	{
		sourceIdx = (sourceIdx + 1) % 2;
	}

	private void Awake()
	{
		if ((bool)instance)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		Object.DontDestroyOnLoad(base.gameObject);
		SetAtten(maxAtten);
	}

	private void Start()
	{
		GameSettings.OnAppliedSettings += ApplyGameSettings;
		ApplyGameSettings(GameSettings.CurrentSettings);
	}

	private void ApplyGameSettings(GameSettings s)
	{
		SetVolume(s.GetFloatSetting("BGM_VOLUME") / 100f);
	}

	public static void Stop()
	{
		if ((bool)instance)
		{
			instance._Stop();
			instance.currID = "noSong.foo";
		}
	}

	private void _Stop()
	{
		Debug.Log("BGMManager._Stop()");
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		if (fadeToRoutine != null)
		{
			StopCoroutine(fadeToRoutine);
		}
		currentSource.Stop();
		otherSource.Stop();
		SetAtten(minAtten);
	}

	public static void FadeOut(float fadeTime = 2f)
	{
		if ((bool)instance)
		{
			instance._FadeOut(fadeTime);
			instance.currID = "noSong.foo";
		}
	}

	private void _FadeOut(float fadeTime)
	{
		Debug.Log("BGMManager FadeOut");
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(0f, play: false, fadeTime));
	}

	public static void FadeIn(float fadeTime = 2f)
	{
		if ((bool)instance)
		{
			instance._FadeIn(fadeTime);
		}
	}

	private void _FadeIn(float fadeTime)
	{
		Debug.Log("BGMManager FadeIn");
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
		fadeRoutine = StartCoroutine(FadeRoutine(1f, play: true, fadeTime));
	}

	public static void FadeTo(AudioClip newTrack, float fadeInTime, bool loop = true, string id = null)
	{
		if (!CockpitRadio.instance || !CockpitRadio.instance.isPlaying)
		{
			if ((bool)instance)
			{
				instance._FadeTo(newTrack, fadeInTime, loop, id);
			}
			else
			{
				Debug.Log("Tried to fade BGM to new track but BGMManager instance doesn't exist!");
			}
		}
	}

	private void _FadeTo(AudioClip newTrack, float fadeInTime, bool loop, string id)
	{
		isLoopingBGM = loop;
		FadeIn(fadeInTime);
		if (currentSource.clip == newTrack && currentSource.isPlaying)
		{
			currentSource.loop = loop;
			return;
		}
		if (currentSource.isPlaying && !string.IsNullOrEmpty(id) && id == currID)
		{
			if (!(currentSource.time > currentSource.clip.length) && currentSource.time != 0f)
			{
				Debug.Log("BGMManager: Same ID.  not fading to new track. Time: " + currentSource.time + ", clip length: " + currentSource.clip.length);
				currentSource.loop = loop;
				return;
			}
			Debug.Log("BGMManager: Same ID but current source time is beyond clip length or zero.");
		}
		else
		{
			if (fadeToRoutine != null)
			{
				Debug.Log("BGM fadeToRoutine is active!");
			}
			if (!currentSource.isPlaying)
			{
				Debug.Log("BGM currentSource is not playing!");
			}
		}
		Debug.Log("Fading to new ID: " + id + ", (oldID: " + currID + ")");
		currID = id;
		if (fadeToRoutine != null)
		{
			StopCoroutine(fadeToRoutine);
			Debug.Log("fadeToRoutine != null->currentSource.Stop()");
			currentSource.Stop();
		}
		if (fadeInTime < 0.01f)
		{
			currentSource.clip = newTrack;
			currentSource.volume = 1f;
			currentSource.Play();
			otherSource.volume = 0f;
			otherSource.Stop();
			otherSource.clip = null;
		}
		else
		{
			fadeToRoutine = StartCoroutine(FadeToRoutine(newTrack, fadeInTime, loop));
		}
	}

	private IEnumerator FadeToRoutine(AudioClip newTrack, float fadeInTime, bool loop)
	{
		Debug.Log("FadeToRoutine fadeInTime:" + fadeInTime);
		otherSource.clip = newTrack;
		otherSource.loop = loop;
		otherSource.Play();
		SwapSources();
		float t = 0f;
		float rate = 1f / fadeInTime;
		while (t < 1f)
		{
			t = Mathf.MoveTowards(t, 1f, rate * Time.unscaledDeltaTime);
			otherSource.volume = 1f - t;
			currentSource.volume = t;
			yield return null;
		}
		otherSource.volume = 0f;
		otherSource.Stop();
		otherSource.clip = null;
		currentSource.volume = 1f;
		fadeToRoutine = null;
	}

	private IEnumerator FadeRoutine(float tgt, bool play, float fadeTime)
	{
		float fadeRate = 1f / Mathf.Max(0.001f, fadeTime);
		AudioSource[] array;
		if (play)
		{
			array = audioSources;
			foreach (AudioSource audioSource in array)
			{
				if (!audioSource.isPlaying)
				{
					audioSource.Play();
				}
			}
		}
		while (currAttenT != tgt)
		{
			currAttenT = Mathf.MoveTowards(currAttenT, tgt, fadeRate * Time.unscaledDeltaTime);
			SetAtten(currAttenT);
			yield return null;
		}
		if (play)
		{
			yield break;
		}
		array = audioSources;
		foreach (AudioSource audioSource2 in array)
		{
			if (audioSource2.isPlaying)
			{
				audioSource2.Pause();
			}
		}
	}

	private void SetAtten(float attenT)
	{
		currAttenT = attenT;
		mixer.SetFloat("ReadyRoomBGMAttenuation", Mathf.Lerp(minAtten, maxAtten, attenFadeCurve.Evaluate(attenT)));
	}

	public static void SetVolume(float v)
	{
		if ((bool)instance)
		{
			float value = Mathf.Lerp(-80f, 10f, Mathf.Sqrt(Mathf.Clamp01(v)));
			instance.mixer.SetFloat("BGMVolumeSetting", value);
		}
	}

	public static void SetBGMCommDucker()
	{
		if ((bool)instance)
		{
			instance.mixer.SetFloat("BGMCommDuckerVolume", VTOLVRConstants.COMM_BGM_DUCKER_ATTEN);
		}
	}

	public static void ReleaseBGMCommDucker()
	{
		if ((bool)instance)
		{
			instance.mixer.SetFloat("BGMCommDuckerVolume", 0f);
		}
	}
}
