using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using Valve.VR;

public class LoadingSceneController : MonoBehaviour
{
	public class LoadingSceneSwitcher : MonoBehaviour
	{
		private void Awake()
		{
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}

		public void LoadScene(int sceneIndex)
		{
			StartCoroutine(SceneSwitch(sceneIndex));
		}

		private IEnumerator SceneSwitch(int index)
		{
			SceneManager.LoadScene("LoadingScene");
			while (!ready || !instance)
			{
				yield return null;
			}
			StartLoadSequence(index);
			UnityEngine.Object.Destroy(base.gameObject);
		}

		public void LoadSceneVTEdit(int sceneIndex)
		{
			StartCoroutine(SceneSwitchVTEdit(sceneIndex));
		}

		private IEnumerator SceneSwitchVTEdit(int index)
		{
			if (GameSettings.forceSynchronousLoading)
			{
				_ready = false;
				SceneManager.LoadScene(index);
				yield break;
			}
			SceneManager.LoadScene("VTEditLoadingScene");
			while (!ready)
			{
				yield return null;
			}
			yield return null;
			StartVTLoadSequence(index);
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class StandardToVRSwitcher : MonoBehaviour
	{
		private bool ls;

		public void SwitchToScene(int sceneIndex, bool loadingScene)
		{
			ls = loadingScene;
			StartCoroutine(SwitchRoutine(sceneIndex));
		}

		private IEnumerator SwitchRoutine(int sceneIndex)
		{
			if (!XRSettings.enabled)
			{
				if (GameSettings.VR_SDK_IS_OCULUS)
				{
					XRSettings.LoadDeviceByName(GameSettings.VR_SDK_ID);
					yield return null;
					XRSettings.enabled = true;
				}
				else
				{
					Debug.Log("Reinitializing SteamVR. Current state: " + SteamVR.initializedState);
					XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
					XRGeneralSettings.Instance.Manager.StartSubsystems();
					XRSettings.LoadDeviceByName("OpenVR");
					if (0 == 0)
					{
						SteamVR.Initialize(forceUnityVRMode: true);
						while (!XRSettings.enabled || SteamVR.initializedState != SteamVR.InitializedStates.InitializeSuccess)
						{
							yield return null;
						}
					}
					else
					{
						Debug.LogError("OpenVR Timed out.");
					}
				}
			}
			if (ls)
			{
				LoadScene(sceneIndex);
			}
			else
			{
				LoadSceneImmediate(sceneIndex);
			}
		}
	}

	private static bool _ready;

	private int sceneIndex;

	private bool playerReady;

	private static float _loadPercent;

	public bool usePredictiveProgress;

	private bool tgtSceneLoaded;

	public Text progressText;

	public GameObject progressUIObject;

	public GameObject readyUIObject;

	public PixelSphereFader pixelFader;

	public static LoadingSceneController instance { get; private set; }

	public static bool ready => _ready;

	public static float loadPercent => _loadPercent;

	public event Action OnResetLoadingBar;

	public static void LoadScene(int sceneIndex)
	{
		new GameObject("SceneSwitcher").AddComponent<LoadingSceneSwitcher>().LoadScene(sceneIndex);
	}

	public static void LoadScene(string sceneName)
	{
		LoadScene(SceneUtility.GetBuildIndexByScenePath(sceneName));
	}

	public static void LoadVTEditScene(string sceneName)
	{
		int buildIndexByScenePath = SceneUtility.GetBuildIndexByScenePath(sceneName);
		new GameObject("SceneSwitcher").AddComponent<LoadingSceneSwitcher>().LoadSceneVTEdit(buildIndexByScenePath);
	}

	public static void LoadSceneImmediate(int sceneIndex)
	{
		SceneManager.LoadScene(sceneIndex);
	}

	public static void LoadSceneImmediate(string sceneName)
	{
		LoadSceneImmediate(SceneUtility.GetBuildIndexByScenePath(sceneName));
	}

	private static void StartLoadSequence(int sceneIndex)
	{
		instance.Load(sceneIndex);
	}

	private static void StartVTLoadSequence(int sceneIndex)
	{
		instance.LoadVTEdit(sceneIndex);
	}

	private void LoadVTEdit(int sceneIndex)
	{
		this.sceneIndex = sceneIndex;
		StartCoroutine(VTLoadSceneRoutine());
	}

	private void OnDestroy()
	{
		Debug.Log("LoadingSceneController destroyed.");
	}

	private IEnumerator VTLoadSceneRoutine()
	{
		VRUtils.DisableVR();
		ScreenFader.FadeIn();
		Application.backgroundLoadingPriority = ThreadPriority.Low;
		tgtSceneLoaded = false;
		SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		SceneManager.GetSceneByBuildIndex(sceneIndex);
		AsyncOperation load = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);
		load.allowSceneActivation = false;
		usePredictiveProgress = true;
		while (load.progress < 0.9f)
		{
			_loadPercent = load.progress;
			yield return null;
		}
		load.allowSceneActivation = true;
		BGMManager.FadeOut();
		_loadPercent = 0f;
		Debug.Log("Waiting for scene to finally load.");
		while (!tgtSceneLoaded)
		{
			yield return null;
		}
		SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
		if ((bool)FlightSceneManager.instance)
		{
			usePredictiveProgress = false;
			if (this.OnResetLoadingBar != null)
			{
				this.OnResetLoadingBar();
			}
			Debug.Log("Awaiting flight scene loading");
			while (!FlightSceneManager.instance.SceneLoadFinished())
			{
				_loadPercent = FlightSceneManager.instance.SceneLoadPercent();
				yield return null;
			}
		}
		Debug.Log("Unloading loading scene");
		SceneManager.UnloadSceneAsync("VTEditLoadingScene");
		_ready = false;
		ControllerEventHandler.UnpauseEvents();
		yield return null;
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
	{
		SceneManager.SetActiveScene(arg0);
		tgtSceneLoaded = true;
	}

	private void Load(int sceneIndex)
	{
		this.sceneIndex = sceneIndex;
		StartCoroutine(LoadSceneRoutine());
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		_loadPercent = 0f;
		_ready = true;
		ScreenFader.FadeIn();
	}

	private IEnumerator LoadSceneRoutine()
	{
		ScreenFader.FadeIn();
		if (GameSettings.forceSynchronousLoading)
		{
			while (!playerReady)
			{
				yield return null;
			}
			ControllerEventHandler.PauseEvents();
			yield return new WaitForSeconds(1f);
			ScreenFader.FadeOut(Color.black, 1f / pixelFader.fadeRate);
			pixelFader.FadeToBlack();
			while (pixelFader.currentValue < 0f)
			{
				yield return null;
			}
			SceneManager.LoadScene(sceneIndex);
		}
		else
		{
			Application.backgroundLoadingPriority = ThreadPriority.Low;
			AsyncOperation load = SceneManager.LoadSceneAsync(sceneIndex);
			load.allowSceneActivation = false;
			while (load.progress < 0.9f)
			{
				_loadPercent = load.progress;
				yield return null;
			}
			_loadPercent = 1f;
			progressUIObject.SetActive(value: false);
			readyUIObject.SetActive(value: true);
			while (!playerReady)
			{
				yield return null;
			}
			ControllerEventHandler.PauseEvents();
			yield return new WaitForSeconds(1f);
			ScreenFader.FadeOut(Color.black, 1f / pixelFader.fadeRate);
			pixelFader.FadeToBlack();
			while (pixelFader.currentValue < 0f)
			{
				yield return null;
			}
			load.allowSceneActivation = true;
		}
		_loadPercent = 0f;
		_ready = false;
		ControllerEventHandler.UnpauseEvents();
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void Update()
	{
		if ((bool)progressText)
		{
			progressText.text = Mathf.Round(loadPercent * 100f) + " %";
		}
		if (Input.GetKeyDown(KeyCode.R))
		{
			PlayerReady();
		}
	}

	public void PlayerReady()
	{
		playerReady = true;
	}

	public static void SwitchToVRScene(string sceneName, bool loadingScene = false)
	{
		if (!string.IsNullOrEmpty(GameSettings.VR_SDK_ID))
		{
			SwitchToVRScene(SceneUtility.GetBuildIndexByScenePath(sceneName), loadingScene);
		}
		else if (loadingScene)
		{
			LoadVTEditScene(sceneName);
		}
		else
		{
			LoadSceneImmediate(sceneName);
		}
	}

	public static void SwitchToVRScene(int sceneIdx, bool loadingScene = false)
	{
		new GameObject().AddComponent<StandardToVRSwitcher>().SwitchToScene(sceneIdx, loadingScene);
	}

	public static void ReloadSceneImmediately()
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}
