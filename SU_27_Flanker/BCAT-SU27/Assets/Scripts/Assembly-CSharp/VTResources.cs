using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;
using VTNetworking;
using VTOLVR.SteamWorkshop;

public static class VTResources
{
	public class AWACSVoiceLoader : AsyncResourceLoader<AWACSVoiceProfile>
	{
	}

	public class GroundCrewVoiceLoader : AsyncResourceLoader<GroundCrewVoiceProfile>
	{
	}

	public class WingmanVoiceLoader2 : AsyncResourceLoader<WingmanVoiceProfile>
	{
	}

	public class WingmanVoiceLoader : MonoBehaviour
	{
		private Dictionary<string, WingmanVoiceProfile> dictionary;

		public event Action OnFinishedLoading;

		public void Load(string builtinWingmanVoicesResourcesPath, Dictionary<string, WingmanVoiceProfile> dictionary)
		{
			this.dictionary = dictionary;
			StartCoroutine(LoadRoutine(builtinWingmanVoicesResourcesPath));
		}

		private IEnumerator LoadRoutine(string builtinWingmanVoicesResourcesPath)
		{
			UnityEngine.Debug.Log("Loading wingman voice profiles ASYNC.");
			ScriptableStringList scriptableStringList = Resources.Load<ScriptableStringList>(builtinWingmanVoicesResourcesPath + "/VoiceManifest");
			float t = Time.realtimeSinceStartup;
			List<ResourceRequest> list = new List<ResourceRequest>();
			foreach (string item2 in scriptableStringList.list)
			{
				ResourceRequest item = Resources.LoadAsync<WingmanVoiceProfile>(builtinWingmanVoicesResourcesPath + "/" + item2);
				list.Add(item);
			}
			foreach (ResourceRequest async in list)
			{
				while (!async.isDone)
				{
					yield return null;
				}
				WingmanVoiceProfile wingmanVoiceProfile = (WingmanVoiceProfile)async.asset;
				if (GameSettings.enabledWingmanVoices != null)
				{
					wingmanVoiceProfile.enabled = GameSettings.enabledWingmanVoices.Contains(wingmanVoiceProfile.name);
					if (!wingmanVoiceProfile.enabled && !string.IsNullOrEmpty(wingmanVoiceProfile.entryVersion) && ConfigNodeUtils.ParseObject<GameVersion>(wingmanVoiceProfile.entryVersion) > currVoiceEntryVersion)
					{
						wingmanVoiceProfile.enabled = true;
						GameSettings.enabledWingmanVoices.Add(wingmanVoiceProfile.name);
					}
				}
				dictionary.Add(wingmanVoiceProfile.name, wingmanVoiceProfile);
			}
			currVoiceEntryVersion = GameStartup.version;
			UnityEngine.Debug.Log("Finished loading wingman voice profiles ASYNC. (" + (Time.realtimeSinceStartup - t) + " s)");
			if (this.OnFinishedLoading != null)
			{
				this.OnFinishedLoading();
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class AsyncResourceLoader<T> : MonoBehaviour where T : UnityEngine.Object
	{
		public event Action OnFinishedLoading;

		public event Action<T> OnLoadedAsset;

		public void Load(string resourcesPath, string manifestName)
		{
			if (!Application.isPlaying)
			{
				UnityEngine.Debug.LogError("Async voice loader was created when the App is not in playmode!!");
				return;
			}
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			StartCoroutine(LoadRoutine(resourcesPath, manifestName));
		}

		private IEnumerator LoadRoutine(string resourcesPath, string manifestName)
		{
			UnityEngine.Debug.LogFormat("Loading resources of type {0} ASYNC.", typeof(T));
			ScriptableStringList scriptableStringList = Resources.Load<ScriptableStringList>(resourcesPath + "/" + manifestName);
			Stopwatch sw = new Stopwatch();
			sw.Start();
			foreach (string item in scriptableStringList.list)
			{
				string text = resourcesPath + "/" + item;
				UnityEngine.Debug.Log(" Async loading: " + text);
				ResourceRequest async = Resources.LoadAsync<T>(text);
				while (!async.isDone)
				{
					yield return null;
				}
				T obj = (T)async.asset;
				this.OnLoadedAsset?.Invoke(obj);
			}
			sw.Stop();
			float num = (float)sw.ElapsedMilliseconds / 1000f;
			UnityEngine.Debug.LogFormat("Finished loading assets of type {0} ASYNC. ({1} s)", typeof(T), num);
			this.OnFinishedLoading?.Invoke();
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class ResourcesAsyncScript : MonoBehaviour
	{
		public IEnumerator LoadRoutine(AsyncOpStatus status)
		{
			yield return null;
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class AudioClipLoadBehaviour : MonoBehaviour
	{
		public void StartLoadRoutine(AudioClip clip, WWW www)
		{
			StartCoroutine(LoadRoutine(clip, www));
		}

		private IEnumerator LoadRoutine(AudioClip clip, WWW www)
		{
			while (clip.loadState != AudioDataLoadState.Loaded)
			{
				yield return null;
			}
			www.Dispose();
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	public class AsyncMp3ClipLoader
	{
		public class AsyncMp3ClipLoaderBehaviour : MonoBehaviour
		{
			private Action<AudioClip> onComplete;

			public void Begin(string path, Action<AudioClip> onComplete)
			{
				this.onComplete = onComplete;
				ReaderCreationStatus rStatus = new ReaderCreationStatus(path);
				StartCoroutine(WaitRoutine(rStatus));
			}

			private IEnumerator WaitRoutine(ReaderCreationStatus rStatus)
			{
				ThreadPool.QueueUserWorkItem(AsyncCreateReader, rStatus);
				while (!rStatus.ready)
				{
					yield return null;
				}
				AudioClip audioClip = AudioClip.Create(rStatus.audioFilePath, rStatus.reader.LengthSamples, rStatus.reader.WaveFormat.Channels, rStatus.reader.WaveFormat.SampleRate, stream: false);
				audioClip.SetData(rStatus.samples, 0);
				onComplete(audioClip);
				UnityEngine.Object.Destroy(base.gameObject);
			}

			private void AsyncCreateReader(object status)
			{
				ReaderCreationStatus readerCreationStatus = (ReaderCreationStatus)status;
				try
				{
					readerCreationStatus.reader = new Mp3FileReaderVT.AudioFileReader(readerCreationStatus.audioFilePath);
					readerCreationStatus.ready = true;
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogError("Error attempting to open stream for " + readerCreationStatus.audioFilePath + "\n" + ex);
				}
			}
		}

		private class ReaderCreationStatus
		{
			public string audioFilePath;

			public Mp3FileReaderVT.AudioFileReader reader;

			private bool _ready;

			private object statusLock;

			private float[] _samples;

			private byte[] readBuffer = new byte[1];

			private WaveBuffer waveBuffer;

			public bool ready
			{
				get
				{
					lock (statusLock)
					{
						return _ready;
					}
				}
				set
				{
					if (value)
					{
						if (reader != null)
						{
							int num = reader.WaveFormat.BitsPerSample / 8;
							int num2 = (int)(reader.Length / num);
							_samples = new float[num2];
							int num3 = (int)reader.Length;
							if (num3 > readBuffer.Length)
							{
								readBuffer = new byte[num3];
								waveBuffer = new WaveBuffer(readBuffer);
							}
							else if (waveBuffer == null)
							{
								waveBuffer = new WaveBuffer(readBuffer);
							}
							int num4 = reader.Read(readBuffer, 0, num3);
							for (int i = 0; i < num4 && i < _samples.Length && i < waveBuffer.FloatBuffer.Length; i++)
							{
								_samples[i] = waveBuffer.FloatBuffer[i];
							}
						}
						lock (statusLock)
						{
							_ready = true;
							return;
						}
					}
					lock (statusLock)
					{
						_ready = false;
					}
				}
			}

			public float[] samples
			{
				get
				{
					if (ready)
					{
						return _samples;
					}
					return null;
				}
			}

			public ReaderCreationStatus(string path)
			{
				audioFilePath = path;
				statusLock = new object();
			}

			~ReaderCreationStatus()
			{
				if (waveBuffer != null)
				{
					waveBuffer.Clear();
					waveBuffer = null;
				}
				if (reader != null)
				{
					reader.Dispose();
					reader = null;
				}
				_samples = null;
			}
		}

		public AudioClip clip { get; private set; }

		public AsyncMp3ClipLoader(string path)
		{
			new GameObject("AsyncMp3ClipLoader").AddComponent<AsyncMp3ClipLoaderBehaviour>().Begin(path, delegate(AudioClip c)
			{
				clip = c;
			});
		}
	}

	public class WorkshopMapListRequest
	{
		public bool isDone;

		public float progress;

		public List<VTMapCustom> maps;
	}

	private class MapListRequestBehaviour : MonoBehaviour
	{
		private WorkshopMapListRequest req;

		private bool previewsOnly;

		public void Request(WorkshopMapListRequest req, bool previewsOnly)
		{
			this.req = req;
			this.previewsOnly = previewsOnly;
			StartCoroutine(RequestRoutine());
		}

		private IEnumerator RequestRoutine()
		{
			UnityEngine.Debug.Log("Beginning workshop map request routine.");
			Query query = Query.Items.WhereUserSubscribed(SteamClient.SteamId).WithTag("Maps");
			bool gotAll = false;
			int page = 1;
			List<Item> items = new List<Item>();
			while (!gotAll)
			{
				UnityEngine.Debug.Log($" - querying page {page}");
				Task<ResultPage?> task = query.GetPageAsync(page);
				while (!task.IsCompleted)
				{
					yield return null;
				}
				if (task.Result.HasValue && task.Result.Value.ResultCount > 0)
				{
					UnityEngine.Debug.Log($" - got page {page} with {task.Result.Value.ResultCount} results");
					foreach (Item entry in task.Result.Value.Entries)
					{
						items.Add(entry);
					}
					if (items.Count == task.Result.Value.TotalCount)
					{
						gotAll = true;
						continue;
					}
					page++;
					yield return null;
				}
				else
				{
					gotAll = true;
				}
			}
			UnityEngine.Debug.Log(" - done querying.  Now loading items");
			req.maps = new List<VTMapCustom>();
			int totalCount = items.Count;
			int loadedCount = 0;
			foreach (Item item in items)
			{
				if (!item.IsDownloading && Directory.Exists(item.Directory))
				{
					string dir = item.Directory;
					string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string text in files)
					{
						ConfigNode configNode;
						if (text.EndsWith(".vtm"))
						{
							configNode = ConfigNode.LoadFromFile(text);
						}
						else
						{
							if (!text.EndsWith(".vtmb"))
							{
								continue;
							}
							configNode = VTSteamWorkshopUtils.ReadWorkshopConfig(text);
						}
						string value = configNode.GetValue("mapID");
						UnityEngine.Debug.Log(" - Loading map " + value);
						value = value + "_" + item.Id.Value;
						configNode.SetValue("mapID", value);
						VTMapCustom vTMapCustom = ScriptableObject.CreateInstance<VTMapCustom>();
						vTMapCustom.LoadFromConfigNode(configNode, dir);
						vTMapCustom.isSteamWorkshopMap = true;
						vTMapCustom.isSWPreviewOnly = previewsOnly;
						if (!previewsOnly)
						{
							string text2 = Path.Combine(dir, "height.png");
							if (!File.Exists(text2))
							{
								text2 += "b";
							}
							if (File.Exists(text2))
							{
								vTMapCustom.heightMap = GetTexture(text2, mipmaps: false, linear: true);
							}
							string text3 = Path.Combine(dir, "height0.png");
							bool flag = false;
							if (!File.Exists(text3))
							{
								text3 += "b";
								if (File.Exists(text3))
								{
									flag = true;
								}
							}
							if (flag || File.Exists(text3))
							{
								int num = 0;
								UnityEngine.Debug.Log(" - - loading heightmap splits");
								List<Texture2D> list = new List<Texture2D>();
								while (File.Exists(text3))
								{
									list.Add(GetTexture(text3));
									UnityEngine.Debug.Log(" - - - loaded " + text3);
									num++;
									text3 = Path.Combine(dir, string.Format("height{0}.png{1}", num, flag ? "b" : ""));
								}
								vTMapCustom.splitHeightmaps = list.ToArray();
							}
						}
						string path = Path.Combine(dir, "preview.jpg");
						if (!File.Exists(path))
						{
							if (!previewsOnly)
							{
								Texture2D texture2D = RenderMapToPreview(vTMapCustom, 512);
								SaveToJpg(texture2D, path);
								UnityEngine.Object.DestroyImmediate(texture2D);
								vTMapCustom.previewImage = GetTexture(path, mipmaps: false);
							}
						}
						else
						{
							vTMapCustom.previewImage = GetTexture(path, mipmaps: false);
						}
						req.maps.Add(vTMapCustom);
						steamWorkshopMaps.Add(vTMapCustom.mapID, vTMapCustom);
						yield return null;
					}
				}
				loadedCount++;
				req.progress = (float)loadedCount / (float)totalCount;
			}
			req.progress = 1f;
			UnityEngine.Debug.Log("SW Map request is done");
			req.isDone = true;
		}
	}

	public delegate void RequestChangeNoteDelegate(Action<string> SetChangeNote, Action Cancel);

	private static Dictionary<string, VTScenarioInfo> customScenarios;

	private static Dictionary<string, VTCampaignInfo> customCampaigns;

	private static Dictionary<string, VTCampaignInfo> builtInCampaigns;

	private static BuiltInCampaigns builtInCampaignsObject;

	private static Dictionary<string, VTCampaignInfo> builtInTutorials;

	public static Dictionary<string, VTCampaignInfo> builtInMultiplayerCampaigns;

	private static Dictionary<string, VTMap> maps;

	private static Dictionary<string, VTMap> nonCustomMaps;

	private static PlayerVehicleList playerVehicles;

	private static VTMOOBProfileSet _oobSet;

	private static Dictionary<string, WingmanVoiceProfile> wingmanVoices;

	private static Dictionary<string, GroundCrewVoiceProfile> groundCrewVoices;

	private static Dictionary<string, AWACSVoiceProfile> awacsVoices;

	private static List<AWACSVoiceProfile> sortedAwacsVoices;

	private static Dictionary<string, VTMapCustom> customMaps = new Dictionary<string, VTMapCustom>();

	public static string[] supportedImageExtensions = new string[2] { ".png", ".jpg" };

	public static string[] supportedAudioExtensions = new string[2] { ".ogg", ".mp3" };

	public static string[] supportedVideoExtensions = new string[1] { ".mp4" };

	private static Dictionary<string, GameObject> vtEditStaticObjectPrefabs;

	public const string mapsResourcesPath = "VTMaps";

	public const string builtInCampaignsResourcesDir = "Campaigns";

	private const string builtInCampaignsObjectPath = "Campaigns/Campaigns";

	private const string staticObjectsResourcePath = "VTEdit/StaticObjects";

	public const string builtinWingmanVoicesResourcesPath = "VTEdit/WingmanVoiceProfiles";

	public const string builtinGroundCrewVoicesResourcesPath = "GroundCrewVoices";

	public const string builtinAwacsVoicesResourcesPath = "AWACSVoices";

	private const string defaultGroundCrewVoiceName = "DefaultGroundCrewVoice";

	private const string defaultTerrainSurfaceMaterialResourcePath = "WheelSurfaceMaterials/RoughGround";

	private static WheelSurfaceMaterial _defaultWsm;

	private static bool _useOverCloud = false;

	private static AudioMixerReference _amr;

	private static bool finishedLoadingVoices = false;

	private static bool _gotvoiceEntryVersion = false;

	private static GameVersion _currEntryVersion;

	private static bool loadedAwacsVoices = false;

	private static AWACSVoiceLoader awacsLoader;

	private static bool loadedGroundCrewVoices = false;

	private static GroundCrewVoiceLoader gcVoiceLoader;

	private static WingmanVoiceLoader2 wingLoader;

	private static List<uint> installedDLCs = new List<uint>();

	private static Dictionary<string, DateTime> campaignModifiedTimes = new Dictionary<string, DateTime>();

	private static Dictionary<string, DateTime> customScenarioModifiedTimes = new Dictionary<string, DateTime>();

	private static Dictionary<string, VTMapCustom> prevLoadedMaps = new Dictionary<string, VTMapCustom>();

	private static Dictionary<string, DateTime> prevLoadedMapModTimes = new Dictionary<string, DateTime>();

	private static char[] invalidFilenameChars = new char[10] { '/', '\\', ',', '{', '}', ':', '|', '=', '%', '$' };

	private static Dictionary<string, VTScenarioInfo> steamWorkshopScenarios = new Dictionary<string, VTScenarioInfo>();

	private static Dictionary<string, VTCampaignInfo> steamWorkshopCampaigns = new Dictionary<string, VTCampaignInfo>();

	private static bool workshopMapsDirty = true;

	private static Dictionary<string, VTMapCustom> steamWorkshopMaps = new Dictionary<string, VTMapCustom>();

	private static GameObject _mPrevSetup;

	public static VTMOOBProfileSet OOBSet
	{
		get
		{
			if (_oobSet == null)
			{
				_oobSet = Resources.Load<VTMOOBProfileSet>("OOBSet");
			}
			return _oobSet;
		}
	}

	public static string gameRootDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

	public static string customScenariosDir => Path.Combine(gameRootDirectory, "CustomScenarios");

	public static string customCampaignsDir => Path.Combine(customScenariosDir, "Campaigns");

	public static string vtEditResourceDir => Path.Combine(gameRootDirectory, "EditorResources");

	public static WheelSurfaceMaterial defaultTerrainSurfaceMaterial
	{
		get
		{
			if (_defaultWsm == null)
			{
				_defaultWsm = Resources.Load<WheelSurfaceMaterial>("WheelSurfaceMaterials/RoughGround");
			}
			return _defaultWsm;
		}
	}

	public static string customMapsDir => Path.Combine(gameRootDirectory, "CustomMaps");

	public static bool useOverCloud => false;

	public static string customMapSceneName
	{
		get
		{
			if (useOverCloud)
			{
				return "CustomMapBase_OverCloud";
			}
			return "CustomMapBase";
		}
	}

	private static AudioMixerReference audioMixerReference
	{
		get
		{
			if (!_amr)
			{
				_amr = Resources.Load<AudioMixerReference>("EnvironmentMixer");
			}
			return _amr;
		}
	}

	private static GameVersion currVoiceEntryVersion
	{
		get
		{
			if (!_gotvoiceEntryVersion)
			{
				_currEntryVersion = GameSettings.GetLoadedVoicesVersion();
				_gotvoiceEntryVersion = true;
			}
			return _currEntryVersion;
		}
		set
		{
			if (!_gotvoiceEntryVersion)
			{
				_currEntryVersion = GameSettings.GetLoadedVoicesVersion();
				_gotvoiceEntryVersion = true;
			}
			if (value != _currEntryVersion)
			{
				_currEntryVersion = value;
				GameSettings.SetLoadedVoicesVersion(_currEntryVersion);
			}
		}
	}

	public static bool isEditorOrDevTools => false;

	private static GameObject mapPreviewSetupPrefab
	{
		get
		{
			if (!_mPrevSetup)
			{
				_mPrevSetup = Resources.Load<GameObject>("VTMapEditor/Preview/VTMapPreviewSetup");
			}
			return _mPrevSetup;
		}
	}

	public static GameObject GetOverCloudRainFX()
	{
		return UnityEngine.Object.Instantiate(Resources.Load<GameObject>("MyOverCloud/OverCloudRainParticles"));
	}

	public static string GetMapDirectoryPath(string mapID)
	{
		return Path.Combine(customMapsDir, mapID);
	}

	public static string GetMapFilePath(string mapID)
	{
		return Path.Combine(GetMapDirectoryPath(mapID), mapID + ".vtm");
	}

	public static List<string> GetExistingMapFilenames()
	{
		List<string> list = new List<string>();
		if (!Directory.Exists(customMapsDir))
		{
			return list;
		}
		string[] directories = Directory.GetDirectories(customMapsDir);
		for (int i = 0; i < directories.Length; i++)
		{
			string[] files = Directory.GetFiles(directories[i]);
			foreach (string text in files)
			{
				if (text.Contains(".vtm"))
				{
					list.Add(Path.GetFileName(text));
				}
			}
		}
		return list;
	}

	public static string GetScenarioDirectoryPath(string scenarioID, string campaignID)
	{
		if (!string.IsNullOrEmpty(campaignID))
		{
			return Path.Combine(Path.Combine(customCampaignsDir, campaignID), scenarioID.Trim());
		}
		return Path.Combine(customScenariosDir, scenarioID.Trim());
	}

	public static AudioMixer GetEnvironmentAudioMixer()
	{
		return audioMixerReference.mixer;
	}

	public static AudioMixerGroup GetExteriorMixerGroup()
	{
		return audioMixerReference.exteriorGroup;
	}

	public static AudioMixerGroup GetInteriorMixerGroup()
	{
		return audioMixerReference.interiorGroup;
	}

	public static void LoadAllResources(bool skipUnmodifiedScenarios = true)
	{
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		LoadPlayerVehicles();
		long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
		LoadMaps();
		long num = stopwatch.ElapsedMilliseconds - elapsedMilliseconds;
		if (Application.isPlaying)
		{
			LoadWingmanVoiceProfilesAsync();
			LoadGroundCrewVoiceProfilesAsync();
			LoadAwacsVoiceProfilesAsync();
		}
		else
		{
			LoadWingmanVoiceProfiles();
			LoadGroundCrewVoiceProfiles();
			LoadAwacsVoiceProfiles();
		}
		long num2 = stopwatch.ElapsedMilliseconds - (num + elapsedMilliseconds);
		LoadCustomScenarios(skipUnmodifiedScenarios);
		long num3 = stopwatch.ElapsedMilliseconds - (num2 + num + elapsedMilliseconds);
		stopwatch.Stop();
		UnityEngine.Debug.Log("Loaded all VTResources in " + stopwatch.ElapsedMilliseconds + " ms.\nPlayerVehicles: " + elapsedMilliseconds + "\nMaps: " + num + "\nVoices: " + num2 + "\nScenarios: " + num3);
	}

	public static void LoadStaticObjectPrefabs()
	{
		if (vtEditStaticObjectPrefabs == null)
		{
			vtEditStaticObjectPrefabs = new Dictionary<string, GameObject>();
			GameObject[] array = Resources.LoadAll<GameObject>("VTEdit/StaticObjects");
			foreach (GameObject gameObject in array)
			{
				vtEditStaticObjectPrefabs.Add(gameObject.name, gameObject);
			}
		}
	}

	public static List<VTStaticObject> GetAllStaticObjectPrefabs()
	{
		LoadStaticObjectPrefabs();
		List<VTStaticObject> list = new List<VTStaticObject>();
		foreach (GameObject value in vtEditStaticObjectPrefabs.Values)
		{
			list.Add(value.GetComponent<VTStaticObject>());
		}
		return list;
	}

	public static GameObject GetStaticObjectPrefab(string id)
	{
		LoadStaticObjectPrefabs();
		if (vtEditStaticObjectPrefabs.ContainsKey(id))
		{
			return vtEditStaticObjectPrefabs[id];
		}
		return null;
	}

	public static void LoadVoiceProfiles()
	{
		if (finishedLoadingVoices)
		{
			bool flag = true;
			if (GameSettings.enabledWingmanVoices != null)
			{
				foreach (WingmanVoiceProfile value in wingmanVoices.Values)
				{
					value.enabled = GameSettings.enabledWingmanVoices.Contains(value.name);
					if (value.enabled)
					{
						flag = false;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			{
				foreach (WingmanVoiceProfile value2 in wingmanVoices.Values)
				{
					value2.enabled = true;
				}
				return;
			}
		}
		LoadWingmanVoiceProfiles();
	}

	public static void ForceReloadWingmanVoices()
	{
		if (wingLoader != null)
		{
			UnityEngine.Object.Destroy(wingLoader.gameObject);
		}
		finishedLoadingVoices = false;
		LoadWingmanVoiceProfiles();
	}

	private static void LoadAwacsVoiceProfiles()
	{
		if (!loadedAwacsVoices)
		{
			if (awacsLoader != null)
			{
				UnityEngine.Debug.Log("AWACS loader was loading async.  Cancelling and loading sync.");
				UnityEngine.Object.Destroy(awacsLoader.gameObject);
			}
			UnityEngine.Debug.Log("Loading AWACS voices synchronously!");
			awacsVoices = new Dictionary<string, AWACSVoiceProfile>();
			sortedAwacsVoices = new List<AWACSVoiceProfile>();
			AWACSVoiceProfile[] array = Resources.LoadAll<AWACSVoiceProfile>("AWACSVoices");
			foreach (AWACSVoiceProfile aWACSVoiceProfile in array)
			{
				awacsVoices.Add(aWACSVoiceProfile.name, aWACSVoiceProfile);
				sortedAwacsVoices.Add(aWACSVoiceProfile);
			}
			sortedAwacsVoices.Sort((AWACSVoiceProfile a, AWACSVoiceProfile b) => a.name.CompareTo(b.name));
			loadedAwacsVoices = true;
		}
	}

	private static void LoadAwacsVoiceProfilesAsync()
	{
		if (loadedAwacsVoices)
		{
			return;
		}
		if (awacsLoader != null)
		{
			UnityEngine.Object.Destroy(awacsLoader.gameObject);
		}
		awacsVoices = new Dictionary<string, AWACSVoiceProfile>();
		sortedAwacsVoices = new List<AWACSVoiceProfile>();
		awacsLoader = new GameObject("awacs voices loader").AddComponent<AWACSVoiceLoader>();
		awacsLoader.OnFinishedLoading += delegate
		{
			loadedAwacsVoices = true;
			sortedAwacsVoices.Sort((AWACSVoiceProfile a, AWACSVoiceProfile b) => a.name.CompareTo(b.name));
		};
		awacsLoader.OnLoadedAsset += delegate(AWACSVoiceProfile p)
		{
			awacsVoices.Add(p.name, p);
			sortedAwacsVoices.Add(p);
		};
		awacsLoader.Load("AWACSVoices", "AWACSVoiceManifest");
	}

	public static List<AWACSVoiceProfile> GetAllAWACSVoices()
	{
		LoadAwacsVoiceProfiles();
		List<AWACSVoiceProfile> list = new List<AWACSVoiceProfile>();
		foreach (AWACSVoiceProfile value in awacsVoices.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static AWACSVoiceProfile GetAWACSVoice(string id)
	{
		LoadAwacsVoiceProfiles();
		if (awacsVoices.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public static AWACSVoiceProfile GetAWACSVoice(int index)
	{
		return sortedAwacsVoices[index];
	}

	public static int GetAWACSVoiceIndex(AWACSVoiceProfile p)
	{
		return sortedAwacsVoices.IndexOf(p);
	}

	private static void LoadGroundCrewVoiceProfiles()
	{
		if (!loadedGroundCrewVoices)
		{
			if ((bool)gcVoiceLoader)
			{
				UnityEngine.Debug.Log("Groundcrew Voices were loading async.  Cancelling async load and loading all synchronously!");
				UnityEngine.Object.Destroy(gcVoiceLoader.gameObject);
			}
			UnityEngine.Debug.Log("Loading ground crew voices!");
			groundCrewVoices = new Dictionary<string, GroundCrewVoiceProfile>();
			GroundCrewVoiceProfile[] array = Resources.LoadAll<GroundCrewVoiceProfile>("GroundCrewVoices");
			foreach (GroundCrewVoiceProfile groundCrewVoiceProfile in array)
			{
				groundCrewVoices.Add(groundCrewVoiceProfile.name, groundCrewVoiceProfile);
			}
			loadedGroundCrewVoices = true;
		}
	}

	private static void LoadGroundCrewVoiceProfilesAsync()
	{
		if (!loadedGroundCrewVoices)
		{
			UnityEngine.Debug.Log("Loading ground crew voices ASYNC!");
			if ((bool)gcVoiceLoader)
			{
				UnityEngine.Debug.Log("Groundcrew Voices were loading async.  Cancelling async load and restarting process.");
				UnityEngine.Object.Destroy(gcVoiceLoader.gameObject);
			}
			groundCrewVoices = new Dictionary<string, GroundCrewVoiceProfile>();
			gcVoiceLoader = new GameObject("groundcrew voices loader").AddComponent<GroundCrewVoiceLoader>();
			gcVoiceLoader.OnFinishedLoading += delegate
			{
				loadedGroundCrewVoices = true;
			};
			gcVoiceLoader.OnLoadedAsset += delegate(GroundCrewVoiceProfile p)
			{
				groundCrewVoices.Add(p.name, p);
			};
			gcVoiceLoader.Load("GroundCrewVoices", "GCVoiceManifest");
		}
	}

	public static GroundCrewVoiceProfile GetGroundCrewVoice(string id)
	{
		LoadGroundCrewVoiceProfiles();
		GroundCrewVoiceProfile value = null;
		if (groundCrewVoices.TryGetValue(id, out value))
		{
			return value;
		}
		return GetDefaultGroundCrewVoice();
	}

	public static GroundCrewVoiceProfile GetDefaultGroundCrewVoice()
	{
		LoadGroundCrewVoiceProfiles();
		GroundCrewVoiceProfile value = null;
		if (groundCrewVoices.TryGetValue("DefaultGroundCrewVoice", out value))
		{
			return value;
		}
		return null;
	}

	public static WingmanVoiceProfile GetDefaultWingmanVoice()
	{
		return Resources.Load<WingmanVoiceProfile>("VTEdit/WingmanVoiceProfiles/DefaultWingmanVoice");
	}

	public static List<GroundCrewVoiceProfile> GetAllGroundCrewVoices()
	{
		LoadGroundCrewVoiceProfiles();
		List<GroundCrewVoiceProfile> list = new List<GroundCrewVoiceProfile>();
		foreach (GroundCrewVoiceProfile value in groundCrewVoices.Values)
		{
			list.Add(value);
		}
		return list;
	}

	private static void LoadWingmanVoiceProfiles()
	{
		if (finishedLoadingVoices)
		{
			return;
		}
		UnityEngine.Debug.Log("Loading wingman voice profiles synchronously!");
		wingmanVoices = new Dictionary<string, WingmanVoiceProfile>();
		WingmanVoiceProfile[] array = Resources.LoadAll<WingmanVoiceProfile>("VTEdit/WingmanVoiceProfiles");
		foreach (WingmanVoiceProfile wingmanVoiceProfile in array)
		{
			if (GameSettings.enabledWingmanVoices != null)
			{
				wingmanVoiceProfile.enabled = GameSettings.enabledWingmanVoices.Contains(wingmanVoiceProfile.name);
				if (!wingmanVoiceProfile.enabled && !string.IsNullOrEmpty(wingmanVoiceProfile.entryVersion) && ConfigNodeUtils.ParseObject<GameVersion>(wingmanVoiceProfile.entryVersion) > currVoiceEntryVersion)
				{
					wingmanVoiceProfile.enabled = true;
					GameSettings.enabledWingmanVoices.Add(wingmanVoiceProfile.name);
				}
			}
			wingmanVoices.Add(wingmanVoiceProfile.name, wingmanVoiceProfile);
		}
		currVoiceEntryVersion = GameStartup.version;
		finishedLoadingVoices = true;
	}

	private static void LoadWingmanVoiceProfilesAsync()
	{
		if (finishedLoadingVoices)
		{
			return;
		}
		if ((bool)wingLoader)
		{
			UnityEngine.Object.Destroy(wingLoader.gameObject);
		}
		wingmanVoices = new Dictionary<string, WingmanVoiceProfile>();
		wingLoader = new GameObject("wingmanVoiceLoader").AddComponent<WingmanVoiceLoader2>();
		if (!wingLoader)
		{
			UnityEngine.Debug.Log("wingLoader is null");
		}
		wingLoader.OnLoadedAsset += delegate(WingmanVoiceProfile vp)
		{
			if (GameSettings.enabledWingmanVoices != null)
			{
				vp.enabled = GameSettings.enabledWingmanVoices.Contains(vp.name);
				if (!vp.enabled && !string.IsNullOrEmpty(vp.entryVersion) && ConfigNodeUtils.ParseObject<GameVersion>(vp.entryVersion) > currVoiceEntryVersion)
				{
					vp.enabled = true;
					GameSettings.enabledWingmanVoices.Add(vp.name);
				}
			}
			try
			{
				wingmanVoices.Add(vp.name, vp);
			}
			catch (ArgumentException)
			{
				UnityEngine.Debug.LogError("LoadWingmanVoiceProfilesAsync tried to register a voice that was already loaded.");
			}
		};
		wingLoader.OnFinishedLoading += delegate
		{
			finishedLoadingVoices = true;
			currVoiceEntryVersion = GameStartup.version;
		};
		wingLoader.Load("VTEdit/WingmanVoiceProfiles", "VoiceManifest");
	}

	public static List<string> GetAllWingmanVoiceNames()
	{
		return Resources.Load<ScriptableStringList>("VTEdit/WingmanVoiceProfiles/VoiceManifest").list.Copy();
	}

	public static List<WingmanVoiceProfile> GetWingmanVoiceProfiles()
	{
		if (wingLoader != null)
		{
			UnityEngine.Object.Destroy(wingLoader.gameObject);
			LoadWingmanVoiceProfiles();
		}
		else
		{
			LoadVoiceProfiles();
		}
		List<WingmanVoiceProfile> list = new List<WingmanVoiceProfile>();
		foreach (WingmanVoiceProfile value in wingmanVoices.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static WingmanVoiceProfile GetWingmanVoiceProfile(string voiceProfileID)
	{
		if (string.IsNullOrEmpty(voiceProfileID))
		{
			return null;
		}
		if (wingmanVoices.ContainsKey(voiceProfileID))
		{
			return wingmanVoices[voiceProfileID];
		}
		UnityEngine.Debug.LogError("Missing wingman voice profile '" + voiceProfileID + "'");
		return null;
	}

	public static void LoadPlayerVehicles()
	{
		if (!(playerVehicles == null))
		{
			return;
		}
		UnityEngine.Debug.Log("Loading player vehicles...");
		playerVehicles = Resources.Load<PlayerVehicleList>("PlayerVehicles");
		if (VTLocalizationManager.writeLocalizationDict)
		{
			foreach (PlayerVehicle playerVehicle in playerVehicles.playerVehicles)
			{
				UnityEngine.Debug.Log(" - " + playerVehicle.vehicleName);
				playerVehicle.GetLocalizedDescription();
				foreach (GameObject allEquipPrefab in playerVehicle.allEquipPrefabs)
				{
					allEquipPrefab.GetComponent<HPEquippable>().GetLocalizedDescription();
				}
			}
		}
		foreach (PlayerVehicle playerVehicle2 in playerVehicles.playerVehicles)
		{
			if (playerVehicle2.dlc)
			{
				TryLoadDLCVehicle(playerVehicle2);
			}
		}
	}

	public static bool HasDLCInstalled(uint dlcID)
	{
		return installedDLCs.Contains(dlcID);
	}

	private static void TryLoadDLCVehicle(PlayerVehicle pv)
	{
		if (Application.isPlaying)
		{
			if (!SteamClient.IsValid)
			{
				UnityEngine.Debug.LogError("Steam client not valid!  Can't load DLC");
			}
			if (!SteamApps.IsDlcInstalled(pv.dlcID))
			{
				UnityEngine.Debug.LogError("DLC not installed! Can't load DLC: " + pv.vehicleName);
			}
		}
		string path = Path.Combine(gameRootDirectory, "DLC");
		path = Path.Combine(path, pv.dlcID.ToString());
		path = Path.Combine(path, pv.dlcID.ToString());
		if (File.Exists(path))
		{
			AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
			if (assetBundle == null)
			{
				UnityEngine.Debug.LogError("Failed to load DLC asset for " + pv.vehicleName + "!");
				return;
			}
			GameObject gameObject = (pv.vehiclePrefab = assetBundle.LoadAsset<GameObject>(pv.dlcPrefabName));
			DLCInfo component = gameObject.GetComponent<DLCInfo>();
			string text = "????";
			if ((bool)component)
			{
				text = component.versionString;
			}
			else
			{
				UnityEngine.Debug.LogError("DLC " + pv.vehicleName + " does not have a DLCInfo!");
			}
			VTNetworkManager.RegisterOverrideResource(pv.resourcePath, gameObject);
			UnityEngine.Debug.Log("Successfully loaded asset for DLC " + pv.vehicleName + "! (v" + text + ")");
			pv.loadedDLCVersion = GameVersion.Parse(text);
			pv.dlcLoaded = true;
			if (!installedDLCs.Contains(pv.dlcID))
			{
				installedDLCs.Add(pv.dlcID);
			}
		}
		else
		{
			UnityEngine.Debug.LogError("File does not exist for DLC asset for " + pv.vehicleName + " (" + path + ")!");
		}
	}

	public static void ReLocalizeScenarioObjectives()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		if (builtInCampaigns != null)
		{
			foreach (VTCampaignInfo value in builtInCampaigns.Values)
			{
				foreach (VTScenarioInfo allScenario in value.allScenarios)
				{
					allScenario.ApplyLocalizedObjectivesIfNecessary();
				}
			}
		}
		if (customCampaigns != null)
		{
			foreach (VTCampaignInfo value2 in customCampaigns.Values)
			{
				foreach (VTScenarioInfo allScenario2 in value2.allScenarios)
				{
					allScenario2.ApplyLocalizedObjectivesIfNecessary();
				}
			}
		}
		if (steamWorkshopCampaigns == null)
		{
			return;
		}
		foreach (VTCampaignInfo value3 in steamWorkshopCampaigns.Values)
		{
			foreach (VTScenarioInfo allScenario3 in value3.allScenarios)
			{
				allScenario3.ApplyLocalizedObjectivesIfNecessary();
			}
		}
	}

	public static PlayerVehicle[] GetPlayerVehicles(bool onlyReadyToFly = true)
	{
		if (onlyReadyToFly)
		{
			List<PlayerVehicle> list = new List<PlayerVehicle>();
			foreach (PlayerVehicle playerVehicle in playerVehicles.playerVehicles)
			{
				if (isEditorOrDevTools || IsVehicleAvailableToPlay(playerVehicle))
				{
					list.Add(playerVehicle);
				}
			}
			return list.ToArray();
		}
		return playerVehicles.playerVehicles.ToArray();
	}

	public static bool IsVehicleAvailableToPlay(PlayerVehicle pv)
	{
		if (pv.readyToFly)
		{
			if (pv.dlc)
			{
				return SteamApps.IsDlcInstalled(pv.dlcID);
			}
			return true;
		}
		return false;
	}

	public static VTMap GetMap(string mapID)
	{
		if (maps == null || maps.Count == 0)
		{
			LoadMaps();
		}
		if (maps.ContainsKey(mapID))
		{
			return maps[mapID];
		}
		UnityEngine.Debug.LogErrorFormat("Unable to locate map: {0}", mapID);
		return null;
	}

	public static List<VTMap> GetMaps()
	{
		if (maps == null || maps.Count == 0)
		{
			LoadMaps();
		}
		List<VTMap> list = new List<VTMap>();
		foreach (VTMap value in maps.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static PlayerVehicle GetPlayerVehicle(string vehicleName)
	{
		if (playerVehicles == null)
		{
			LoadPlayerVehicles();
		}
		foreach (PlayerVehicle playerVehicle in playerVehicles.playerVehicles)
		{
			if (playerVehicle.vehicleName == vehicleName)
			{
				return playerVehicle;
			}
		}
		return null;
	}

	public static int LoadCustomCampaignAtPath(string filePath, bool skipUnmodified)
	{
		try
		{
			if (!filePath.EndsWith(".vtc") && !filePath.EndsWith(".vtcb"))
			{
				return -1;
			}
			string directoryName = Path.GetDirectoryName(filePath);
			string fileName = Path.GetFileName(directoryName);
			if (skipUnmodified)
			{
				DateTime dateTime = SafelyGetLastWriteTime(directoryName);
				if (campaignModifiedTimes.TryGetValue(fileName, out var value))
				{
					if (dateTime == value)
					{
						return 0;
					}
					campaignModifiedTimes[fileName] = dateTime;
				}
				else
				{
					campaignModifiedTimes.Add(fileName, dateTime);
				}
			}
			UnityEngine.Debug.Log("- Loading campaign: " + fileName);
			VTCampaignInfo vTCampaignInfo = new VTCampaignInfo(ConfigNode.LoadFromFile(filePath), filePath);
			PlayerVehicle playerVehicle = GetPlayerVehicle(vTCampaignInfo.vehicle);
			if (playerVehicle != null && (isEditorOrDevTools || playerVehicle.readyToFly))
			{
				try
				{
					customCampaigns.Remove(vTCampaignInfo.campaignID);
					customCampaigns.Add(vTCampaignInfo.campaignID, vTCampaignInfo);
					return 1;
				}
				catch (ArgumentException)
				{
					UnityEngine.Debug.LogError("ERROR Duplicate custom campaign ID: " + vTCampaignInfo.campaignID);
					return -1;
				}
			}
			UnityEngine.Debug.LogError("ERROR campaign is for an invalid vehicle");
			return -1;
		}
		catch (KeyNotFoundException ex2)
		{
			UnityEngine.Debug.LogError("KeyNotFoundException thrown when attempting to load VTCampaign from " + filePath + ". It may be outdated or invalid.\n\n" + ex2);
			return -1;
		}
	}

	public static void LoadCustomScenarios(bool skipUnmodified = true)
	{
		if (customScenarios == null)
		{
			customScenarios = new Dictionary<string, VTScenarioInfo>();
		}
		if (customCampaigns == null)
		{
			customCampaigns = new Dictionary<string, VTCampaignInfo>();
		}
		if (builtInCampaigns == null)
		{
			builtInCampaigns = new Dictionary<string, VTCampaignInfo>();
		}
		if (builtInTutorials == null)
		{
			builtInTutorials = new Dictionary<string, VTCampaignInfo>();
		}
		if (builtInMultiplayerCampaigns == null)
		{
			builtInMultiplayerCampaigns = new Dictionary<string, VTCampaignInfo>();
		}
		UnityEngine.Debug.Log("Loading all missions!");
		if (builtInCampaignsObject == null || !Application.isPlaying)
		{
			UnityEngine.Debug.Log("Loading built-in campaigns from resources.");
			builtInCampaignsObject = Resources.Load<BuiltInCampaigns>("Campaigns/Campaigns");
			foreach (SerializedCampaign campaign in builtInCampaignsObject.campaigns)
			{
				if (string.IsNullOrEmpty(campaign.campaignConfig))
				{
					continue;
				}
				VTCampaignInfo vTCampaignInfo = new VTCampaignInfo(campaign);
				PlayerVehicle playerVehicle = GetPlayerVehicle(vTCampaignInfo.vehicle);
				if (!(playerVehicle != null) || (!isEditorOrDevTools && !playerVehicle.readyToFly) || (playerVehicle.dlc && !playerVehicle.IsDLCOwned()))
				{
					continue;
				}
				try
				{
					if (!skipUnmodified)
					{
						builtInCampaigns.Remove(vTCampaignInfo.campaignID);
					}
					builtInCampaigns.Add(vTCampaignInfo.campaignID, vTCampaignInfo);
				}
				catch (ArgumentException)
				{
					UnityEngine.Debug.LogError("ERROR Duplicate built-in campaign ID: " + vTCampaignInfo.campaignID);
				}
			}
			foreach (SerializedCampaign tutorial in builtInCampaignsObject.tutorials)
			{
				if (string.IsNullOrEmpty(tutorial.campaignConfig))
				{
					continue;
				}
				VTCampaignInfo vTCampaignInfo2 = new VTCampaignInfo(tutorial);
				PlayerVehicle playerVehicle2 = GetPlayerVehicle(vTCampaignInfo2.vehicle);
				if (!(playerVehicle2 != null) || (!isEditorOrDevTools && !playerVehicle2.readyToFly) || (playerVehicle2.dlc && !playerVehicle2.IsDLCOwned()))
				{
					continue;
				}
				try
				{
					if (!skipUnmodified)
					{
						builtInTutorials.Remove(vTCampaignInfo2.campaignID);
					}
					builtInTutorials.Add(vTCampaignInfo2.campaignID, vTCampaignInfo2);
				}
				catch (ArgumentException)
				{
					UnityEngine.Debug.LogError("ERROR Duplicate built-in tutorial campaign ID: " + vTCampaignInfo2.campaignID);
				}
			}
			foreach (SerializedCampaign multiplayerCampaign in builtInCampaignsObject.multiplayerCampaigns)
			{
				if (string.IsNullOrEmpty(multiplayerCampaign.campaignConfig))
				{
					continue;
				}
				VTCampaignInfo vTCampaignInfo3 = new VTCampaignInfo(multiplayerCampaign);
				GetPlayerVehicle(vTCampaignInfo3.vehicle);
				try
				{
					if (!skipUnmodified)
					{
						builtInMultiplayerCampaigns.Remove(vTCampaignInfo3.campaignID);
					}
					builtInMultiplayerCampaigns.Add(vTCampaignInfo3.campaignID, vTCampaignInfo3);
				}
				catch (ArgumentException)
				{
					UnityEngine.Debug.LogError("ERROR Duplicate built-in mp campaign ID: " + vTCampaignInfo3.campaignID);
				}
			}
		}
		else
		{
			UnityEngine.Debug.Log("Built-in campaigns already loaded.");
		}
		if (Directory.Exists(customScenariosDir))
		{
			UnityEngine.Debug.Log("Loading custom campaigns:");
			if (Directory.Exists(customCampaignsDir))
			{
				bool flag = false;
				int num = 0;
				while (!flag)
				{
					try
					{
						int num2 = 0;
						string[] directories = Directory.GetDirectories(customCampaignsDir, "*", SearchOption.TopDirectoryOnly);
						foreach (string path in directories)
						{
							string searchPattern = "*.vtc";
							string[] files = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
							for (int j = 0; j < files.Length; j++)
							{
								if (LoadCustomCampaignAtPath(files[j], skipUnmodified) == 0)
								{
									num2++;
								}
							}
						}
						UnityEngine.Debug.Log("- Skipped " + num2 + " campaigns (not modified.)");
						flag = true;
					}
					catch (DirectoryNotFoundException)
					{
						num++;
						Thread.Sleep(20);
						if (num % 5 == 0)
						{
							UnityEngine.Debug.Log("Directory cache not refreshed after a delete.  Attempting to load scenarios # " + num + ".");
						}
					}
				}
			}
			else
			{
				UnityEngine.Debug.Log("No custom campaigns directory!");
			}
			UnityEngine.Debug.Log("Loading standalone scenarios:");
			{
				foreach (VTScenarioInfo item in LoadScenariosFromDir(customScenariosDir, skipUnmodified))
				{
					customScenarios.Remove(item.id);
					customScenarios.Add(item.id, item);
				}
				return;
			}
		}
		UnityEngine.Debug.Log("No custom scenarios directory!");
	}

	public static void DeleteCustomScenario(string scenarioID, string campaignID)
	{
		if (!string.IsNullOrEmpty(campaignID))
		{
			if (!customCampaigns.TryGetValue(campaignID, out var value))
			{
				return;
			}
			UnityEngine.Debug.LogFormat("Deleting a scenario {0} from campaign {1}", scenarioID, campaignID);
			value.allScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
			value.missionScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
			value.trainingScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
			string path = Path.Combine(customCampaignsDir, campaignID);
			path = Path.Combine(path, scenarioID.Trim());
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
			UnityEngine.Debug.Log(" - checking if any maps are no longer being used...");
			string[] files = Directory.GetFiles(value.directoryPath, "*.vtm", SearchOption.AllDirectories);
			foreach (string text in files)
			{
				string text2 = Path.GetFileName(text).Replace(".vtm", string.Empty);
				bool flag = false;
				string[] files2 = Directory.GetFiles(value.directoryPath, "*.vts", SearchOption.AllDirectories);
				for (int j = 0; j < files2.Length; j++)
				{
					if (ConfigNode.LoadFromFile(files2[j]).GetValue("mapID") == text2)
					{
						flag = true;
						UnityEngine.Debug.LogFormat(" - - {0} is still being used. Keeping it.", text2);
						break;
					}
				}
				if (!flag)
				{
					string text3 = text.Substring(0, text.Length - (1 + text2.Length + 4));
					UnityEngine.Debug.LogFormat(" - - {0} is no longer being used!  Deleting it from campaign directory. ({1})", text2, text3);
					Directory.Delete(text3, recursive: true);
				}
			}
		}
		else
		{
			string path2 = Path.Combine(customScenariosDir, scenarioID.Trim());
			if (Directory.Exists(path2))
			{
				Directory.Delete(path2, recursive: true);
			}
			customScenarios.Remove(scenarioID);
		}
	}

	public static void DeleteCustomCampaign(string campaignID)
	{
		if (customCampaigns.ContainsKey(campaignID))
		{
			string text = Path.Combine(customCampaignsDir, campaignID);
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
				customCampaigns.Remove(campaignID);
				return;
			}
			UnityEngine.Debug.LogError("Tried to delete campaign '" + campaignID + "' but the directory does not exist! (" + text + ")");
		}
		else
		{
			UnityEngine.Debug.LogError("Tried to delete campaign '" + campaignID + "' but the campaign does not exist!");
		}
	}

	public static void ReloadCustomScenario(string scenarioID, string campaignID)
	{
		if (!Directory.Exists(customScenariosDir))
		{
			return;
		}
		if (!string.IsNullOrEmpty(campaignID))
		{
			if (!Directory.Exists(customCampaignsDir))
			{
				return;
			}
			if (customCampaigns.TryGetValue(campaignID, out var value))
			{
				string text = Path.Combine(customCampaignsDir, campaignID);
				if (Directory.Exists(text))
				{
					value.allScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
					value.trainingScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
					value.missionScenarios.RemoveAll((VTScenarioInfo x) => x.id == scenarioID);
					string path = Path.Combine(text, scenarioID.Trim());
					path = Path.Combine(path, scenarioID + ".vts");
					VTScenarioInfo vTScenarioInfo = new VTScenarioInfo(ConfigNode.LoadFromFile(path), path);
					value.allScenarios.Add(vTScenarioInfo);
					if (vTScenarioInfo.isTraining)
					{
						value.trainingScenarios.Add(vTScenarioInfo);
					}
					else
					{
						value.missionScenarios.Add(vTScenarioInfo);
					}
				}
			}
			else
			{
				UnityEngine.Debug.LogError("ReloadCustomScenario: No pre-existing custom campaign info for " + campaignID + "!");
			}
		}
		else
		{
			customScenarios.Remove(scenarioID);
			string path2 = Path.Combine(customScenariosDir, scenarioID.Trim());
			path2 = Path.Combine(path2, scenarioID + ".vts");
			VTScenarioInfo value2 = new VTScenarioInfo(ConfigNode.LoadFromFile(path2), path2);
			customScenarios.Add(scenarioID, value2);
		}
	}

	public static List<VTScenarioInfo> LoadScenariosFromDir(string parentDirectory, bool checkModified = false, bool enforceDirectoryName = true, bool decodeWSScenarios = false)
	{
		bool flag = false;
		List<VTScenarioInfo> list = new List<VTScenarioInfo>();
		int num = 0;
		string[] directories = Directory.GetDirectories(parentDirectory, "*", SearchOption.TopDirectoryOnly);
		foreach (string path in directories)
		{
			string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
			foreach (string text in files)
			{
				try
				{
					string text2 = text;
					if (text.EndsWith(".vts") || (decodeWSScenarios && text.EndsWith(".vtsb")))
					{
						if (checkModified)
						{
							string fileName = Path.GetFileName(path);
							DateTime dateTime = SafelyGetLastWriteTime(path);
							if (customScenarioModifiedTimes.TryGetValue(fileName, out var value))
							{
								if (dateTime == value)
								{
									num++;
									continue;
								}
								customScenarioModifiedTimes[fileName] = dateTime;
							}
							else
							{
								customScenarioModifiedTimes.Add(fileName, dateTime);
							}
						}
						ConfigNode configNode = ((!text2.EndsWith(".vtsb")) ? ConfigNode.LoadFromFile(text2) : VTSteamWorkshopUtils.ReadWorkshopConfig(text2));
						if (parentDirectory == customScenariosDir)
						{
							string fileName2 = Path.GetFileName(path);
							if (enforceDirectoryName && !fileName2.Equals(configNode.GetValue("scenarioID")))
							{
								flag = true;
								break;
							}
						}
						VTScenarioInfo vTScenarioInfo = new VTScenarioInfo(configNode, text2);
						PlayerVehicle vehicle = vTScenarioInfo.vehicle;
						if (vehicle != null && (isEditorOrDevTools || vehicle.readyToFly))
						{
							list.Add(vTScenarioInfo);
						}
					}
				}
				catch (KeyNotFoundException)
				{
					UnityEngine.Debug.LogError("KeyNotFoundException thrown when attempting to load VTScenario from " + text + ". It may be an outdated scenario file.");
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (flag)
		{
			RepairAllScenarioFilePaths();
			return LoadScenariosFromDir(parentDirectory);
		}
		if (checkModified)
		{
			UnityEngine.Debug.Log("- Skipped " + num + " scenarios (not modified).");
		}
		return list;
	}

	public static IEnumerator LoadScenariosFromDirAsync(MonoBehaviour host, List<VTScenarioInfo> scenarios, string parentDirectory, bool checkModified = false, bool enforceDirectoryName = true, bool decodeWSScenarios = false)
	{
		bool needsRepair = false;
		int skipped = 0;
		scenarios.Clear();
		string[] directories = Directory.GetDirectories(parentDirectory, "*", SearchOption.TopDirectoryOnly);
		foreach (string dirPath in directories)
		{
			string[] files = Directory.GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly);
			foreach (string text in files)
			{
				string text2 = text;
				if (text.EndsWith(".vts") || (decodeWSScenarios && text.EndsWith(".vtsb")))
				{
					if (checkModified)
					{
						string fileName = Path.GetFileName(dirPath);
						DateTime dateTime = SafelyGetLastWriteTime(dirPath);
						if (customScenarioModifiedTimes.TryGetValue(fileName, out var value))
						{
							if (dateTime == value)
							{
								skipped++;
								continue;
							}
							customScenarioModifiedTimes[fileName] = dateTime;
						}
						else
						{
							customScenarioModifiedTimes.Add(fileName, dateTime);
						}
					}
					ConfigNode configNode = null;
					try
					{
						configNode = ((!text2.EndsWith(".vtsb")) ? ConfigNode.LoadFromFile(text2) : VTSteamWorkshopUtils.ReadWorkshopConfig(text2));
					}
					catch (KeyNotFoundException)
					{
						UnityEngine.Debug.LogError("KeyNotFoundException thrown when attempting to load VTScenario from " + text + ". It may be an outdated scenario file.");
					}
					if (configNode != null)
					{
						if (parentDirectory == customScenariosDir)
						{
							string fileName2 = Path.GetFileName(dirPath);
							if (enforceDirectoryName && !fileName2.Equals(configNode.GetValue("scenarioID")))
							{
								needsRepair = true;
								break;
							}
						}
						VTScenarioInfo vTScenarioInfo = new VTScenarioInfo(configNode, text2);
						PlayerVehicle vehicle = vTScenarioInfo.vehicle;
						if (vehicle != null && (isEditorOrDevTools || vehicle.readyToFly))
						{
							scenarios.Add(vTScenarioInfo);
						}
					}
					yield return null;
				}
				if (needsRepair)
				{
					break;
				}
			}
			if (needsRepair)
			{
				break;
			}
		}
		if (needsRepair)
		{
			RepairAllScenarioFilePaths();
			yield return host.StartCoroutine(LoadScenariosFromDirAsync(host, scenarios, parentDirectory, checkModified, enforceDirectoryName, decodeWSScenarios));
		}
		else if (checkModified)
		{
			UnityEngine.Debug.Log("- Skipped " + skipped + " scenarios (not modified).");
		}
	}

	private static void RepairAllScenarioFilePaths()
	{
		Dictionary<string, ConfigNode> dictionary = new Dictionary<string, ConfigNode>();
		string[] directories = Directory.GetDirectories(customScenariosDir, "*", SearchOption.TopDirectoryOnly);
		foreach (string path in directories)
		{
			string[] files = Directory.GetFiles(path, "*.vts", SearchOption.TopDirectoryOnly);
			foreach (string text in files)
			{
				ConfigNode configNode = ConfigNode.LoadFromFile(text);
				if (!Path.GetFileName(path).Equals(configNode.GetValue("scenarioID")))
				{
					dictionary.Add(text, configNode);
				}
			}
		}
		foreach (KeyValuePair<string, ConfigNode> item in dictionary)
		{
			RepairScenarioFilePath(item.Value, item.Key);
		}
	}

	private static string RepairScenarioFilePath(ConfigNode scenarioNode, string filePath)
	{
		string value = scenarioNode.GetValue("scenarioID");
		UnityEngine.Debug.Log("VTResources: Incorrect filepath for scenario '" + value + "'.  Repairing...");
		string text = value;
		int num = 0;
		bool flag = false;
		string scenarioDirectoryPath = GetScenarioDirectoryPath(value, null);
		string text2 = scenarioDirectoryPath;
		if (Directory.Exists(scenarioDirectoryPath))
		{
			while (!flag)
			{
				if (Directory.Exists(text2))
				{
					num++;
					text2 = scenarioDirectoryPath + num;
				}
				else
				{
					text = value + num;
					flag = true;
				}
			}
		}
		CopyDirectory(Path.GetDirectoryName(filePath), text2);
		string[] files = Directory.GetFiles(text2, "*.vts", SearchOption.TopDirectoryOnly);
		foreach (string text3 in files)
		{
			ConfigNode configNode = ConfigNode.LoadFromFile(text3);
			configNode.SetValue("scenarioID", text);
			string text4 = Path.Combine(text2, text + ".vts");
			File.Move(text3, text4);
			configNode.SaveToFile(text4);
		}
		Directory.Delete(Path.GetDirectoryName(filePath), recursive: true);
		UnityEngine.Debug.Log("VTResources: - Moved " + value + " to " + text);
		return text2;
	}

	public static void LoadMaps()
	{
		LoadCustomMaps();
		maps = new Dictionary<string, VTMap>();
		LoadNonCustomMaps();
		foreach (VTMap value in nonCustomMaps.Values)
		{
			maps.Add(value.mapID, value);
		}
		foreach (VTMapCustom value2 in customMaps.Values)
		{
			maps.Add(value2.mapID, value2);
		}
	}

	private static void LoadNonCustomMaps()
	{
		if (nonCustomMaps == null)
		{
			nonCustomMaps = new Dictionary<string, VTMap>();
			VTMap[] array = Resources.LoadAll<VTMap>("VTMaps");
			foreach (VTMap vTMap in array)
			{
				nonCustomMaps.Add(vTMap.mapID, vTMap);
			}
		}
	}

	public static void LaunchMap(string mapID, bool skipLoading = false)
	{
		if (maps == null || customMaps == null)
		{
			LoadMaps();
		}
		UnityEngine.Debug.Log("launching map: " + mapID);
		VTMap value = null;
		if (!maps.TryGetValue(mapID, out value))
		{
			if (steamWorkshopMaps == null || !steamWorkshopMaps.TryGetValue(mapID, out var value2))
			{
				UnityEngine.Debug.LogError("Map '" + mapID + "' does not exist!");
				return;
			}
			value = value2;
		}
		string sceneName = value.sceneName;
		if (value is VTMapCustom)
		{
			sceneName = customMapSceneName;
			VTCustomMapManager.customMapToLoad = (VTMapCustom)value;
		}
		if (skipLoading)
		{
			LoadingSceneController.LoadSceneImmediate(sceneName);
		}
		else if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.Scenario)
		{
			LoadingSceneController.LoadVTEditScene(sceneName);
		}
		else
		{
			LoadingSceneController.LoadScene(sceneName);
		}
	}

	public static string GetMapSceneNameForScenario(VTScenarioInfo scenarioInfo)
	{
		LoadNonCustomMaps();
		if (nonCustomMaps.TryGetValue(scenarioInfo.mapID, out var value))
		{
			return value.sceneName;
		}
		return customMapSceneName;
	}

	public static VTMap GetMapForScenario(VTScenarioInfo scenarioInfo, out string sceneName)
	{
		if (maps == null || customMaps == null)
		{
			LoadMaps();
		}
		if (nonCustomMaps == null)
		{
			UnityEngine.Debug.LogError("nonCustomMaps is null");
		}
		if (scenarioInfo == null)
		{
			UnityEngine.Debug.LogError("scenarioInfo is null");
		}
		if (nonCustomMaps.TryGetValue(scenarioInfo.mapID, out var value))
		{
			sceneName = value.sceneName;
			return value;
		}
		sceneName = customMapSceneName;
		VTMapCustom vTMapCustom = null;
		if (scenarioInfo.isBuiltIn)
		{
			sceneName = customMapSceneName;
			VTMapCustom map = GetBuiltInCampaign(scenarioInfo.campaignID).serializedCampaign.GetMap(scenarioInfo.mapID);
			if (map != null)
			{
				vTMapCustom = map;
				UnityEngine.Debug.Log("Loading map from built-in campaign.");
			}
		}
		else if (!string.IsNullOrEmpty(scenarioInfo.campaignID))
		{
			vTMapCustom = LoadCustomMap(Path.Combine(GetCustomCampaign(scenarioInfo.campaignID).directoryPath, scenarioInfo.mapID), allowWorkshopEncoded: true);
			UnityEngine.Debug.Log("Loading map from campaign directory.");
		}
		else
		{
			vTMapCustom = LoadCustomMap(Path.Combine(scenarioInfo.directoryPath, scenarioInfo.mapID), allowWorkshopEncoded: true);
		}
		if (vTMapCustom != null)
		{
			UnityEngine.Debug.Log("Loading map from scenario directory.");
			VTMapCustom result = vTMapCustom;
			sceneName = customMapSceneName;
			return result;
		}
		if (customMaps.ContainsKey(scenarioInfo.mapID))
		{
			UnityEngine.Debug.Log("Loading map from custom maps directory.");
			VTMapCustom result2 = customMaps[scenarioInfo.mapID];
			sceneName = customMapSceneName;
			return result2;
		}
		if (steamWorkshopMaps != null && steamWorkshopMaps.ContainsKey(scenarioInfo.mapID))
		{
			UnityEngine.Debug.Log("Loading map from Steam Workshop.");
			VTMapCustom result3 = steamWorkshopMaps[scenarioInfo.mapID];
			sceneName = customMapSceneName;
			return result3;
		}
		UnityEngine.Debug.LogError("ERROR! Missing map with ID " + scenarioInfo.mapID + " for scenario " + scenarioInfo.id);
		sceneName = string.Empty;
		return null;
	}

	public static void LaunchMapForScenario(VTScenarioInfo scenarioInfo, bool skipLoading)
	{
		bool val = false;
		GameSettings.TryGetGameSettingValue<bool>("USE_OVERCLOUD", out val);
		string sceneName;
		VTMap mapForScenario = GetMapForScenario(scenarioInfo, out sceneName);
		if (mapForScenario is VTMapCustom)
		{
			VTMapCustom vTMapCustom = (VTCustomMapManager.customMapToLoad = (VTMapCustom)mapForScenario);
			if (vTMapCustom.isSWPreviewOnly)
			{
				FullyLoadSteamWorkshopMap(vTMapCustom);
			}
		}
		if (skipLoading)
		{
			if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
			{
				LoadingSceneController.SwitchToVRScene(sceneName);
			}
			else
			{
				LoadingSceneController.LoadSceneImmediate(sceneName);
			}
		}
		else if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.Scenario)
		{
			LoadingSceneController.LoadVTEditScene(sceneName);
		}
		else
		{
			LoadingSceneController.SwitchToVRScene(sceneName, loadingScene: true);
		}
	}

	public static string GetMapSceneName(string mapID)
	{
		if (maps == null)
		{
			LoadMaps();
		}
		if (nonCustomMaps.TryGetValue(mapID, out var value))
		{
			return value.sceneName;
		}
		return customMapSceneName;
	}

	public static void SaveCustomScenario(VTScenario scenario, string fileName, string campaignID)
	{
		if (!Directory.Exists(customScenariosDir))
		{
			Directory.CreateDirectory(customScenariosDir);
		}
		string scenarioID = scenario.scenarioID;
		string text;
		if (string.IsNullOrEmpty(campaignID))
		{
			text = Path.Combine(customScenariosDir, fileName);
		}
		else
		{
			if (scenario.campaignOrderIdx < 0)
			{
				VTCampaignInfo customCampaign = GetCustomCampaign(campaignID);
				int num = (scenario.campaignOrderIdx = ((!scenario.isTraining) ? customCampaign.missionScenarios.Count : customCampaign.trainingScenarios.Count));
			}
			text = Path.Combine(Path.Combine(customCampaignsDir, campaignID), fileName);
		}
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		ConfigNode configNode = new ConfigNode("CustomScenario");
		scenario.SaveToConfigNode(configNode);
		configNode.SaveToFile(Path.Combine(text, fileName) + ".vts");
		ReloadCustomScenario(scenarioID, campaignID);
	}

	public static void LoadResourcesAsync(out AsyncOpStatus status)
	{
		UnityEngine.Debug.LogError("LoadResourcesAsync is not yet implemented.");
		status = new AsyncOpStatus();
		ResourcesAsyncScript resourcesAsyncScript = new GameObject("ResourceLoader").AddComponent<ResourcesAsyncScript>();
		resourcesAsyncScript.StartCoroutine(resourcesAsyncScript.LoadRoutine(status));
	}

	public static List<VTScenarioInfo> GetCustomScenarios()
	{
		List<VTScenarioInfo> list = new List<VTScenarioInfo>();
		foreach (VTScenarioInfo value in customScenarios.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static VTScenarioInfo GetCustomScenario(string id, string campaignID)
	{
		if (!string.IsNullOrEmpty(campaignID) && customCampaigns.ContainsKey(campaignID))
		{
			return GetCustomCampaign(campaignID).GetScenario(id);
		}
		if (customScenarios.ContainsKey(id))
		{
			return customScenarios[id];
		}
		if (steamWorkshopCampaigns.TryGetValue(campaignID, out var value))
		{
			return value.GetScenario(id);
		}
		UnityEngine.Debug.LogFormat("Custom scenario '" + id + "' not found{0}", string.IsNullOrEmpty(campaignID) ? "." : (" in campaign '" + campaignID + "'."));
		return null;
	}

	public static VTScenarioInfo GetScenario(string scenarioID, Campaign campaign)
	{
		if (campaign.isBuiltIn)
		{
			return GetBuiltInScenario(scenarioID, campaign.campaignID);
		}
		if (campaign.isSteamworksStandalone)
		{
			if (campaign.isStandaloneScenarios)
			{
				return GetSteamWorkshopStandaloneScenario(scenarioID);
			}
			VTCampaignInfo steamWorkshopCampaign = GetSteamWorkshopCampaign(campaign.campaignID);
			foreach (VTScenarioInfo missionScenario in steamWorkshopCampaign.missionScenarios)
			{
				if (missionScenario.id == scenarioID)
				{
					return missionScenario;
				}
			}
			foreach (VTScenarioInfo trainingScenario in steamWorkshopCampaign.trainingScenarios)
			{
				if (trainingScenario.id == scenarioID)
				{
					return trainingScenario;
				}
			}
			return null;
		}
		return GetCustomScenario(scenarioID, campaign.campaignID);
	}

	public static VTCampaignInfo GetCustomCampaign(string id)
	{
		if (customCampaigns == null)
		{
			LoadCustomScenarios();
		}
		if (customCampaigns.ContainsKey(id))
		{
			return customCampaigns[id];
		}
		if (steamWorkshopCampaigns.ContainsKey(id))
		{
			return steamWorkshopCampaigns[id];
		}
		return null;
	}

	public static List<VTCampaignInfo> GetCustomCampaigns()
	{
		if (customCampaigns == null)
		{
			LoadCustomScenarios();
		}
		List<VTCampaignInfo> list = new List<VTCampaignInfo>();
		foreach (VTCampaignInfo value in customCampaigns.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static VTCampaignInfo GetBuiltInCampaign(string campaignID)
	{
		if (builtInCampaigns == null || builtInTutorials == null || builtInMultiplayerCampaigns == null)
		{
			LoadCustomScenarios();
		}
		if (builtInCampaigns.TryGetValue(campaignID, out var value))
		{
			return value;
		}
		if (builtInTutorials.TryGetValue(campaignID, out value))
		{
			return value;
		}
		if (builtInMultiplayerCampaigns.TryGetValue(campaignID, out value))
		{
			return value;
		}
		return null;
	}

	public static List<VTCampaignInfo> GetBuiltInCampaigns()
	{
		if (builtInCampaigns == null)
		{
			LoadCustomScenarios();
		}
		List<VTCampaignInfo> list = new List<VTCampaignInfo>();
		foreach (VTCampaignInfo value in builtInCampaigns.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static List<VTCampaignInfo> GetBuiltInTutorials()
	{
		if (builtInTutorials == null)
		{
			LoadCustomScenarios();
		}
		List<VTCampaignInfo> list = new List<VTCampaignInfo>();
		foreach (VTCampaignInfo value in builtInTutorials.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static VTScenarioInfo GetBuiltInScenario(string scenarioID, string campaignID)
	{
		return GetBuiltInCampaign(campaignID).GetScenario(scenarioID);
	}

	public static Texture2D GetTexture(string path, bool mipmaps = true, bool linear = false)
	{
		if (path.StartsWith("%BuiltIn"))
		{
			return GetBuiltInTexture(path);
		}
		string text = Path.GetExtension(path).ToLower();
		if (!supportedImageExtensions.Contains(text))
		{
			bool flag = false;
			string[] array = supportedImageExtensions;
			foreach (string text2 in array)
			{
				if (text == text2 + "b")
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				UnityEngine.Debug.LogError("Unable to load image. Invalid file extension: " + path);
				return null;
			}
		}
		try
		{
			byte[] array2 = File.ReadAllBytes(path);
			if (path.EndsWith(".pngb") || path.EndsWith(".jpgb"))
			{
				VTSteamWorkshopUtils.WSDecode(array2);
			}
			Texture2D texture2D = new Texture2D(1, 1, TextureFormat.RGBA32, mipmaps, linear);
			texture2D.LoadImage(array2);
			return texture2D;
		}
		catch (Exception)
		{
			UnityEngine.Debug.LogError("Failed to load image at path: " + path);
			return null;
		}
	}

	public static AudioClip GetAudioClip(string path)
	{
		if (path.StartsWith("%BuiltIn"))
		{
			return GetBuiltInAudioClip(path);
		}
		WWW wWW = new WWW("file://" + path);
		AudioClip audioClip = wWW.GetAudioClip(threeD: true, stream: false);
		new GameObject("Audio Clip Loader").AddComponent<AudioClipLoadBehaviour>().StartLoadRoutine(audioClip, wWW);
		return audioClip;
	}

	public static AsyncMp3ClipLoader LoadMP3Clip(string path)
	{
		return new AsyncMp3ClipLoader(path);
	}

	public static void LoadMP3Clip(string path, Action<AudioClip> onComplete)
	{
		new GameObject("AsyncMp3ClipLoader").AddComponent<AsyncMp3ClipLoader.AsyncMp3ClipLoaderBehaviour>().Begin(path, onComplete);
	}

	private static AudioClip GetBuiltInAudioClip(string path)
	{
		string[] array = path.Replace('/', '\\').Split('\\');
		string campaignID = array[1];
		string scenarioID = array[2];
		string text = array[3];
		for (int i = 4; i < array.Length; i++)
		{
			text = Path.Combine(text, array[i]);
		}
		return builtInCampaignsObject.GetCampaign(campaignID).GetScenario(scenarioID).GetAudioClip(text);
	}

	private static Texture2D GetBuiltInTexture(string path)
	{
		string[] array = path.Replace('/', '\\').Split('\\');
		string campaignID = array[1];
		string scenarioID = array[2];
		string text = array[3];
		for (int i = 4; i < array.Length; i++)
		{
			text = Path.Combine(text, array[i]);
		}
		return builtInCampaignsObject.GetCampaign(campaignID).GetScenario(scenarioID).GetTexture(text);
	}

	public static AudioClip GetBuiltInScenarioAudio(string campaignID, string scenarioID, string audioPath)
	{
		return builtInCampaignsObject.GetCampaign(campaignID).GetScenario(scenarioID).GetAudioClip(audioPath);
	}

	public static VideoClip GetBuiltInScenarioVideo(string campaignID, string scenarioID, string videoPath)
	{
		return builtInCampaignsObject.GetCampaign(campaignID).GetScenario(scenarioID).GetVideoClip(videoPath);
	}

	public static string CopyVTEditResourceToScenario(string editorResourcePath, string scenarioID, string campaignID)
	{
		UnityEngine.Debug.Log("Moving resource from path: " + editorResourcePath);
		int length = vtEditResourceDir.Length;
		string text = editorResourcePath.Substring(length + 1);
		UnityEngine.Debug.Log("Relative path: " + text);
		string text2 = Path.Combine(GetScenarioDirectoryPath(scenarioID, campaignID), text);
		UnityEngine.Debug.Log("New path: " + text2);
		Directory.CreateDirectory(Path.GetDirectoryName(text2));
		if (File.Exists(text2))
		{
			File.Delete(text2);
		}
		File.Copy(editorResourcePath, text2);
		return text;
	}

	public static void ClearEmptyScenarioResources(string scenarioID, string campaignID)
	{
		VTScenarioInfo customScenario = GetCustomScenario(scenarioID, campaignID);
		if (customScenario != null)
		{
			DeleteEmptyDirs(customScenario.directoryPath);
		}
	}

	private static void LoadCustomMaps()
	{
		foreach (KeyValuePair<string, VTMapCustom> customMap in customMaps)
		{
			prevLoadedMaps.Add(customMap.Key, customMap.Value);
		}
		customMaps.Clear();
		if (Directory.Exists(customMapsDir))
		{
			string[] directories = Directory.GetDirectories(customMapsDir);
			for (int i = 0; i < directories.Length; i++)
			{
				VTMapCustom vTMapCustom = LoadCustomMap(directories[i]);
				if (vTMapCustom != null)
				{
					if (!customMaps.ContainsKey(vTMapCustom.mapID))
					{
						customMaps.Add(vTMapCustom.mapID, vTMapCustom);
					}
					else
					{
						UnityEngine.Debug.LogError("ERROR: Tried loading a custom map with a duplicate ID: " + vTMapCustom.mapID + "\nCheck map folder for duplicates. (Skipping duplicate)");
					}
				}
			}
		}
		foreach (VTMapCustom value in prevLoadedMaps.Values)
		{
			UnityEngine.Object.Destroy(value.heightMap);
			if ((bool)value.previewImage)
			{
				UnityEngine.Object.Destroy(value.previewImage);
			}
		}
		prevLoadedMaps.Clear();
	}

	public static DateTime SafelyGetLastWriteTime(string path)
	{
		DateTime result = DateTime.Today;
		try
		{
			result = File.GetLastWriteTimeUtc(path);
			return result;
		}
		catch (TimeZoneNotFoundException ex)
		{
			UnityEngine.Debug.LogError("Exception when attempting to get file write time:\n" + ex);
			return result;
		}
	}

	private static VTMapCustom LoadCustomMap(string dir, bool allowWorkshopEncoded = false)
	{
		if (Directory.Exists(dir))
		{
			string[] files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
			foreach (string text in files)
			{
				if (!text.EndsWith(".vtm") && (!allowWorkshopEncoded || !text.EndsWith(".vtmb")))
				{
					continue;
				}
				ConfigNode configNode = null;
				configNode = ((!text.EndsWith(".vtmb")) ? ConfigNode.LoadFromFile(text) : VTSteamWorkshopUtils.ReadWorkshopConfig(text));
				if (configNode == null)
				{
					continue;
				}
				string fileName = Path.GetFileName(dir);
				configNode.SetValue("mapID", fileName);
				DateTime dateTime = SafelyGetLastWriteTime(text);
				if (prevLoadedMapModTimes.TryGetValue(text, out var value))
				{
					if (dateTime == value && prevLoadedMaps.TryGetValue(fileName, out var value2))
					{
						prevLoadedMaps.Remove(fileName);
						return value2;
					}
					prevLoadedMapModTimes[text] = dateTime;
				}
				else
				{
					prevLoadedMapModTimes.Add(text, dateTime);
				}
				VTMapCustom vTMapCustom = ScriptableObject.CreateInstance<VTMapCustom>();
				vTMapCustom.LoadFromConfigNode(configNode, dir);
				string text2 = Path.Combine(dir, "height.png");
				if (!File.Exists(text2))
				{
					text2 += "b";
				}
				if (File.Exists(text2))
				{
					vTMapCustom.heightMap = GetTexture(text2, mipmaps: false, linear: true);
				}
				string text3 = Path.Combine(dir, "height0.png");
				bool flag = false;
				if (!File.Exists(text3))
				{
					text3 += "b";
					if (File.Exists(text3))
					{
						flag = true;
					}
				}
				if (flag || File.Exists(text3))
				{
					int num = 0;
					List<Texture2D> list = new List<Texture2D>();
					while (File.Exists(text3))
					{
						list.Add(GetTexture(text3));
						num++;
						text3 = Path.Combine(dir, "height" + num + ".png");
						if (flag)
						{
							text3 += "b";
						}
					}
					vTMapCustom.splitHeightmaps = list.ToArray();
				}
				string path = Path.Combine(dir, "preview.jpg");
				if (!File.Exists(path))
				{
					Texture2D texture2D = RenderMapToPreview(vTMapCustom, 512);
					SaveToJpg(texture2D, path);
					UnityEngine.Object.Destroy(texture2D);
				}
				vTMapCustom.previewImage = GetTexture(path, mipmaps: false);
				vTMapCustom.sceneName = customMapSceneName;
				return vTMapCustom;
			}
		}
		return null;
	}

	public static List<VTMapCustom> GetAllCustomMaps()
	{
		if (customMaps == null)
		{
			LoadCustomMaps();
		}
		List<VTMapCustom> list = new List<VTMapCustom>();
		foreach (VTMapCustom value in customMaps.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static VTMapCustom GetCustomMap(string mapID)
	{
		if (customMaps == null)
		{
			LoadMaps();
		}
		if (customMaps.TryGetValue(mapID, out var value))
		{
			return value;
		}
		if (steamWorkshopMaps != null && steamWorkshopMaps.TryGetValue(mapID, out value))
		{
			return value;
		}
		UnityEngine.Debug.Log("Custom map: " + mapID + " does not exist!");
		return null;
	}

	private static void DeleteEmptyDirs(string startDir)
	{
		string[] directories = Directory.GetDirectories(startDir);
		foreach (string text in directories)
		{
			DeleteEmptyDirs(text);
			if (Directory.GetFileSystemEntries(text).Length == 0)
			{
				Directory.Delete(text, recursive: false);
			}
		}
	}

	public static bool IsValidFilename(string filename, List<string> existingFileNames)
	{
		if (string.IsNullOrEmpty(filename))
		{
			return false;
		}
		if (filename.IndexOfAny(invalidFilenameChars) >= 0)
		{
			return false;
		}
		if (filename != filename.Trim())
		{
			return false;
		}
		foreach (string existingFileName in existingFileNames)
		{
			if (existingFileName == filename)
			{
				return false;
			}
		}
		return true;
	}

	public static void CopyDirectory(string sourceDir, string destDir, string[] excludeExtensions = null)
	{
		DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
		if (!directoryInfo.Exists)
		{
			throw new DirectoryNotFoundException("Source not found: " + sourceDir);
		}
		DirectoryInfo[] directories = directoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
		if (!Directory.Exists(destDir))
		{
			Directory.CreateDirectory(destDir);
		}
		FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly);
		foreach (FileInfo fileInfo in files)
		{
			if (excludeExtensions != null)
			{
				bool flag = false;
				string text = fileInfo.Extension.ToLower();
				for (int j = 0; j < excludeExtensions.Length; j++)
				{
					if (flag)
					{
						break;
					}
					if (text == excludeExtensions[j])
					{
						flag = true;
					}
				}
				if (flag)
				{
					continue;
				}
			}
			string text2 = Path.Combine(destDir, fileInfo.Name);
			if (File.Exists(text2))
			{
				File.Delete(text2);
			}
			fileInfo.CopyTo(text2, overwrite: false);
		}
		DirectoryInfo[] array = directories;
		foreach (DirectoryInfo directoryInfo2 in array)
		{
			string destDir2 = Path.Combine(destDir, directoryInfo2.Name);
			CopyDirectory(directoryInfo2.FullName, destDir2, excludeExtensions);
		}
	}

	public static VTScenarioInfo GetSteamWorkshopStandaloneScenario(string scenarioID)
	{
		if (steamWorkshopScenarios.TryGetValue(scenarioID, out var value))
		{
			return value;
		}
		return null;
	}

	public static void LoadWorkshopSingleScenario(Item item)
	{
		string[] files = Directory.GetFiles(item.Directory, "*.vts*", SearchOption.TopDirectoryOnly);
		foreach (string text in files)
		{
			if (!text.EndsWith(".vts") && !text.EndsWith(".vtsb"))
			{
				continue;
			}
			ConfigNode configNode = VTScenarioInfo.ReadConfigNode(text);
			if (configNode != null)
			{
				string value = item.Id.Value.ToString();
				configNode.SetValue("scenarioID", value);
				VTScenarioInfo vTScenarioInfo = new VTScenarioInfo(configNode, text);
				vTScenarioInfo.isWorkshop = true;
				if (steamWorkshopScenarios.ContainsKey(vTScenarioInfo.id))
				{
					steamWorkshopScenarios[vTScenarioInfo.id] = vTScenarioInfo;
				}
				else
				{
					steamWorkshopScenarios.Add(vTScenarioInfo.id, vTScenarioInfo);
				}
			}
		}
	}

	public static List<VTScenarioInfo> GetSteamWorkshopSingleScenarios()
	{
		List<VTScenarioInfo> list = new List<VTScenarioInfo>();
		foreach (VTScenarioInfo value in steamWorkshopScenarios.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static VTCampaignInfo GetSteamWorkshopCampaign(string campaignID)
	{
		if (steamWorkshopCampaigns.TryGetValue(campaignID, out var value))
		{
			return value;
		}
		return null;
	}

	public static VTCampaignInfo LoadWorkshopCampaign(Item item)
	{
		string[] files = Directory.GetFiles(item.Directory, "*.vtc*", SearchOption.TopDirectoryOnly);
		foreach (string text in files)
		{
			ConfigNode configNode = null;
			if (text.EndsWith(".vtc"))
			{
				configNode = ConfigNode.LoadFromFile(text);
			}
			else if (text.EndsWith(".vtcb"))
			{
				configNode = VTSteamWorkshopUtils.ReadWorkshopConfig(text);
			}
			if (configNode == null)
			{
				continue;
			}
			string value = item.Id.Value.ToString();
			configNode.SetValue("campaignID", value);
			VTCampaignInfo vTCampaignInfo = new VTCampaignInfo(configNode, text);
			vTCampaignInfo.isWorkshop = true;
			vTCampaignInfo.wsItem = item;
			foreach (VTScenarioInfo allScenario in vTCampaignInfo.allScenarios)
			{
				allScenario.isWorkshop = true;
			}
			if (steamWorkshopCampaigns.ContainsKey(vTCampaignInfo.campaignID))
			{
				steamWorkshopCampaigns[vTCampaignInfo.campaignID] = vTCampaignInfo;
			}
			else
			{
				steamWorkshopCampaigns.Add(vTCampaignInfo.campaignID, vTCampaignInfo);
			}
			string text2 = (vTCampaignInfo.workshopAuthor = VTSteamWorkshopUtils.GetAuthorName(item));
			return vTCampaignInfo;
		}
		return null;
	}

	public static void SetWorkshopMapsDirty()
	{
		workshopMapsDirty = true;
	}

	public static VTMapCustom GetSteamWorkshopMap(string mapID)
	{
		if (steamWorkshopMaps.TryGetValue(mapID, out var value))
		{
			return value;
		}
		return null;
	}

	public static WorkshopMapListRequest LoadSteamWorkshopMaps(bool previewsOnly = false)
	{
		WorkshopMapListRequest workshopMapListRequest = new WorkshopMapListRequest();
		if (workshopMapsDirty)
		{
			foreach (VTMapCustom value in steamWorkshopMaps.Values)
			{
				UnityEngine.Object.Destroy(value);
			}
			steamWorkshopMaps.Clear();
			workshopMapsDirty = false;
			new GameObject("MapRequestor").AddComponent<MapListRequestBehaviour>().Request(workshopMapListRequest, previewsOnly);
		}
		else
		{
			workshopMapListRequest.maps = new List<VTMapCustom>();
			foreach (KeyValuePair<string, VTMapCustom> steamWorkshopMap in steamWorkshopMaps)
			{
				workshopMapListRequest.maps.Add(steamWorkshopMap.Value);
			}
			workshopMapListRequest.isDone = true;
		}
		return workshopMapListRequest;
	}

	public static void FullyLoadSteamWorkshopMap(VTMapCustom cMap)
	{
		string mapDir = cMap.mapDir;
		string text = Path.Combine(mapDir, "height.png");
		if (!File.Exists(text))
		{
			text += "b";
		}
		if (File.Exists(text))
		{
			cMap.heightMap = GetTexture(text, mipmaps: false, linear: true);
		}
		string text2 = Path.Combine(mapDir, "height0.png");
		bool flag = false;
		if (!File.Exists(text2))
		{
			text2 += "b";
			if (File.Exists(text2))
			{
				flag = true;
			}
		}
		if (flag || File.Exists(text2))
		{
			int num = 0;
			UnityEngine.Debug.Log(" - - loading heightmap splits");
			List<Texture2D> list = new List<Texture2D>();
			while (File.Exists(text2))
			{
				list.Add(GetTexture(text2));
				UnityEngine.Debug.Log(" - - - loaded " + text2);
				num++;
				text2 = Path.Combine(mapDir, string.Format("height{0}.png{1}", num, flag ? "b" : ""));
			}
			cMap.splitHeightmaps = list.ToArray();
		}
		cMap.isSWPreviewOnly = false;
	}

	public static void UploadCampaignToSteamWorkshop(string campaignID, RequestChangeNoteDelegate onRequestChangeNote, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		if (!SteamClient.IsValid)
		{
			return;
		}
		string directoryPath = GetCustomCampaign(campaignID).directoryPath;
		WorkshopItemUpdate u = VTSteamWorkshopUtils.GetItemUpdateFromFolder(directoryPath);
		if (u != null)
		{
			VTSteamWorkshopUtils.CheckCanUpdateItem(u.PublishedFileId, delegate(bool able, string msg)
			{
				if (able)
				{
					if (onRequestChangeNote != null)
					{
						onRequestChangeNote(delegate(string note)
						{
							_FinallyUploadCampaign(campaignID, note, u, onBeginUpdate, onComplete);
						}, delegate
						{
							onComplete(new WorkshopItemUpdateEventArgs
							{
								IsError = true,
								ErrorMessage = "Upload cancelled"
							});
						});
					}
					else
					{
						_FinallyUploadCampaign(campaignID, string.Empty, u, onBeginUpdate, onComplete);
					}
				}
				else
				{
					u = new WorkshopItemUpdate();
					_FinallyUploadCampaign(campaignID, string.Empty, u, onBeginUpdate, onComplete);
				}
			});
		}
		else
		{
			u = new WorkshopItemUpdate();
			_FinallyUploadCampaign(campaignID, string.Empty, u, onBeginUpdate, onComplete);
		}
	}

	private static void _FinallyUploadCampaign(string campaignID, string changeNote, WorkshopItemUpdate u, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		VTCampaignInfo customCampaign = GetCustomCampaign(campaignID);
		string directoryPath = customCampaign.directoryPath;
		if (customCampaign.multiplayer)
		{
			ConfigNode config = customCampaign.config;
			int num = 1;
			if (config.HasValue("wsUploadVersion"))
			{
				num = config.GetValue<int>("wsUploadVersion");
				num++;
			}
			UnityEngine.Debug.Log($"Updating multiplayer workshop campaign version to {num}");
			config.SetValue("wsUploadVersion", num);
			config.SaveToFile(customCampaign.filePath);
		}
		if (string.IsNullOrEmpty(customCampaign.campaignName))
		{
			u.Name = customCampaign.campaignID;
		}
		else
		{
			u.Name = customCampaign.campaignName;
		}
		if (string.IsNullOrEmpty(customCampaign.description))
		{
			u.Description = "No description.\n\nby " + SteamClient.Name;
		}
		else
		{
			u.Description = customCampaign.description + "\n\nby " + SteamClient.Name;
		}
		if (!string.IsNullOrEmpty(changeNote))
		{
			u.ChangeNote = changeNote;
		}
		VTSteamWorkshopUtils.VTWorkshopUploadTempFile temp = VTSteamWorkshopUtils.GetSWUploadFile(directoryPath);
		u.ContentPath = temp.tempPath;
		List<string> list = new List<string>(3);
		if (customCampaign.multiplayer)
		{
			list.Add("Multiplayer Campaigns");
		}
		else
		{
			list.Add("Campaigns");
			list.Add("Any Vehicle");
			list.Add(customCampaign.vehicle);
		}
		u.Tags = list;
		string iconPath = string.Empty;
		string[] files = Directory.GetFiles(customCampaign.filePath.Substring(0, customCampaign.filePath.LastIndexOf(Path.DirectorySeparatorChar)), "*", SearchOption.TopDirectoryOnly);
		foreach (string text in files)
		{
			if (text.EndsWith(".jpg") || text.EndsWith(".png"))
			{
				iconPath = text;
				if (text.Contains(Path.DirectorySeparatorChar + "image."))
				{
					break;
				}
			}
		}
		u.IconPath = iconPath;
		onBeginUpdate?.Invoke(u);
		VTSteamWorkshopUtils.UploadToWorkshop(u, delegate(WorkshopItemUpdateEventArgs args)
		{
			if (onComplete != null)
			{
				onComplete(args);
			}
			temp.Dispose();
		});
	}

	public static void UploadScenarioToSteamWorkshop(VTScenario currentScenario, RequestChangeNoteDelegate onRequestChangeNote, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		if (!SteamClient.IsValid)
		{
			return;
		}
		string scenarioDirectoryPath = GetScenarioDirectoryPath(currentScenario.scenarioID, currentScenario.campaignID);
		WorkshopItemUpdate u = VTSteamWorkshopUtils.GetItemUpdateFromFolder(scenarioDirectoryPath);
		if (u != null)
		{
			VTSteamWorkshopUtils.CheckCanUpdateItem(u.PublishedFileId, delegate(bool able, string msg)
			{
				if (able)
				{
					if (onRequestChangeNote != null)
					{
						onRequestChangeNote(delegate(string note)
						{
							_FinallyUploadScenario(currentScenario, note, u, onBeginUpdate, onComplete);
						}, delegate
						{
							onComplete(new WorkshopItemUpdateEventArgs
							{
								IsError = true,
								ErrorMessage = "Upload cancelled"
							});
						});
					}
					else
					{
						_FinallyUploadScenario(currentScenario, string.Empty, u, onBeginUpdate, onComplete);
					}
				}
				else
				{
					u = new WorkshopItemUpdate();
					_FinallyUploadScenario(currentScenario, string.Empty, u, onBeginUpdate, onComplete);
				}
			});
		}
		else
		{
			u = new WorkshopItemUpdate();
			_FinallyUploadScenario(currentScenario, string.Empty, u, onBeginUpdate, onComplete);
		}
	}

	private static void _FinallyUploadScenario(VTScenario currentScenario, string changeNote, WorkshopItemUpdate u, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		UnityEngine.Debug.Log("Finally uploading scenario to workshop.");
		string scenarioDirectoryPath = GetScenarioDirectoryPath(currentScenario.scenarioID, currentScenario.campaignID);
		if (string.IsNullOrEmpty(currentScenario.scenarioName))
		{
			u.Name = currentScenario.scenarioID;
		}
		else
		{
			u.Name = currentScenario.scenarioName;
		}
		if (string.IsNullOrEmpty(currentScenario.scenarioDescription))
		{
			u.Description = "No description.\n\nby " + SteamClient.Name;
		}
		else
		{
			u.Description = currentScenario.scenarioDescription + "\n\nby " + SteamClient.Name;
		}
		if (!string.IsNullOrEmpty(changeNote))
		{
			u.ChangeNote = changeNote;
		}
		VTSteamWorkshopUtils.VTWorkshopUploadTempFile temp = VTSteamWorkshopUtils.GetSWUploadFile(scenarioDirectoryPath);
		u.ContentPath = temp.tempPath;
		List<string> list = new List<string>(3);
		list.Add("Single Scenarios");
		list.Add("Any Vehicle");
		list.Add(currentScenario.vehicle.vehicleName);
		u.Tags = list;
		string text = Path.Combine(scenarioDirectoryPath, "image.jpg");
		if (!File.Exists(text))
		{
			text = Path.Combine(scenarioDirectoryPath, "image.png");
		}
		if (File.Exists(text))
		{
			u.IconPath = text;
		}
		onBeginUpdate?.Invoke(u);
		VTSteamWorkshopUtils.UploadToWorkshop(u, delegate(WorkshopItemUpdateEventArgs a)
		{
			if (onComplete != null)
			{
				onComplete(a);
			}
			temp.Dispose();
		});
	}

	public static void UploadMapToSteamWorkshop(string mapID, RequestChangeNoteDelegate onRequestChangeNote, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		string mapDirectoryPath = GetMapDirectoryPath(mapID);
		WorkshopItemUpdate u = VTSteamWorkshopUtils.GetItemUpdateFromFolder(mapDirectoryPath);
		if (u != null)
		{
			VTSteamWorkshopUtils.CheckCanUpdateItem(u.PublishedFileId, delegate(bool able, string message)
			{
				if (able)
				{
					if (onRequestChangeNote != null)
					{
						onRequestChangeNote(delegate(string note)
						{
							_FinallyUploadMap(mapID, note, u, onBeginUpdate, onComplete);
						}, delegate
						{
							onComplete(new WorkshopItemUpdateEventArgs
							{
								IsError = true,
								ErrorMessage = "Upload cancelled"
							});
						});
					}
					else
					{
						_FinallyUploadMap(mapID, string.Empty, u, onBeginUpdate, onComplete);
					}
				}
				else
				{
					u = new WorkshopItemUpdate();
					_FinallyUploadMap(mapID, string.Empty, u, onBeginUpdate, onComplete);
				}
			});
		}
		else
		{
			u = new WorkshopItemUpdate();
			_FinallyUploadMap(mapID, string.Empty, u, onBeginUpdate, onComplete);
		}
	}

	private static void _FinallyUploadMap(string mapID, string changeNote, WorkshopItemUpdate u, Action<WorkshopItemUpdate> onBeginUpdate, Action<WorkshopItemUpdateEventArgs> onComplete)
	{
		VTMapCustom customMap = GetCustomMap(mapID);
		string mapDirectoryPath = GetMapDirectoryPath(mapID);
		if (string.IsNullOrEmpty(customMap.mapName))
		{
			u.Name = customMap.mapID;
		}
		else
		{
			u.Name = customMap.mapName;
		}
		if (string.IsNullOrEmpty(customMap.mapDescription))
		{
			u.Description = "No description.\n\nby" + SteamClient.Name;
		}
		else
		{
			u.Description = customMap.mapDescription + "\n\nby " + SteamClient.Name;
		}
		if (!string.IsNullOrEmpty(changeNote))
		{
			u.ChangeNote = changeNote;
		}
		VTSteamWorkshopUtils.VTWorkshopUploadTempFile temp = VTSteamWorkshopUtils.GetSWUploadFile(mapDirectoryPath);
		UnityEngine.Debug.Log("Creating temp file for upload: " + temp.tempPath);
		u.ContentPath = temp.tempPath;
		string text = Path.Combine(mapDirectoryPath, "preview.jpg");
		if (File.Exists(text))
		{
			u.IconPath = text;
		}
		string text2 = Path.Combine(temp.tempPath, "height.png");
		if (File.Exists(text2))
		{
			VTSteamWorkshopUtils.WSEncode(text2);
		}
		List<string> list = new List<string>(1);
		list.Add("Maps");
		u.Tags = list;
		onBeginUpdate?.Invoke(u);
		VTSteamWorkshopUtils.UploadToWorkshop(u, delegate(WorkshopItemUpdateEventArgs a)
		{
			if (onComplete != null)
			{
				onComplete(a);
			}
			temp.Dispose();
		});
	}

	public static Texture2D RenderMapToPreview(VTMapCustom map, int size)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(mapPreviewSetupPrefab);
		Texture2D result = gameObject.GetComponentInChildren<VTMapPreviewSetup>().ShootMap(map, size);
		UnityEngine.Object.Destroy(gameObject);
		return result;
	}

	public static void SaveToPng(Texture2D tex, string path)
	{
		byte[] bytes = tex.EncodeToPNG();
		File.WriteAllBytes(path, bytes);
	}

	public static void SaveToJpg(Texture2D tex, string path, int quality = 75)
	{
		byte[] bytes = tex.EncodeToJPG(quality);
		File.WriteAllBytes(path, bytes);
	}
}
