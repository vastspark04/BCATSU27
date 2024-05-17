using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
	private struct ScreenFadeOrder
	{
		public uint orderID;

		public bool fadeOut;

		public Color color;

		public float time;

		public bool fadeoutVolume;
	}

	private static List<ScreenFader> instances = new List<ScreenFader>();

	private static ScreenFadeOrder currentOrder;

	private static uint nextOrderID = 1u;

	private uint instOrderID;

	public float maxAlpha = 1f;

	public GameObject canvasObject;

	public Image fadeImage;

	private Coroutine fadeRoutine;

	private float currentAlpha = 1f;

	private AudioMixer envMixer;

	private static float nrmVol = 0f;

	private void Awake()
	{
		instances.Add(this);
		envMixer = VTResources.GetEnvironmentAudioMixer();
		currentAlpha = maxAlpha;
		SetNormVol(nrmVol);
		fadeImage.color = new Color(0f, 0f, 0f, maxAlpha);
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	private IEnumerator EnableRoutine()
	{
		yield return null;
		if (currentOrder.orderID != 0)
		{
			instOrderID = currentOrder.orderID;
			if (currentOrder.fadeOut)
			{
				_FadeOut(currentOrder.color, currentOrder.fadeoutVolume, currentOrder.time);
			}
			else
			{
				_FadeIn(currentOrder.time);
			}
		}
	}

	private void OnDestroy()
	{
		if (instances != null)
		{
			instances.Remove(this);
		}
	}

	public static void FadeOut(float _time = 1f)
	{
		FadeOut(Color.black, _time);
	}

	public static void FadeOut(Color _color, float _time = 1f, bool fadeoutVolume = true)
	{
		ScreenFadeOrder screenFadeOrder = default(ScreenFadeOrder);
		screenFadeOrder.orderID = nextOrderID;
		screenFadeOrder.fadeOut = true;
		screenFadeOrder.color = _color;
		screenFadeOrder.time = _time;
		screenFadeOrder.fadeoutVolume = fadeoutVolume;
		currentOrder = screenFadeOrder;
		nextOrderID++;
		if (instances == null)
		{
			return;
		}
		for (int i = 0; i < instances.Count; i++)
		{
			if ((bool)instances[i] && instances[i].gameObject.activeInHierarchy)
			{
				instances[i]._FadeOut(_color, fadeoutVolume, _time);
			}
		}
	}

	private void _FadeOut(Color color, bool fadeoutVolume, float time = 1f)
	{
		if (time <= 0f)
		{
			FadeOutImmediate(color, fadeoutVolume);
			return;
		}
		StopFadeRoutine();
		fadeRoutine = StartCoroutine(FadeOutRoutine(time, color, fadeoutVolume));
		Debug.Log("Fading out");
	}

	private void FadeOutImmediate(Color color, bool fadeoutVolume)
	{
		StopFadeRoutine();
		color.a = maxAlpha;
		fadeImage.color = color;
		if (fadeoutVolume)
		{
			SetNormVol(0f);
		}
		canvasObject.SetActive(value: true);
	}

	public static void FadeIn(float _time = 1f)
	{
		ScreenFadeOrder screenFadeOrder = default(ScreenFadeOrder);
		screenFadeOrder.orderID = nextOrderID;
		screenFadeOrder.fadeOut = false;
		screenFadeOrder.time = _time;
		currentOrder = screenFadeOrder;
		nextOrderID++;
		if (instances == null)
		{
			return;
		}
		for (int i = 0; i < instances.Count; i++)
		{
			if ((bool)instances[i] && instances[i].gameObject.activeInHierarchy)
			{
				instances[i]._FadeIn(_time);
			}
		}
	}

	private void _FadeIn(float time = 1f)
	{
		StopFadeRoutine();
		Debug.Log($"Fading in ({time})");
		if (time <= 0f)
		{
			FadeInImmediate();
		}
		else
		{
			fadeRoutine = StartCoroutine(FadeInRoutine(time));
		}
	}

	private void FadeInImmediate()
	{
		StopFadeRoutine();
		fadeImage.color = Color.clear;
		SetNormVol(1f);
		canvasObject.SetActive(value: false);
	}

	private void SetNormVol(float t)
	{
		nrmVol = Mathf.Clamp01(t);
		envMixer.SetFloat("TransitionFade", Mathf.Lerp(-80f, 0f, t));
	}

	private IEnumerator FadeOutRoutine(float time, Color color, bool fadeoutVolume)
	{
		canvasObject.SetActive(value: true);
		float deltaAlpha = maxAlpha / time;
		Color currentColor = color;
		while (currentAlpha < maxAlpha)
		{
			currentColor.a = currentAlpha;
			fadeImage.color = currentColor;
			currentAlpha += deltaAlpha * Time.deltaTime;
			if (fadeoutVolume)
			{
				SetNormVol(Mathf.Min(1f - currentAlpha / maxAlpha, nrmVol));
			}
			yield return null;
		}
		currentAlpha = maxAlpha;
		currentColor.a = maxAlpha;
		fadeImage.color = currentColor;
		if (fadeoutVolume)
		{
			SetNormVol(0f);
		}
	}

	private IEnumerator FadeInRoutine(float time)
	{
		float deltaAlpha = maxAlpha / time;
		Color currentColor = fadeImage.color;
		currentAlpha = currentColor.a;
		while (currentAlpha > 0f)
		{
			currentColor.a = currentAlpha;
			fadeImage.color = currentColor;
			currentAlpha -= deltaAlpha * Time.deltaTime;
			SetNormVol(Mathf.Max(nrmVol, 1f - currentAlpha / maxAlpha));
			yield return null;
		}
		currentAlpha = 0f;
		currentColor.a = 0f;
		fadeImage.color = currentColor;
		SetNormVol(1f);
		canvasObject.SetActive(value: false);
	}

	private void StopFadeRoutine()
	{
		if (fadeRoutine != null)
		{
			StopCoroutine(fadeRoutine);
		}
	}
}
