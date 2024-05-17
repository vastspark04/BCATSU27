using System.Collections;
using Rewired;
using Steamworks;
using UnityEngine;
using UnityEngine.CrashReportHandler;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using UnityEngine.XR;

public class SplashSceneController : MonoBehaviour
{
	public GameObject errorObj;

	public Text errorText;

	public float fogLerpRate = 2f;

	public Renderer[] logoRenderers;

	public Renderer planeRenderer;

	private bool entitlementValid;

	private bool entitlementChecked;

	public Cubemap overrideSkyTexture;

	public Cubemap overrideFogTexture;

	private void DisplayError(string msg)
	{
		errorObj.SetActive(value: true);
		errorText.text = msg;
	}

	private void Awake()
	{
		Debug.Log(GameStartup.versionString);
		if (SteamClient.IsValid)
		{
			Debug.Log("Steamworks initialized!");
		}
		else
		{
			Debug.Log("Steamworks disabled.");
		}
		VTLocalizationManager.Init();
		SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		GameSettings.VR_SDK_ID = XRSettings.loadedDeviceName;
		Debug.Log("Initial VR SDK: " + GameSettings.VR_SDK_ID);
		if (PhraseRecognitionSystem.isSupported)
		{
			Debug.Log("PhraseRecognitionSystem.isSupported is true.  Warming up keyword recognizer.");
			KeywordRecognizer keywordRecognizer = new KeywordRecognizer(new string[1] { "Warmup" });
			keywordRecognizer.Start();
			keywordRecognizer.Stop();
			keywordRecognizer.Dispose();
			Debug.Log("KeywordRecognizer successfully started and stopped.");
		}
		else
		{
			Debug.Log("PhraseRecognitionSystem.isSupported is false.  Disabling keyword recognition.");
		}
		if (GameSettings.TryGetGameSettingValue<string>("LANGUAGE", out var val))
		{
			VTLocalizationManager.SetLanguage(val);
		}
		VTResources.LoadAllResources();
		entitlementChecked = true;
		entitlementValid = true;
		planeRenderer.sharedMaterial.renderQueue = 2999;
		Shader.SetGlobalFloat("_SkyAltVertDiv", 70000f);
		Shader.SetGlobalFloat("_SkyAltColorDiv", 10000f);
		Shader.SetGlobalFloat("_SphericalClipFactor", 0.8f);
		Shader.SetGlobalTexture("_GlobalSkyCube", overrideSkyTexture);
		Shader.SetGlobalTexture("_GlobalSkyCubeHigh", overrideSkyTexture);
		Shader.SetGlobalTexture("_GlobalFogCube", overrideFogTexture);
		Shader.SetGlobalFloat("_GlobalCubeRotation", 0f);
		Shader.SetGlobalFloat("_SinGlobalCubeRotation", Mathf.Sin(0f));
		Shader.SetGlobalFloat("_CosGlobalCubeRotation", Mathf.Cos(0f));
		SetupRudderDebug();
		if (GameSettings.TryGetGameSettingValue<float>("FIXED_TIMESTEP", out var val2))
		{
			Time.fixedDeltaTime = val2;
		}
	}

	private static void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log("Loaded scene: " + scene.name);
		CrashReportHandler.SetUserMetadata("lastLoadedScene", scene.buildIndex + ": " + scene.name);
	}

	private void Start()
	{
		StartCoroutine(StartupRoutine());
		StartCoroutine(FogRoutine());
	}

	private IEnumerator FogRoutine()
	{
		CameraFogSettings fogSettings = GetComponent<CameraFogSettings>();
		if ((bool)fogSettings)
		{
			while (base.enabled)
			{
				fogSettings.density = Mathf.Lerp(fogSettings.density, 0.007f, fogLerpRate * Time.deltaTime);
				yield return null;
			}
		}
	}

	private IEnumerator StartupRoutine()
	{
		ScreenFader.FadeIn(2f);
		while (!entitlementChecked)
		{
			yield return null;
		}
		if (entitlementValid)
		{
			yield return new WaitForSeconds(2f);
			if (GameSettings.forceSynchronousLoading)
			{
				ScreenFader.FadeOut(Color.black, 2f);
				StartCoroutine(FadeOutLogosRoutine());
				yield return new WaitForSeconds(3f);
				SceneManager.LoadScene("SamplerScene");
				yield break;
			}
			AsyncOperation load = SceneManager.LoadSceneAsync("SamplerScene");
			load.allowSceneActivation = false;
			while (load.progress < 0.9f)
			{
				yield return null;
			}
			ScreenFader.FadeOut(Color.black, 2f);
			StartCoroutine(FadeOutLogosRoutine());
			yield return new WaitForSeconds(3f);
			load.allowSceneActivation = true;
		}
		else
		{
			yield return new WaitForSeconds(10f);
			Debug.LogError("Authentication failed!");
			Application.Quit();
		}
	}

	private IEnumerator FadeOutLogosRoutine()
	{
		MaterialPropertyBlock props = new MaterialPropertyBlock();
		Color c = Color.white;
		float t = 1f;
		while (t > 0f)
		{
			t = (c.a = t - Time.deltaTime / 2f);
			props.SetColor("_Color", c);
			Renderer[] array = logoRenderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetPropertyBlock(props);
			}
			yield return null;
		}
	}

	public void OnVroteinAuthResult(bool bResult, int iResult, string strResult)
	{
	}

	private void SetupRudderDebug()
	{
		ReInput.ControllerConnectedEvent += delegate(ControllerStatusChangedEventArgs args)
		{
			Debug.LogFormat("ReWired: Controller Connected Event: Name='{0}' ControllerType='{1}', ControllerID='{2}'", args.name, args.controllerType, args.controllerId);
		};
		ReInput.ControllerDisconnectedEvent += delegate(ControllerStatusChangedEventArgs args)
		{
			Debug.LogFormat("ReWired: Controller Disconnected Event: Name='{0}' ControllerType='{1}', ControllerID='{2}'", args.name, args.controllerType, args.controllerId);
		};
		ReInput.ControllerPreDisconnectEvent += delegate(ControllerStatusChangedEventArgs args)
		{
			Debug.LogFormat("ReWired: Controller PreDisconnect Event: Name='{0}' ControllerType='{1}', ControllerID='{2}'", args.name, args.controllerType, args.controllerId);
		};
	}
}
