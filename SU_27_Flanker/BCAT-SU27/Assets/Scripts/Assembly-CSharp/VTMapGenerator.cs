using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VTMapGenerator : MonoBehaviour, ITerrainJobServer
{
	public enum VTMapTypes
	{
		Archipelago,
		Mountain_Lakes,
		HeightMap
	}

	public enum EdgeModes
	{
		Water,
		Hills,
		Coast
	}

	public delegate void ChunkDelegate(VTTerrainChunk chunk);

	[Serializable]
	public class VTTerrainMeshProfile
	{
		public Mesh mesh;

		public bool colliders;

		public int lodRadius;
	}

	public class VTTerrainChunk
	{
		public IntVector2 grid;

		public Transform gridTransform;

		public GameObject[] lodObjects;

		public Mesh[] sharedMeshes;

		public VTTerrainMesh[] terrainMeshes;

		public TerrainModInfo mods;

		public VTMapGenerator generator;

		public bool colliderBaked;

		public MeshCollider collider;

		public int lodsGenerated;

		public List<GroundUnitMover> groundMovers = new List<GroundUnitMover>();

		public void UpdateWorldPosition()
		{
			gridTransform.position = generator.GridToWorldPos(grid);
		}

		public void OnOriginShift(Vector3 offset)
		{
			UpdateWorldPosition();
		}

		public void RecalculateMeshes()
		{
			generator.RecalculateGrid(grid);
		}
	}

	public class TerrainModInfo
	{
		private Dictionary<string, List<VTTerrainMod>> prefabMods = new Dictionary<string, List<VTTerrainMod>>();

		public void AddPrefabMod(string id, VTTerrainMod mod)
		{
			List<VTTerrainMod> list;
			if (prefabMods.ContainsKey(id))
			{
				list = prefabMods[id];
			}
			else
			{
				list = new List<VTTerrainMod>();
				prefabMods.Add(id, list);
			}
			list.Add(mod);
		}

		public List<VTTerrainMod> GetAllMods()
		{
			List<VTTerrainMod> list = new List<VTTerrainMod>();
			foreach (List<VTTerrainMod> value in prefabMods.Values)
			{
				foreach (VTTerrainMod item in value)
				{
					list.Add(item);
				}
			}
			return list;
		}

		public bool RemoveMod(string prefabId)
		{
			return prefabMods.Remove(prefabId);
		}
	}

	[Serializable]
	public class VTOOBTerrainProfile
	{
		public Mesh mesh;

		public Texture2D heightMap;

		public int repeatScale = 6;

		public Material editorMaterial;
	}

	public bool isPreview;

	public VTMapTypes mapType;

	public MapGenBiome.Biomes biome;

	public MapGenBiome[] biomeProfiles;

	public EdgeModes edgeMode;

	public CardinalDirections coastSide;

	public bool generateOnStart = true;

	public int noiseSeed = 1345;

	public int gridSize;

	public float chunkSize;

	public Material terrainMaterial;

	public Material cityMaterial;

	public VTTerrainMeshProfile[] terrainProfiles;

	public int jobPoolCount = 5000;

	private int maxJobsProcessedPerFrame = 4;

	private VTTerrainMesh[] readonlyTMeshes;

	public Transform playerTransform;

	private bool progressiveColliderBaking = true;

	public VTOOBTerrainProfile oobProfile;

	private VTTerrainChunk[,] chunks;

	private VTTerrainWorker[] workers;

	private Queue<VTTerrainJob> jobQueue = new Queue<VTTerrainJob>();

	private Stack<VTTerrainJob> jobPool = new Stack<VTTerrainJob>();

	private int jobsRemaining;

	private int lodCount;

	private bool generating;

	private bool _finishedInitialGen;

	private int TERRAIN_STATIC_BATCH_SIZE = 4;

	public Texture2D heightMap;

	public float hm_maxHeight = 6000f;

	public float hm_minHeight = -80f;

	public BDTexture hmBdt;

	private ConfigNode terrainSettings;

	private Texture2D[] splitHeightMaps;

	private FlightSceneManager.FlightSceneLoadItem loadItem;

	private bool recalculatingRoads;

	private List<Coroutine> genCoroutines = new List<Coroutine>();

	private Coroutine lodRoutine;

	private bool[,] colliderBakesComplete;

	private int bakeTotal;

	private int bakeFinished;

	private Queue<VTTerrainJob> jobPushQueue = new Queue<VTTerrainJob>();

	private object jobQueueLock = new object();

	public BDTexture oobHeightMap;

	private List<Mesh> meshesToDestroyOnCleanup = new List<Mesh>();

	private ObjectPool[,] oobPools;

	private Dictionary<IntVector2, GameObject> activeOOBObjects = new Dictionary<IntVector2, GameObject>();

	private bool oobDone;

	private ObjectPool[,] coastOOBPools;

	public static VTMapGenerator fetch { get; private set; }

	public MapGenBiome currentBiome => biomeProfiles[(int)biome];

	public float loadPercent
	{
		get
		{
			if (loadItem != null)
			{
				return Mathf.Clamp01(loadItem.currentValue / loadItem.maxValue);
			}
			return 0f;
		}
	}

	public bool colliderBakeComplete { get; private set; }

	private bool hasOOB
	{
		get
		{
			if (edgeMode != 0 && oobProfile != null)
			{
				return oobProfile.mesh != null;
			}
			return false;
		}
	}

	public event ChunkDelegate OnChunkGenerated;

	public event ChunkDelegate OnChunkGeneratedEarly;

	public event ChunkDelegate OnChunkRecalculated;

	public event ChunkDelegate OnChunkRecalculatedEarly;

	public VTTerrainChunk GetTerrainChunk(int x, int y)
	{
		return chunks[x, y];
	}

	public VTTerrainChunk GetTerrainChunk(IntVector2 grid)
	{
		if (grid.x >= 0 && grid.y >= 0 && grid.x < gridSize && grid.y < gridSize)
		{
			return chunks[grid.x, grid.y];
		}
		return null;
	}

	public bool IsPositionOutOfBounds(Vector3 worldPos)
	{
		if (GetTerrainChunk(worldPos) != null)
		{
			return false;
		}
		return true;
	}

	public VTTerrainChunk GetTerrainChunk(Vector3 worldPos)
	{
		return GetTerrainChunk(ChunkGridAtPos(worldPos));
	}

	public int GetJobsRemaining()
	{
		return jobsRemaining;
	}

	public bool IsGenerating()
	{
		return generating;
	}

	public bool HasFinishedInitialGeneration()
	{
		return _finishedInitialGen;
	}

	private void Awake()
	{
		fetch = this;
		lodCount = terrainProfiles.Length;
		if (GameSettings.TryGetGameSettingValue<int>("TERRAIN_STATIC_BATCH_SIZE", out var val))
		{
			TERRAIN_STATIC_BATCH_SIZE = val;
		}
		SetupReadonlyMeshes();
		SetupJobPool();
		SetupWorkers();
	}

	private void Start()
	{
		if (generateOnStart)
		{
			Generate();
		}
	}

	private void SetupReadonlyMeshes()
	{
		readonlyTMeshes = new VTTerrainMesh[lodCount];
		for (int i = 0; i < lodCount; i++)
		{
			readonlyTMeshes[i] = new VTTerrainMesh(terrainProfiles[i].mesh);
			if ((bool)cityMaterial)
			{
				readonlyTMeshes[i].subMeshCount = 2;
			}
		}
	}

	private void SetupJobPool()
	{
		jobPoolCount = gridSize * gridSize;
		jobPool.Clear();
		for (int i = 0; i < jobPoolCount; i++)
		{
			VTTerrainJob terrainJob = GetTerrainJob();
			terrainJob.Initialize(readonlyTMeshes);
			if (terrainSettings != null)
			{
				terrainJob.ApplySettings(terrainSettings);
			}
			jobPool.Push(terrainJob);
		}
	}

	public static VTTerrainJob GetJobInstanceFromType(VTMapTypes type)
	{
		return (VTTerrainJob)Activator.CreateInstance(GetJobType(type));
	}

	private VTTerrainJob GetTerrainJob()
	{
		VTTerrainJob jobInstanceFromType = GetJobInstanceFromType(mapType);
		jobInstanceFromType.chunkSize = chunkSize;
		jobInstanceFromType.mapSize = gridSize;
		if (mapType == VTMapTypes.HeightMap)
		{
			if (hmBdt == null)
			{
				CreateHeightmapBDTexture();
			}
			VTTHeightMap vTTHeightMap = (VTTHeightMap)jobInstanceFromType;
			vTTHeightMap.heightMap = hmBdt;
			vTTHeightMap.maxHeight = hm_maxHeight;
			vTTHeightMap.minHeight = hm_minHeight;
			vTTHeightMap.biome = biome;
			if (edgeMode == EdgeModes.Hills)
			{
				vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Heightmap;
				vTTHeightMap.oobHeightmap = oobHeightMap;
				if (oobProfile != null)
				{
					vTTHeightMap.oobGridScale = oobProfile.repeatScale;
				}
			}
			else if (edgeMode == EdgeModes.Coast)
			{
				vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Coast;
				vTTHeightMap.coastSide = coastSide;
				if (oobProfile != null)
				{
					vTTHeightMap.oobHeightmap = oobHeightMap;
					vTTHeightMap.oobGridScale = oobProfile.repeatScale;
					if (coastSide == CardinalDirections.North)
					{
						vTTHeightMap.oobGridOffset = new IntVector2(0, oobProfile.repeatScale - (gridSize - oobProfile.repeatScale));
					}
					else if (coastSide == CardinalDirections.East)
					{
						vTTHeightMap.oobGridOffset = new IntVector2(oobProfile.repeatScale - (gridSize - oobProfile.repeatScale), 0);
					}
				}
			}
			else if (edgeMode == EdgeModes.Water)
			{
				vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Water;
			}
			vTTHeightMap.loadCities = cityMaterial != null;
		}
		return jobInstanceFromType;
	}

	private void CreateHeightmapBDTexture()
	{
		int num = 20 * gridSize + 1;
		if (splitHeightMaps != null && splitHeightMaps.Length != 0 && splitHeightMaps[0].width == splitHeightMaps[0].height && splitHeightMaps[0].width == num)
		{
			hmBdt = new BDTexture(splitHeightMaps);
			UpdateJobPool();
		}
		else
		{
			BDColor[,] array = new BDColor[num, num];
			bool flag = num == heightMap.width;
			for (int i = 0; i < num; i++)
			{
				for (int j = 0; j < num; j++)
				{
					if (flag)
					{
						Color pixel = heightMap.GetPixel(i, j);
						array[i, j] = new BDColor(pixel.r, pixel.g, pixel.b, pixel.a);
					}
					else
					{
						Vector2 vector = new Vector2(i, j) / (num - 1);
						Color pixelBilinear = heightMap.GetPixelBilinear(vector.x, vector.y);
						array[i, j] = new BDColor(pixelBilinear.r, pixelBilinear.g, pixelBilinear.b, pixelBilinear.a);
					}
				}
			}
			if (hmBdt != null)
			{
				hmBdt.SetPixels(array);
			}
			else
			{
				hmBdt = new BDTexture(array);
				UpdateJobPool();
			}
		}
		if (oobProfile != null && oobProfile.heightMap != null)
		{
			oobHeightMap = new BDTexture(oobProfile.heightMap);
			oobHeightMap.wrapMode = BDTexture.WrapModes.Repeat;
		}
	}

	public void SetNewHeightmap(Texture2D tex)
	{
		heightMap = tex;
		CreateHeightmapBDTexture();
	}

	private void UpdateJobPool()
	{
		jobPoolCount = gridSize * gridSize;
		while (jobPool.Count > jobPoolCount)
		{
			jobPool.Pop();
		}
		bool flag = mapType == VTMapTypes.HeightMap;
		Stack<VTTerrainJob> stack = new Stack<VTTerrainJob>();
		while (jobPool.Count > 0)
		{
			VTTerrainJob vTTerrainJob = jobPool.Pop();
			if (terrainSettings != null)
			{
				vTTerrainJob.ApplySettings(terrainSettings);
			}
			vTTerrainJob.mapSize = gridSize;
			if (flag)
			{
				VTTHeightMap vTTHeightMap = (VTTHeightMap)vTTerrainJob;
				vTTHeightMap.heightMap = hmBdt;
				vTTHeightMap.minHeight = hm_minHeight;
				vTTHeightMap.maxHeight = hm_maxHeight;
				if (edgeMode == EdgeModes.Hills)
				{
					vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Heightmap;
					if (oobProfile != null)
					{
						vTTHeightMap.oobHeightmap = oobHeightMap;
						vTTHeightMap.oobGridScale = oobProfile.repeatScale;
					}
				}
				else if (edgeMode == EdgeModes.Coast)
				{
					vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Coast;
					vTTHeightMap.coastSide = coastSide;
					if (oobProfile != null)
					{
						vTTHeightMap.oobHeightmap = oobHeightMap;
						vTTHeightMap.oobGridScale = oobProfile.repeatScale;
						if (coastSide == CardinalDirections.North)
						{
							vTTHeightMap.oobGridOffset = new IntVector2(0, oobProfile.repeatScale - (gridSize - oobProfile.repeatScale));
						}
						else if (coastSide == CardinalDirections.East)
						{
							vTTHeightMap.oobGridOffset = new IntVector2(oobProfile.repeatScale - (gridSize - oobProfile.repeatScale), 0);
						}
					}
				}
				else if (edgeMode == EdgeModes.Water)
				{
					vTTHeightMap.edgeMode = VTTHeightMap.EdgeModes.Water;
				}
			}
			stack.Push(vTTerrainJob);
		}
		jobPool = stack;
		if (jobPool.Count >= jobPoolCount)
		{
			return;
		}
		Mesh[] array = new Mesh[lodCount];
		for (int i = 0; i < lodCount; i++)
		{
			array[i] = terrainProfiles[i].mesh;
		}
		while (jobPool.Count < jobPoolCount)
		{
			VTTerrainJob terrainJob = GetTerrainJob();
			terrainJob.Initialize(readonlyTMeshes);
			if (terrainSettings != null)
			{
				terrainJob.ApplySettings(terrainSettings);
			}
			jobPool.Push(terrainJob);
		}
	}

	private void SetupWorkers()
	{
		int num = Mathf.Max(1, Environment.ProcessorCount - 2);
		Debug.Log("Setting up " + num + " workers.");
		workers = new VTTerrainWorker[num];
		for (int i = 0; i < num; i++)
		{
			workers[i] = new VTTerrainWorker();
			workers[i].Start(this, noiseSeed, 1000);
		}
	}

	private void StopWorkers()
	{
		if (workers == null)
		{
			return;
		}
		for (int i = 0; i < workers.Length; i++)
		{
			if (workers[i] != null)
			{
				workers[i].Stop();
			}
		}
	}

	private void OnDestroy()
	{
		StopWorkers();
		DestroyMeshes();
		GC.Collect();
	}

	private void DestroyMeshes()
	{
		if (chunks != null)
		{
			for (int i = 0; i < chunks.GetLength(0); i++)
			{
				for (int j = 0; j < chunks.GetLength(1); j++)
				{
					VTTerrainChunk vTTerrainChunk = chunks[i, j];
					if (vTTerrainChunk == null)
					{
						continue;
					}
					if (vTTerrainChunk.sharedMeshes != null)
					{
						for (int k = 0; k < vTTerrainChunk.sharedMeshes.Length; k++)
						{
							UnityEngine.Object.Destroy(vTTerrainChunk.sharedMeshes[k]);
						}
					}
					if (vTTerrainChunk.terrainMeshes == null)
					{
						continue;
					}
					for (int l = 0; l < vTTerrainChunk.terrainMeshes.Length; l++)
					{
						if (vTTerrainChunk.terrainMeshes[l] != null)
						{
							vTTerrainChunk.terrainMeshes[l].Clear();
							vTTerrainChunk.terrainMeshes[l] = null;
						}
					}
					vTTerrainChunk.terrainMeshes = null;
				}
			}
		}
		if (meshesToDestroyOnCleanup != null)
		{
			foreach (Mesh item in meshesToDestroyOnCleanup)
			{
				if (item != null)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
			meshesToDestroyOnCleanup = null;
		}
		if (readonlyTMeshes != null)
		{
			for (int m = 0; m < readonlyTMeshes.Length; m++)
			{
				readonlyTMeshes[m] = null;
			}
			readonlyTMeshes = null;
		}
	}

	[ContextMenu("Generate")]
	public void Generate()
	{
		if (!generating)
		{
			ClearMap();
			StartCoroutine(GenerationRoutine());
		}
	}

	public void GenerateVTMap(VTMapCustom map)
	{
		if ((bool)map.heightMap)
		{
			heightMap = map.heightMap;
		}
		if (map.splitHeightmaps != null && map.splitHeightmaps.Length != 0)
		{
			splitHeightMaps = map.splitHeightmaps;
		}
		gridSize = map.mapSize;
		SetSeed(map.seed);
		terrainSettings = map.terrainSettings;
		biome = map.biome;
		mapType = map.mapType;
		edgeMode = map.edgeMode;
		coastSide = map.coastSide;
		SetupJobPool();
		if ((bool)FlightSceneManager.instance)
		{
			loadItem = FlightSceneManager.instance.AddLoadItem();
			loadItem.maxValue = gridSize * gridSize * lodCount;
			if (hasOOB)
			{
				loadItem.maxValue += oobProfile.repeatScale * oobProfile.repeatScale;
			}
			loadItem.currentValue = 0f;
		}
		Generate();
	}

	public void SetSeed(string seed)
	{
		if (!string.IsNullOrEmpty(seed))
		{
			noiseSeed = StringSeedToInt(seed);
		}
	}

	public static int StringSeedToInt(string seed)
	{
		if (seed == "seed")
		{
			return 3526257;
		}
		return seed.GetHashCode() % 9999999;
	}

	public void ApplySettingsForPreview(VTMapTypes newMapType, EdgeModes newEdgeMode, ConfigNode terrainSettings)
	{
		if (!generating)
		{
			this.terrainSettings = terrainSettings;
			if (mapType != newMapType || edgeMode != newEdgeMode)
			{
				edgeMode = newEdgeMode;
				mapType = newMapType;
				SetupJobPool();
			}
			else
			{
				UpdateJobPool();
			}
		}
	}

	public void ClearMap()
	{
		if (chunks == null)
		{
			return;
		}
		int length = chunks.GetLength(0);
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length; j++)
			{
				VTTerrainChunk vTTerrainChunk = chunks[i, j];
				if (vTTerrainChunk != null && vTTerrainChunk.lodObjects != null)
				{
					GameObject[] lodObjects = vTTerrainChunk.lodObjects;
					for (int k = 0; k < lodObjects.Length; k++)
					{
						UnityEngine.Object.Destroy(lodObjects[k]);
					}
					Mesh[] sharedMeshes = vTTerrainChunk.sharedMeshes;
					for (int k = 0; k < sharedMeshes.Length; k++)
					{
						UnityEngine.Object.Destroy(sharedMeshes[k]);
					}
				}
			}
		}
	}

	private IEnumerator GenerationRoutine()
	{
		if (generating)
		{
			yield break;
		}
		generating = true;
		ResetColliderBakeProgress();
		if (loadItem != null)
		{
			StartCoroutine(LoadItemDoneRoutine());
		}
		if (chunks == null || chunks.GetLength(0) != gridSize)
		{
			chunks = new VTTerrainChunk[gridSize, gridSize];
			SetupJobPool();
		}
		else
		{
			UpdateJobPool();
		}
		for (int j = 0; j < workers.Length; j++)
		{
			workers[j].SetNoiseSeed(noiseSeed);
		}
		genCoroutines.Add(StartCoroutine(JobCollectionRoutine()));
		float t = Time.realtimeSinceStartup;
		Debug.Log("Starting jobs.");
		yield return null;
		int maxDispatchesPerFrame = 20;
		int dispatches = 0;
		jobsRemaining = gridSize * gridSize * lodCount;
		base.transform.position = Vector3.zero;
		WheelSurfaceMaterial defaultSurfaceMat = VTResources.defaultTerrainSurfaceMaterial;
		if (biomeProfiles != null && biomeProfiles.Length != 0)
		{
			MapGenBiome mapGenBiome = biomeProfiles[(int)biome];
			terrainMaterial = mapGenBiome.terrainMaterial;
			if ((bool)cityMaterial)
			{
				cityMaterial.SetColor("_Color", terrainMaterial.GetColor("_Color"));
				cityMaterial.SetTexture("_TileR", terrainMaterial.GetTexture("_TileR"));
				cityMaterial.SetTexture("_TileR_NRM", terrainMaterial.GetTexture("_TileR_NRM"));
				cityMaterial.SetFloat("_RScale", terrainMaterial.GetFloat("_RScale"));
				cityMaterial.SetColor("_RTint", terrainMaterial.GetColor("_RTint"));
				cityMaterial.SetTexture("_TileG", terrainMaterial.GetTexture("_TileG"));
				cityMaterial.SetTexture("_TileG_NRM", terrainMaterial.GetTexture("_TileG_NRM"));
				cityMaterial.SetFloat("_GScale", terrainMaterial.GetFloat("_GScale"));
				cityMaterial.SetColor("_GTint", terrainMaterial.GetColor("_GTint"));
				cityMaterial.SetTexture("_TileB", terrainMaterial.GetTexture("_TileB"));
				cityMaterial.SetTexture("_TileB_NRM", terrainMaterial.GetTexture("_TileB_NRM"));
				cityMaterial.SetFloat("_BScale", terrainMaterial.GetFloat("_BScale"));
				cityMaterial.SetColor("_BTint", terrainMaterial.GetColor("_BTint"));
				cityMaterial.SetTexture("_TileA", terrainMaterial.GetTexture("_TileA"));
				cityMaterial.SetTexture("_TileA_NRM", terrainMaterial.GetTexture("_TileA_NRM"));
				cityMaterial.SetFloat("_AScale", terrainMaterial.GetFloat("_AScale"));
				cityMaterial.SetColor("_ATint", terrainMaterial.GetColor("_ATint"));
				cityMaterial.SetTexture("_BeachTex", terrainMaterial.GetTexture("_BeachTex"));
				cityMaterial.SetFloat("_BeachScale", terrainMaterial.GetFloat("_BeachScale"));
				cityMaterial.SetFloat("_NrmAmt", terrainMaterial.GetFloat("_NrmAmt"));
			}
			if (mapGenBiome.treeProfile.overrideLodSizes != null && mapGenBiome.treeProfile.overrideLodSizes.Length == VTTMapTrees.fetch.treeProfiles.Length)
			{
				for (int k = 0; k < mapGenBiome.treeProfile.overrideLodSizes.Length; k++)
				{
					VTTMapTrees.fetch.treeProfiles[k].lodHeight = mapGenBiome.treeProfile.overrideLodSizes[k];
				}
			}
			if ((bool)mapGenBiome.defaultSurfaceMaterial)
			{
				defaultSurfaceMat = mapGenBiome.defaultSurfaceMaterial;
			}
		}
		Material[] mats = ((!cityMaterial) ? new Material[1] { terrainMaterial } : new Material[2] { terrainMaterial, cityMaterial });
		if (hasOOB)
		{
			StartCoroutine(CreateOOB());
		}
		Shader.SetGlobalTexture("_TerrainHeightMap", heightMap);
		Shader.SetGlobalFloat("_TerrainMapSize", gridSize);
		List<VTMapEdPrefab> prefabList = null;
		if ((bool)VTMapManager.fetch)
		{
			prefabList = ((VTMapCustom)VTMapManager.fetch.map).prefabs.GetAllPrefabs();
		}
		for (int x2 = 0; x2 < gridSize; x2++)
		{
			for (int y = 0; y < gridSize; y++)
			{
				VTTerrainChunk c = (chunks[x2, y] = new VTTerrainChunk());
				c.generator = this;
				c.grid = new IntVector2(x2, y);
				c.lodObjects = new GameObject[lodCount];
				c.sharedMeshes = new Mesh[lodCount];
				c.terrainMeshes = new VTTerrainMesh[lodCount];
				c.gridTransform = new GameObject("TerrainChunk(" + x2 + "," + y + ")").transform;
				c.mods = new TerrainModInfo();
				if (prefabList != null)
				{
					LoadModsFromMap(prefabList, c);
				}
				VTTerrainMod[] chunkMods = c.mods.GetAllMods().ToArray();
				c.UpdateWorldPosition();
				if ((bool)FloatingOrigin.instance)
				{
					FloatingOrigin.instance.OnOriginShift += c.OnOriginShift;
				}
				for (int i = 0; i < lodCount; i++)
				{
					if (dispatches >= maxDispatchesPerFrame)
					{
						PushJobQueue();
						yield return null;
						dispatches = 0;
					}
					string text = "terrain";
					GameObject go = (c.lodObjects[i] = new GameObject(text));
					go.transform.parent = c.gridTransform;
					go.transform.localPosition = Vector3.zero;
					go.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
					MeshFilter meshFilter = go.AddComponent<MeshFilter>();
					MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
					Mesh mesh = new Mesh();
					meshesToDestroyOnCleanup.Add(mesh);
					meshFilter.sharedMesh = mesh;
					meshFilter.sharedMesh.name = go.name;
					meshRenderer.sharedMaterials = mats;
					if (i > 0)
					{
						meshRenderer.receiveShadows = false;
						meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
					}
					c.sharedMeshes[i] = meshFilter.sharedMesh;
					if (terrainProfiles[i].colliders)
					{
						MeshCollider meshCollider = (c.collider = go.AddComponent<MeshCollider>());
						WheelSurface.RegisterMaterial(meshCollider, defaultSurfaceMat);
						if (progressiveColliderBaking)
						{
							meshCollider.enabled = false;
						}
						else
						{
							meshCollider.sharedMesh = meshFilter.sharedMesh;
							c.colliderBaked = true;
						}
					}
					while (jobPool.Count < 1)
					{
						yield return null;
					}
					VTTerrainJob vTTerrainJob = jobPool.Pop();
					vTTerrainJob.isRecalculate = false;
					vTTerrainJob.gridPosition = c.grid;
					vTTerrainJob.lod = i;
					vTTerrainJob.mods = chunkMods;
					jobPushQueue.Enqueue(vTTerrainJob);
					dispatches++;
					go.SetActive(terrainProfiles[i].colliders || i == 0);
				}
			}
		}
		PushJobQueue();
		Debug.Log("All jobs dispatched.  Waiting for job completion.");
		while (jobsRemaining > 0)
		{
			yield return null;
		}
		Debug.Log("All jobs complete.  Time: " + (Time.realtimeSinceStartup - t));
		if (!isPreview && TERRAIN_STATIC_BATCH_SIZE > 0 && VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
		{
			int tERRAIN_STATIC_BATCH_SIZE = TERRAIN_STATIC_BATCH_SIZE;
			VTTerrainMesh vTTerrainMesh = new VTTerrainMesh();
			float num = chunkSize * (float)tERRAIN_STATIC_BATCH_SIZE;
			for (int l = 0; l < gridSize; l += tERRAIN_STATIC_BATCH_SIZE)
			{
				for (int m = 0; m < gridSize; m += tERRAIN_STATIC_BATCH_SIZE)
				{
					for (int n = 0; n < lodCount; n++)
					{
						VTTerrainChunk vTTerrainChunk = chunks[l, m];
						vTTerrainChunk.terrainMeshes[n].gridPos = vTTerrainChunk.grid;
						vTTerrainMesh.gridPos = vTTerrainChunk.grid;
						vTTerrainMesh.AppendMesh(vTTerrainChunk.terrainMeshes[n], chunkSize, forceColors: true, forceTangents: true);
						float num2 = 0f;
						for (int num3 = l; num3 < l + tERRAIN_STATIC_BATCH_SIZE && num3 < gridSize; num3++)
						{
							for (int num4 = m; num4 < m + tERRAIN_STATIC_BATCH_SIZE && num4 < gridSize; num4++)
							{
								if (num3 != l || num4 != m)
								{
									VTTerrainChunk vTTerrainChunk2 = chunks[num3, num4];
									vTTerrainChunk2.terrainMeshes[n].gridPos = vTTerrainChunk2.grid;
									vTTerrainMesh.AppendMesh(vTTerrainChunk2.terrainMeshes[n], chunkSize);
									vTTerrainChunk2.lodObjects[n].GetComponent<MeshRenderer>().enabled = false;
								}
								num2 = Mathf.Max(num2, chunks[num3, num4].lodObjects[n].GetComponent<MeshFilter>().sharedMesh.bounds.size.z);
							}
						}
						Mesh mesh2 = new Mesh();
						meshesToDestroyOnCleanup.Add(mesh2);
						vTTerrainMesh.ApplyToMesh(mesh2, recalculateBounds: false);
						mesh2.bounds = new Bounds(new Vector3((0f - num) / 2f, num / 2f, num2 / 2f), new Vector3(num, num, num2));
						vTTerrainChunk.lodObjects[n].GetComponent<MeshFilter>().sharedMesh = mesh2;
						if (vTTerrainMesh.subMeshTriangles[1].Count == 0)
						{
							MeshRenderer component = vTTerrainChunk.lodObjects[n].GetComponent<MeshRenderer>();
							component.sharedMaterials = new Material[1] { component.sharedMaterials[0] };
						}
						vTTerrainMesh.Clear();
					}
				}
			}
		}
		if (!playerTransform && (bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
		{
			playerTransform = FlightSceneManager.instance.playerActor.transform;
		}
		if ((bool)playerTransform)
		{
			StartCoroutine(LODRoutine());
		}
		else
		{
			for (int x2 = 0; x2 < gridSize; x2++)
			{
				for (int num5 = 0; num5 < gridSize; num5++)
				{
					VTTerrainChunk vTTerrainChunk3 = chunks[x2, num5];
					if (vTTerrainChunk3 == null)
					{
						continue;
					}
					for (int num6 = 0; num6 < lodCount; num6++)
					{
						if ((bool)vTTerrainChunk3.lodObjects[num6])
						{
							vTTerrainChunk3.lodObjects[num6].SetActive(num6 == 0);
						}
					}
				}
				yield return null;
			}
		}
		_finishedInitialGen = true;
		Resources.UnloadUnusedAssets();
		GC.Collect();
		if (progressiveColliderBaking)
		{
			StartCoroutine(ColliderBakeRoutine());
		}
	}

	private IntVector2 GetBatchAdjustedLODGrid(IntVector2 playerGrid, IntVector2 tGrid)
	{
		IntVector2 batchedParentGrid = GetBatchedParentGrid(tGrid);
		IntVector2 intVector = playerGrid - batchedParentGrid;
		if (playerGrid.x > tGrid.x)
		{
			tGrid.x = batchedParentGrid.x + Mathf.Min(TERRAIN_STATIC_BATCH_SIZE - 1, intVector.x);
		}
		if (playerGrid.y > tGrid.y)
		{
			tGrid.y = batchedParentGrid.y + Mathf.Min(TERRAIN_STATIC_BATCH_SIZE - 1, intVector.y);
		}
		return tGrid;
	}

	private IntVector2 GetBatchedParentGrid(IntVector2 grid)
	{
		int num = grid.x % TERRAIN_STATIC_BATCH_SIZE;
		int num2 = grid.y % TERRAIN_STATIC_BATCH_SIZE;
		return new IntVector2(grid.x - num, grid.y - num2);
	}

	private IEnumerator LoadItemDoneRoutine()
	{
		while (!_finishedInitialGen)
		{
			yield return null;
		}
		if (hasOOB)
		{
			while (!oobDone)
			{
				yield return null;
			}
		}
		if (loadItem != null)
		{
			loadItem.done = true;
		}
	}

	private void LoadModsFromMap(List<VTMapEdPrefab> prefabList, VTTerrainChunk chunk)
	{
		foreach (VTMapEdPrefab prefab in prefabList)
		{
			foreach (VTTerrainMod terrainMod in prefab.GetTerrainMods())
			{
				if (terrainMod.AppliesToChunk(chunk))
				{
					chunk.mods.AddPrefabMod("prefab" + prefab.id, terrainMod);
				}
			}
		}
	}

	public void RemoveMod(string modID)
	{
		List<IntVector2> list = new List<IntVector2>();
		for (int i = 0; i < gridSize; i++)
		{
			for (int j = 0; j < gridSize; j++)
			{
				if (chunks[i, j].mods.RemoveMod(modID))
				{
					list.Add(new IntVector2(i, j));
				}
			}
		}
		foreach (IntVector2 item in list)
		{
			RecalculateGrid(item);
		}
	}

	public void RecalculateGrid(IntVector2 grid)
	{
		StartCoroutine(RecalcGridRoutine(grid));
	}

	private IEnumerator RecalcGridRoutine(IntVector2 grid)
	{
		while (jobPool.Count < terrainProfiles.Length)
		{
			yield return null;
		}
		chunks[grid.x, grid.y].lodsGenerated = 0;
		for (int i = 0; i < terrainProfiles.Length; i++)
		{
			VTTerrainJob vTTerrainJob = jobPool.Pop();
			vTTerrainJob.gridPosition = grid;
			vTTerrainJob.lod = i;
			vTTerrainJob.mods = GetTerrainChunk(grid).mods.GetAllMods().ToArray();
			vTTerrainJob.isRecalculate = true;
			jobsRemaining++;
			jobPushQueue.Enqueue(vTTerrainJob);
		}
		yield return null;
		PushJobQueue();
		StartCoroutine(RecalcRoadsRoutine());
	}

	private IEnumerator RecalcRoadsRoutine()
	{
		if (!recalculatingRoads)
		{
			recalculatingRoads = true;
			yield return null;
			while (generating)
			{
				yield return null;
			}
			yield return null;
			if ((bool)VTMapRoads.instance)
			{
				VTMapRoads.instance.RecalculateAllHeights();
			}
			recalculatingRoads = false;
		}
	}

	private IEnumerator JobCollectionRoutine()
	{
		Queue<VTTerrainJob> colliderJobQueue = new Queue<VTTerrainJob>();
		Queue<VTTerrainJob> nonColliderJobQueue = new Queue<VTTerrainJob>();
		genCoroutines.Add(StartCoroutine(JobProcessRoutine(colliderJobQueue, progressiveColliderBaking ? maxJobsProcessedPerFrame : 2)));
		genCoroutines.Add(StartCoroutine(JobProcessRoutine(nonColliderJobQueue, maxJobsProcessedPerFrame)));
		while (base.enabled)
		{
			while (jobsRemaining == 0)
			{
				yield return null;
			}
			generating = true;
			while (jobsRemaining > 0)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					_ = workers[i];
					bool flag = true;
					while (flag)
					{
						VTTerrainJob jobOutput = workers[i].GetJobOutput();
						if (jobOutput != null)
						{
							if (terrainProfiles[jobOutput.lod].colliders)
							{
								colliderJobQueue.Enqueue(jobOutput);
							}
							else
							{
								nonColliderJobQueue.Enqueue(jobOutput);
							}
						}
						else
						{
							flag = false;
						}
					}
				}
				yield return null;
			}
			generating = false;
		}
	}

	private IEnumerator JobProcessRoutine(Queue<VTTerrainJob> jobs, int maxJobs)
	{
		while (base.enabled)
		{
			int jobsProcessed = 0;
			while (jobsRemaining == 0)
			{
				yield return null;
			}
			while (jobsRemaining > 0)
			{
				while (jobs.Count < 1 && jobsRemaining > 0)
				{
					yield return null;
				}
				if (jobsRemaining <= 0 || jobs == null || jobs.Count == 0)
				{
					break;
				}
				VTTerrainJob vTTerrainJob = jobs.Dequeue();
				_ = vTTerrainJob.lod;
				ProcessJobOutput(vTTerrainJob);
				jobsProcessed++;
				if (jobsProcessed >= maxJobs)
				{
					yield return null;
					jobsProcessed = 0;
				}
			}
		}
	}

	private void ProcessJobOutput(VTTerrainJob job)
	{
		VTTerrainChunk vTTerrainChunk = chunks[job.gridPosition.x, job.gridPosition.y];
		if (vTTerrainChunk != null && vTTerrainChunk.sharedMeshes != null && job.lod < vTTerrainChunk.sharedMeshes.Length && vTTerrainChunk.sharedMeshes[job.lod] != null)
		{
			VTTerrainMesh vTTerrainMesh = job.outputMeshes[job.lod];
			for (int i = 0; i < vTTerrainMesh.subMeshTriangles.Length; i++)
			{
				if (vTTerrainMesh.subMeshTriangles[i].Count <= 3)
				{
					vTTerrainMesh.subMeshTriangles[i] = new List<int>();
				}
			}
			vTTerrainMesh.ApplyToMesh(vTTerrainChunk.sharedMeshes[job.lod], recalculateBounds: false);
			float maxHeight = job.maxHeight;
			vTTerrainChunk.sharedMeshes[job.lod].bounds = new Bounds(new Vector3((0f - chunkSize) / 2f, chunkSize / 2f, maxHeight / 2f), new Vector3(chunkSize, chunkSize, maxHeight));
			vTTerrainChunk.terrainMeshes[job.lod] = job.outputMeshes[job.lod];
			job.outputMeshes[job.lod] = null;
			vTTerrainChunk.lodObjects[job.lod].transform.position = GridToWorldPos(job.gridPosition);
			vTTerrainChunk.lodsGenerated++;
			jobsRemaining--;
			if (loadItem != null)
			{
				loadItem.currentValue += 1f;
			}
			if (terrainProfiles[job.lod].colliders && job.isRecalculate)
			{
				vTTerrainChunk.lodObjects[job.lod].SetActive(value: false);
				vTTerrainChunk.lodObjects[job.lod].SetActive(value: true);
			}
			if (vTTerrainChunk.lodsGenerated == lodCount)
			{
				if (job.isRecalculate)
				{
					InvokeChunkRecalculated(vTTerrainChunk);
				}
				else
				{
					InvokeChunkComplete(vTTerrainChunk);
				}
			}
		}
		else
		{
			Debug.Log("A terrain job output had no destination mesh.");
		}
		jobPool.Push(job);
	}

	private void InvokeChunkComplete(VTTerrainChunk chunk)
	{
		if (this.OnChunkGeneratedEarly != null)
		{
			this.OnChunkGeneratedEarly(chunk);
		}
		if (this.OnChunkGenerated != null)
		{
			this.OnChunkGenerated(chunk);
		}
	}

	private void InvokeChunkRecalculated(VTTerrainChunk chunk)
	{
		if (this.OnChunkRecalculatedEarly != null)
		{
			this.OnChunkRecalculatedEarly(chunk);
		}
		if (this.OnChunkRecalculated != null)
		{
			this.OnChunkRecalculated(chunk);
		}
	}

	public Vector3 GridToWorldPos(IntVector2 grid)
	{
		return (new Vector3D((float)grid.x * chunkSize, 0.0, (float)grid.y * chunkSize) - FloatingOrigin.accumOffset).toVector3;
	}

	public void StartLODRoutine(Transform playerTf)
	{
		playerTransform = playerTf;
		if (lodRoutine != null)
		{
			StopCoroutine(lodRoutine);
		}
		lodRoutine = StartCoroutine(LODRoutine());
	}

	private IEnumerator LODRoutine()
	{
		int maxLODUpdates = 30;
		int currLODUpdates = 0;
		int interval = 1;
		while (base.enabled)
		{
			IntVector2 playerPos = WorldToGridPos(playerTransform.position);
			for (int x = 0; x < gridSize; x += interval)
			{
				for (int y = 0; y < gridSize; y += interval)
				{
					IntVector2 intVector = new IntVector2(x, y);
					if (TERRAIN_STATIC_BATCH_SIZE > 0)
					{
						intVector = GetBatchAdjustedLODGrid(playerPos, intVector);
					}
					int num = Mathf.Abs(IntVector2.MaxOffset(intVector, playerPos));
					VTTerrainChunk vTTerrainChunk = chunks[x, y];
					int num2 = terrainProfiles.Length;
					if (vTTerrainChunk.groundMovers.Count > 0)
					{
						num2 = 0;
					}
					else
					{
						for (int num3 = terrainProfiles.Length - 1; num3 >= 0; num3--)
						{
							if (num <= terrainProfiles[num3].lodRadius)
							{
								num2 = num3;
							}
						}
					}
					for (int i = 0; i < vTTerrainChunk.lodObjects.Length; i++)
					{
						vTTerrainChunk.lodObjects[i].SetActive(i == num2);
					}
					currLODUpdates++;
					if (currLODUpdates >= maxLODUpdates)
					{
						yield return null;
						currLODUpdates = 0;
					}
				}
			}
			while (!playerTransform || WorldToGridPos(playerTransform.position) == playerPos)
			{
				if (!playerTransform && (bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
				{
					playerTransform = FlightSceneManager.instance.playerActor.transform;
				}
				yield return null;
			}
		}
	}

	private void ResetColliderBakeProgress()
	{
		if (colliderBakesComplete == null)
		{
			colliderBakesComplete = new bool[gridSize, gridSize];
		}
		else
		{
			for (int i = 0; i < colliderBakesComplete.GetLength(0); i++)
			{
				for (int j = 0; j < colliderBakesComplete.GetLength(1); j++)
				{
					colliderBakesComplete[i, j] = false;
				}
			}
		}
		bakeTotal = gridSize * gridSize;
		bakeFinished = 0;
	}

	private IEnumerator ColliderBakeRoutine()
	{
		while (!VRHead.instance)
		{
			yield return null;
		}
		float t = Time.realtimeSinceStartup;
		Debug.Log("Starting collider bake");
		IntVector2 startGrid = WorldToGridPos(VRHead.position);
		if (BakeCollider(startGrid))
		{
			yield return null;
		}
		int radius = 1;
		while (bakeFinished < bakeTotal)
		{
			for (int side = -1; side <= 1; side += 2)
			{
				for (int y2 = startGrid.y - radius; y2 <= startGrid.y + radius; y2++)
				{
					IntVector2 grid = new IntVector2(startGrid.x + side * radius, y2);
					if (BakeCollider(grid))
					{
						yield return null;
					}
				}
				for (int y2 = startGrid.x - (radius - 1); y2 <= startGrid.x + (radius - 1); y2++)
				{
					IntVector2 grid2 = new IntVector2(y2, startGrid.y + side * radius);
					if (BakeCollider(grid2))
					{
						yield return null;
					}
				}
			}
			radius++;
		}
		Debug.Log("Collider bake complete. Time: " + (Time.realtimeSinceStartup - t) + "s");
		colliderBakeComplete = true;
	}

	public bool BakeCollider(IntVector2 grid)
	{
		if (grid.x >= 0 && grid.x < gridSize && grid.y >= 0 && grid.y < gridSize && !colliderBakesComplete[grid.x, grid.y])
		{
			VTTerrainChunk vTTerrainChunk = chunks[grid.x, grid.y];
			if (vTTerrainChunk != null && vTTerrainChunk.lodObjects != null)
			{
				for (int i = 0; i < vTTerrainChunk.lodObjects.Length; i++)
				{
					if (terrainProfiles[i].colliders)
					{
						bool activeSelf = vTTerrainChunk.lodObjects[i].activeSelf;
						vTTerrainChunk.lodObjects[i].SetActive(value: true);
						MeshCollider collider = vTTerrainChunk.collider;
						collider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;
						collider.enabled = true;
						collider.sharedMesh = vTTerrainChunk.sharedMeshes[i];
						Vector3 localPosition = collider.transform.localPosition;
						collider.transform.position = Vector3.zero;
						collider.transform.localPosition = localPosition;
						vTTerrainChunk.lodObjects[i].SetActive(activeSelf);
						vTTerrainChunk.colliderBaked = true;
					}
				}
				colliderBakesComplete[grid.x, grid.y] = true;
				bakeFinished++;
				return true;
			}
		}
		return false;
	}

	public bool BakeColliderAtPosition(Vector3 worldPos)
	{
		return BakeCollider(ChunkGridAtPos(worldPos));
	}

	public void BakeCollidersAtPositionRadius(Vector3 worldPos, float radius)
	{
		BakeColliderAtPosition(worldPos);
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector = Quaternion.AngleAxis((float)i * 45f, Vector3.up) * (radius * Vector3.forward);
			BakeColliderAtPosition(worldPos + vector);
		}
	}

	public IntVector2 WorldToGridPos(Vector3 worldPos)
	{
		Vector3D vector3D = FloatingOrigin.accumOffset + worldPos;
		int x = (int)Math.Round(vector3D.x / (double)chunkSize);
		int y = (int)Math.Round(vector3D.z / (double)chunkSize);
		return new IntVector2(x, y);
	}

	public IntVector2 ChunkGridAtPos(Vector3 worldPos)
	{
		Vector3D vector3D = FloatingOrigin.accumOffset + worldPos;
		int x = (int)Math.Floor(vector3D.x / (double)chunkSize);
		int y = (int)Math.Floor(vector3D.z / (double)chunkSize);
		return new IntVector2(x, y);
	}

	public Vector3 SurfacePoint(Vector3 worldPoint)
	{
		IntVector2 grid = ChunkGridAtPos(worldPoint);
		VTTerrainChunk terrainChunk = GetTerrainChunk(grid);
		if (terrainChunk != null)
		{
			return terrainChunk.terrainMeshes[0].WorldProjectPointOnTerrain(worldPoint, terrainChunk.lodObjects[0].transform);
		}
		return worldPoint;
	}

	public bool IsChunkColliderEnabled(Vector3 position)
	{
		IntVector2 grid = ChunkGridAtPos(position);
		VTTerrainChunk terrainChunk = GetTerrainChunk(grid);
		if (terrainChunk != null && terrainChunk.colliderBaked && terrainChunk.collider.gameObject.activeSelf)
		{
			return true;
		}
		return false;
	}

	public Vector3 GetSurfacePos(Vector3 position)
	{
		float terrainAltitude = GetTerrainAltitude(position);
		position.y = WaterPhysics.instance.height + terrainAltitude;
		return position;
	}

	public float GetTerrainAltitude(Vector3 position)
	{
		IntVector2 grid = ChunkGridAtPos(position);
		VTTerrainChunk terrainChunk = GetTerrainChunk(grid);
		if (terrainChunk != null && terrainChunk.colliderBaked)
		{
			Vector3 vector = position;
			vector.y = WaterPhysics.instance.height;
			if (terrainChunk.collider.gameObject.activeSelf)
			{
				if (Physics.Linecast(vector + 10000f * Vector3.up, vector, out var hitInfo, 1))
				{
					return hitInfo.point.y - WaterPhysics.instance.height;
				}
				return 0f;
			}
		}
		return GetHeightmapAltitude(position);
	}

	public float GetHeightmapAltitude(Vector3 position)
	{
		Vector3D vector3D = VTMapManager.WorldToGlobalPoint(position);
		Vector2 vector = new Vector2((float)vector3D.x, (float)vector3D.z);
		double num = (float)gridSize * chunkSize;
		Vector2 vector2 = new Vector2((float)(vector3D.x / num), (float)(vector3D.z / num));
		float r = hmBdt.GetColorUV(vector2.x, vector2.y).r;
		float hmHeight = Mathf.Lerp(hm_minHeight, hm_maxHeight, r);
		float oobHeight;
		if (edgeMode == EdgeModes.Hills)
		{
			Vector2 vector3 = (float)oobHeightMap.width / (chunkSize * (float)oobProfile.repeatScale) * vector;
			oobHeight = oobHeightMap.GetColor(vector3.x, vector3.y).r * hm_maxHeight;
		}
		else
		{
			oobHeight = -80f;
		}
		return Mathf.Max(0f, VTTHeightMap.GetBlendedHeight(hmHeight, oobHeight, gridSize, chunkSize, hm_maxHeight, vector, edgeMode == EdgeModes.Coast, coastSide));
	}

	public float GetHeightmapAltitude(Vector3D gPos)
	{
		if (hmBdt == null)
		{
			return 0f;
		}
		Vector2 vector = new Vector2((float)gPos.x, (float)gPos.z);
		double num = (float)gridSize * chunkSize;
		Vector2 vector2 = new Vector2((float)(gPos.x / num), (float)(gPos.z / num));
		float r = hmBdt.GetColorUV(vector2.x, vector2.y).r;
		float hmHeight = Mathf.Lerp(hm_minHeight, hm_maxHeight, r);
		float oobHeight;
		if (edgeMode == EdgeModes.Hills)
		{
			Vector2 vector3 = (float)oobHeightMap.width / (chunkSize * (float)oobProfile.repeatScale) * vector;
			oobHeight = oobHeightMap.GetColor(vector3.x, vector3.y).r * hm_maxHeight;
		}
		else
		{
			oobHeight = -80f;
		}
		return Mathf.Max(0f, VTTHeightMap.GetBlendedHeight(hmHeight, oobHeight, gridSize, chunkSize, hm_maxHeight, vector, edgeMode == EdgeModes.Coast, coastSide));
	}

	public void SetChunkLOD(IntVector2 chunkGrid, int lod)
	{
		VTTerrainChunk terrainChunk = GetTerrainChunk(chunkGrid);
		if (terrainChunk != null)
		{
			for (int i = 0; i < terrainChunk.lodObjects.Length; i++)
			{
				terrainChunk.lodObjects[i].SetActive(i == lod);
			}
		}
	}

	private void PushJobQueue()
	{
		if (jobPushQueue.Count <= 0)
		{
			return;
		}
		lock (jobQueueLock)
		{
			while (jobPushQueue.Count > 0)
			{
				jobQueue.Enqueue(jobPushQueue.Dequeue());
			}
		}
	}

	public int GetJobQueueCount()
	{
		int result = 0;
		lock (jobQueueLock)
		{
			if (jobQueue != null)
			{
				return jobQueue.Count;
			}
			return result;
		}
	}

	public VTTerrainJob RequestJob()
	{
		VTTerrainJob result = null;
		lock (jobQueueLock)
		{
			if (jobQueue.Count > 0)
			{
				return jobQueue.Dequeue();
			}
			return result;
		}
	}

	public static Type GetJobType(VTMapTypes mapType)
	{
		return mapType switch
		{
			VTMapTypes.Archipelago => typeof(VTTArchipelago), 
			VTMapTypes.Mountain_Lakes => typeof(VTTMountains), 
			VTMapTypes.HeightMap => typeof(VTTHeightMap), 
			_ => null, 
		};
	}

	public Color GetTerrainColor(Vector3 pos)
	{
		IntVector2 grid = ChunkGridAtPos(pos);
		VTTerrainChunk terrainChunk = GetTerrainChunk(grid);
		if (terrainChunk != null)
		{
			VTTerrainMesh vTTerrainMesh = terrainChunk.terrainMeshes[0];
			Vector3 vector = terrainChunk.lodObjects[0].transform.InverseTransformPoint(pos);
			float num = float.MaxValue;
			Color result = Color.black;
			for (int i = 0; i < vTTerrainMesh.verts.Count; i++)
			{
				float sqrMagnitude = (vector - vTTerrainMesh.verts[i]).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = vTTerrainMesh.colors[i];
				}
			}
			return result;
		}
		return Color.white;
	}

	public Color GetTerrainColor(Vector3 pos, out Vector3 worldNormal)
	{
		IntVector2 grid = ChunkGridAtPos(pos);
		worldNormal = Vector3.up;
		VTTerrainChunk terrainChunk = GetTerrainChunk(grid);
		if (terrainChunk != null)
		{
			VTTerrainMesh vTTerrainMesh = terrainChunk.terrainMeshes[0];
			Vector3 vector = terrainChunk.lodObjects[0].transform.InverseTransformPoint(pos);
			float num = float.MaxValue;
			Color result = Color.black;
			for (int i = 0; i < vTTerrainMesh.verts.Count; i++)
			{
				float sqrMagnitude = (vector - vTTerrainMesh.verts[i]).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = vTTerrainMesh.colors[i];
					worldNormal = vTTerrainMesh.normals[i];
				}
			}
			worldNormal = terrainChunk.lodObjects[0].transform.TransformDirection(worldNormal);
			return result;
		}
		return Color.white;
	}

	private IEnumerator CreateOOB()
	{
		if (edgeMode == EdgeModes.Water)
		{
			yield break;
		}
		int rScale = oobProfile.repeatScale;
		oobPools = new ObjectPool[rScale, rScale];
		if (edgeMode == EdgeModes.Coast)
		{
			coastOOBPools = new ObjectPool[rScale, rScale];
		}
		VTTHeightMap oobJob = new VTTHeightMap();
		oobJob.vertsPerSide = 5;
		oobJob.Initialize(new VTTerrainMesh[1]
		{
			new VTTerrainMesh(oobProfile.mesh)
		});
		oobJob.biome = biome;
		oobJob.loadCities = false;
		oobJob.minHeight = 0f;
		oobJob.maxHeight = hm_maxHeight;
		oobJob.heightMap = oobHeightMap;
		oobJob.mapSize = oobProfile.repeatScale;
		oobJob.chunkSize = 3072f;
		oobJob.noiseModule = new FastNoise(noiseSeed);
		oobJob.edgeMode = VTTHeightMap.EdgeModes.None;
		oobJob.coastSide = coastSide;
		Material material = oobProfile.editorMaterial;
		if (biomeProfiles != null && biomeProfiles.Length != 0)
		{
			material = biomeProfiles[(int)biome].editorOOBMaterial;
		}
		Material mat = ((VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario) ? terrainMaterial : material);
		for (int x = 0; x < rScale; x++)
		{
			for (int y = 0; y < rScale; y++)
			{
				oobJob.gridPosition = new IntVector2(x, y);
				oobJob.DoJob();
				Mesh mesh = new Mesh();
				meshesToDestroyOnCleanup.Add(mesh);
				mesh.name = "OOB " + x + "," + y;
				oobJob.outputMeshes[0].ApplyToMesh(mesh);
				GameObject gameObject = new GameObject("OOB(" + x + "," + y + ")");
				gameObject.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
				gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
				gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
				gameObject.AddComponent<FloatingOriginTransform>();
				gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
				oobPools[x, y] = ObjectPool.CreateObjectPool(gameObject, 3, canGrow: true, destroyOnLoad: true);
				gameObject.SetActive(value: false);
				if (edgeMode == EdgeModes.Coast)
				{
					oobJob.coastalOOB = true;
					oobJob.DoJob();
					Mesh mesh2 = new Mesh();
					meshesToDestroyOnCleanup.Add(mesh2);
					mesh2.name = "Coastal OOB " + x + "," + y;
					oobJob.outputMeshes[0].ApplyToMesh(mesh2);
					GameObject gameObject2 = new GameObject("Coastal OOB(" + x + "," + y + ")");
					gameObject2.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
					gameObject2.AddComponent<MeshFilter>().sharedMesh = mesh2;
					gameObject2.AddComponent<MeshRenderer>().sharedMaterial = mat;
					gameObject2.AddComponent<FloatingOriginTransform>();
					gameObject2.AddComponent<MeshCollider>().sharedMesh = mesh2;
					coastOOBPools[x, y] = ObjectPool.CreateObjectPool(gameObject2, 3, canGrow: true, destroyOnLoad: true);
					gameObject2.SetActive(value: false);
					oobJob.coastalOOB = false;
				}
				if (loadItem != null)
				{
					loadItem.currentValue += 1f;
				}
				yield return null;
			}
		}
		oobDone = true;
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			StartCoroutine(OOBLODRoutine());
		}
		else
		{
			CreateEditorOOB();
		}
	}

	private void CreateEditorOOB()
	{
		int num = 3;
		for (int i = -num; i <= gridSize + num; i++)
		{
			for (int j = -num; j <= gridSize + num; j++)
			{
				if (i < 0 || i >= gridSize || j < 0 || j >= gridSize)
				{
					IntVector2 intVector = new IntVector2(i, j);
					GameObject oOBObject = GetOOBObject(intVector);
					if ((bool)oOBObject)
					{
						oOBObject.SetActive(value: true);
						oOBObject.transform.position = GridToWorldPos(intVector);
					}
				}
			}
		}
	}

	private IEnumerator OOBLODRoutine()
	{
		while (!playerTransform)
		{
			yield return null;
		}
		while (base.enabled)
		{
			while (!playerTransform)
			{
				yield return null;
			}
			IntVector2 playerPos = ChunkGridAtPos(playerTransform.position);
			int radius = terrainProfiles[0].lodRadius;
			for (int x = playerPos.x - radius; x <= playerPos.x + radius; x++)
			{
				for (int i = playerPos.y - radius; i <= playerPos.y + radius; i++)
				{
					if (x >= 0 && i >= 0 && x < gridSize && i < gridSize)
					{
						continue;
					}
					IntVector2 intVector = new IntVector2(x, i);
					if (!activeOOBObjects.ContainsKey(intVector))
					{
						GameObject oOBObject = GetOOBObject(intVector);
						if ((bool)oOBObject)
						{
							activeOOBObjects.Add(intVector, oOBObject);
							oOBObject.transform.position = GridToWorldPos(intVector);
							oOBObject.SetActive(value: true);
							StartCoroutine(OOBLifeRoutine(oOBObject, intVector, radius));
						}
					}
				}
				yield return null;
			}
			yield return null;
		}
	}

	private IEnumerator OOBLifeRoutine(GameObject obj, IntVector2 grid, int radius)
	{
		WaitForSeconds wait = new WaitForSeconds(2f);
		while (true)
		{
			if (!playerTransform)
			{
				yield return null;
				continue;
			}
			if (IntVector2.MaxOffset(ChunkGridAtPos(playerTransform.position), grid) > radius)
			{
				break;
			}
			yield return wait;
		}
		obj.SetActive(value: false);
		activeOOBObjects.Remove(grid);
	}

	private int Mod(int n, int l)
	{
		int num = n % l;
		if (num < 0)
		{
			num += l;
		}
		return num;
	}

	private GameObject GetOOBObject(IntVector2 gridPos)
	{
		int repeatScale = oobProfile.repeatScale;
		int num = (gridPos.x % repeatScale + repeatScale) % repeatScale;
		int num2 = (gridPos.y % repeatScale + repeatScale) % repeatScale;
		if (edgeMode == EdgeModes.Coast)
		{
			if (coastSide == CardinalDirections.North)
			{
				num2 = Mod(num2 + (repeatScale - (gridSize - repeatScale)), repeatScale);
				if (gridPos.y < gridSize - oobProfile.repeatScale)
				{
					return oobPools[num, num2].GetPooledObject();
				}
				if (gridPos.y < gridSize)
				{
					return coastOOBPools[num, num2].GetPooledObject();
				}
				return null;
			}
			if (coastSide == CardinalDirections.East)
			{
				num = Mod(num + (repeatScale - (gridSize - repeatScale)), repeatScale);
				if (gridPos.x < gridSize - oobProfile.repeatScale)
				{
					return oobPools[num, num2].GetPooledObject();
				}
				if (gridPos.x < gridSize)
				{
					return coastOOBPools[num, num2].GetPooledObject();
				}
				return null;
			}
			if (coastSide == CardinalDirections.South)
			{
				if (gridPos.y < 0)
				{
					return null;
				}
				if (gridPos.y < oobProfile.repeatScale)
				{
					return coastOOBPools[num, num2].GetPooledObject();
				}
				return oobPools[num, num2].GetPooledObject();
			}
			if (gridPos.x < 0)
			{
				return null;
			}
			if (gridPos.x < oobProfile.repeatScale)
			{
				return coastOOBPools[num, num2].GetPooledObject();
			}
			return oobPools[num, num2].GetPooledObject();
		}
		return oobPools[num, num2].GetPooledObject();
	}
}
