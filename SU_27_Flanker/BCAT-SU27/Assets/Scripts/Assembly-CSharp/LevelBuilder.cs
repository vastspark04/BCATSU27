using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelBuilder : MonoBehaviour
{
	public delegate void GridEvent(IntVector2 grid);

	[Serializable]
	public struct SpecialChunk : IEquatable<SpecialChunk>
	{
		public IntVector2 grid;

		public int chunkIndex;

		public int rotation;

		public bool Equals(SpecialChunk other)
		{
			return grid.Equals(other.grid);
		}

		public SpecialChunk(SpecialChunk other)
		{
			grid = other.grid;
			chunkIndex = other.chunkIndex;
			rotation = other.rotation;
		}
	}

	public class LevelChunk : MonoBehaviour
	{
		private LevelBuilder lb;

		private IntVector2 grid;

		public void Init(LevelBuilder lb, IntVector2 grid)
		{
			this.lb = lb;
			this.grid = grid;
			StartCoroutine(Check());
		}

		private IEnumerator Check()
		{
			while (base.enabled)
			{
				if (IntVector2.MaxOffset(grid, lb.currentPlayerGrid) > lb.loadRange + 2)
				{
					lb.DegenerateGrid(grid);
					break;
				}
				yield return new WaitForSeconds(2f);
			}
		}
	}

	public struct ChunkMapInfo
	{
		private IntVector2 _grid;

		private int _prefabIdx;

		private int _rotation;

		private bool _special;

		public IntVector2 grid => _grid;

		public int prefabIdx => _prefabIdx;

		public int rotation => _rotation;

		public bool special => _special;

		public ChunkMapInfo(IntVector2 grid, int prefabIdx, int rotation, bool special = false)
		{
			_grid = grid;
			_prefabIdx = prefabIdx;
			_rotation = rotation;
			_special = special;
		}
	}

	public IntVector2 currentPlayerGrid;

	public GameObject playerPrefab;

	public Texture2D chunkMap;

	public bool legacyMapMode = true;

	public float tileSize;

	public GameObject[] chunkPrefabs;

	public int loadRange = 5;

	public Transform playerTransform;

	public Transform playerSpawnTransform;

	private Vector3D originOffset;

	private Dictionary<IntVector2, GameObject> activeChunks = new Dictionary<IntVector2, GameObject>();

	private ChunkMapInfo[][] mapInfo;

	private ObjectPool[] chunkPools;

	public IntVector2 playerStartPos;

	public List<GameObject> specialChunkPrefabs;

	public List<SpecialChunk> specialChunks;

	[Space]
	public UIMap uiMap;

	private ObjectPool[] specialChunkPools;

	private FlightSceneManager.FlightReadyContingent readyContingent;

	public ChunkLODTextureCamera chunkLodCamera;

	public GameObject chunkLodQuadTemplate;

	private bool[,] gridsActive;

	public static LevelBuilder fetch { get; private set; }

	public int width { get; private set; }

	public int height { get; private set; }

	public bool playerReady { get; private set; }

	public bool mapReady { get; private set; }

	public event GridEvent OnSpawnGrid;

	public event GridEvent OnDespawnGrid;

	private void Awake()
	{
		fetch = this;
		playerReady = false;
		mapReady = false;
		FloatingOrigin.instance.OnOriginShift += OnOriginShift;
		if ((bool)PilotSaveManager.currentCampaign && !PilotSaveManager.currentCampaign.isCustomScenarios && (bool)PilotSaveManager.currentVehicle)
		{
			playerPrefab = PilotSaveManager.currentVehicle.vehiclePrefab;
		}
		if ((bool)playerPrefab)
		{
			playerTransform = UnityEngine.Object.Instantiate(playerPrefab).transform;
			playerTransform.gameObject.SetActive(value: true);
		}
		readyContingent = new FlightSceneManager.FlightReadyContingent();
		FlightSceneManager.instance.AddReadyContingent(readyContingent);
	}

	private void Start()
	{
		if ((bool)playerTransform)
		{
			FlightSceneManager.instance.playerActor = playerTransform.GetComponent<Actor>();
		}
		StartCoroutine(GenerationRoutine());
		StartCoroutine(SpawnPlayerRoutine());
		GameObject gameObject = GameObject.FindWithTag("LevelCreator");
		if ((bool)gameObject)
		{
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	private void Update()
	{
		UpdateCurrentPlayerGrid();
	}

	public void GenerateData()
	{
		GeneratePools();
		GenerateMapInfo();
	}

	private void OnOriginShift(Vector3 offset)
	{
		originOffset -= offset;
	}

	private IEnumerator SpawnPlayerRoutine()
	{
		if (!playerTransform || !playerPrefab)
		{
			playerReady = true;
			readyContingent.ready = true;
			yield break;
		}
		if (PilotSaveManager.currentCampaign != null && PilotSaveManager.currentCampaign.isCustomScenarios)
		{
			playerReady = true;
			readyContingent.ready = true;
			yield break;
		}
		BlackoutEffect.instance.SetGAccum(100f);
		Rigidbody playerRb = playerTransform.gameObject.GetComponent<Rigidbody>();
		if ((bool)playerRb)
		{
			playerRb.isKinematic = true;
			while (!mapReady)
			{
				BlackoutEffect.instance.SetGAccum(100f);
				playerRb.velocity = Vector3.zero;
				yield return new WaitForFixedUpdate();
			}
			yield return null;
			if (ScenarioSelectorUI.scenarioChosen && (bool)ScenarioSetup.instance && (bool)ScenarioSetup.instance.currentScenarioConfig.spawnTransform)
			{
				playerSpawnTransform = ScenarioSetup.instance.currentScenarioConfig.spawnTransform;
			}
			if ((bool)playerSpawnTransform)
			{
				playerTransform.position = playerSpawnTransform.position;
			}
			else
			{
				Vector3 position = playerTransform.position;
				Vector3 vector = GridToPosition(playerStartPos);
				Vector3 vector2 = vector;
				Debug.Log("SpawnPos: " + vector2.ToString());
				position.x = vector.x;
				position.z = vector.z;
				_ = playerTransform.rotation;
			}
			yield return null;
			if ((bool)playerSpawnTransform)
			{
				_ = playerSpawnTransform.GetComponentInParent<GridPlatoon>().spawnInGrid;
				while (!mapReady)
				{
					playerTransform.position = playerSpawnTransform.position;
					BlackoutEffect.instance.SetGAccum(100f);
					yield return null;
				}
				Vector3 position = playerSpawnTransform.position + 10f * Vector3.up;
				Quaternion rotation = playerSpawnTransform.rotation;
				playerTransform.position = position;
				playerTransform.rotation = rotation;
			}
			PlayerVehicleSetup component = playerTransform.GetComponent<PlayerVehicleSetup>();
			component.SetupForFlight();
			component.LandVehicle(playerSpawnTransform);
			BlackoutEffect.instance.SetGAccum(0f);
			playerReady = true;
			readyContingent.ready = true;
		}
		else
		{
			Debug.LogError("Player transform has no attached rigidbody.");
		}
	}

	private void GenerateMapInfo()
	{
		if (!chunkMap)
		{
			GenerateRandomMap();
			return;
		}
		RefreshMapInfo();
		gridsActive = new bool[width, height];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				gridsActive[i, j] = false;
			}
		}
	}

	public void RefreshMapInfo()
	{
		width = chunkMap.width;
		height = chunkMap.height;
		mapInfo = new ChunkMapInfo[width][];
		for (int i = 0; i < width; i++)
		{
			mapInfo[i] = new ChunkMapInfo[height];
			for (int j = 0; j < height; j++)
			{
				IntVector2 grid = new IntVector2(i, j);
				if (specialChunks.ContainsGrid(grid, out var index))
				{
					mapInfo[i][j] = new ChunkMapInfo(grid, specialChunks[index].chunkIndex, specialChunks[index].rotation, special: true);
				}
				else
				{
					mapInfo[i][j] = PixelToMapInfo(grid);
				}
			}
		}
	}

	private void GenerateRandomMap()
	{
		int num = 50;
		mapInfo = new ChunkMapInfo[num][];
		int maxExclusive = chunkPrefabs.Length;
		for (int i = 0; i < num; i++)
		{
			mapInfo[i] = new ChunkMapInfo[num];
			for (int j = 0; j < num; j++)
			{
				int prefabIdx = UnityEngine.Random.Range(0, maxExclusive);
				int rotation = UnityEngine.Random.Range(0, 4);
				ChunkMapInfo chunkMapInfo = new ChunkMapInfo(new IntVector2(i, j), prefabIdx, rotation);
				mapInfo[i][j] = chunkMapInfo;
			}
		}
	}

	private ChunkMapInfo PixelToMapInfo(IntVector2 grid)
	{
		if (legacyMapMode)
		{
			return LegacyPixelToMapInfo(grid);
		}
		Color pixel = chunkMap.GetPixel(grid.x, grid.y);
		int prefabIdx = Mathf.RoundToInt((pixel.r + pixel.g + pixel.b) * 128f);
		int rotation = Mathf.RoundToInt(pixel.a * 3f);
		return new ChunkMapInfo(grid, prefabIdx, rotation);
	}

	private ChunkMapInfo LegacyPixelToMapInfo(IntVector2 grid)
	{
		Color pixel = chunkMap.GetPixel(grid.x, grid.y);
		int num = chunkPrefabs.Length;
		int prefabIdx = Mathf.RoundToInt(pixel.r * (float)(num - 1));
		int rotation = Mathf.RoundToInt(pixel.a * 3f);
		bool special = pixel.b > 0.1f;
		return new ChunkMapInfo(grid, prefabIdx, rotation, special);
	}

	public Color MapInfoToPixel(ChunkMapInfo info)
	{
		if (legacyMapMode)
		{
			return LegacyMapInfoToPixel(info);
		}
		Color result = default(Color);
		float num = info.prefabIdx;
		num /= 128f;
		result.r = Mathf.Clamp01(num);
		num -= result.r;
		result.g = Mathf.Clamp01(num);
		num -= result.g;
		result.b = Mathf.Clamp01(num);
		result.a = (float)info.rotation / 3f;
		return result;
	}

	public void ConvertLegacyToNewMap()
	{
		legacyMapMode = false;
		for (int i = 0; i < chunkMap.width; i++)
		{
			for (int j = 0; j < chunkMap.height; j++)
			{
				IntVector2 grid = new IntVector2(i, j);
				ChunkMapInfo info = LegacyPixelToMapInfo(grid);
				chunkMap.SetPixel(i, j, MapInfoToPixel(info));
			}
		}
		chunkMap.Apply();
	}

	public Color LegacyMapInfoToPixel(ChunkMapInfo info)
	{
		Color black = Color.black;
		if (info.special)
		{
			black.b = 1f;
		}
		black.r = 1f / ((float)chunkPrefabs.Length - 1f) * (float)info.prefabIdx;
		black.g = black.r;
		black.a = (float)info.rotation / 3f;
		return black;
	}

	private void GeneratePools()
	{
		GameObject gameObject = GameObject.FindWithTag("Platoons");
		if ((bool)gameObject)
		{
			gameObject.SetActive(value: false);
		}
		chunkPools = new ObjectPool[chunkPrefabs.Length];
		for (int i = 0; i < chunkPools.Length; i++)
		{
			MeshCombiner2 meshCombiner = chunkPrefabs[i].AddComponent<MeshCombiner2>();
			meshCombiner.destroyNonColliders = true;
			meshCombiner.CombineMeshes();
			UnityEngine.Object.Destroy(meshCombiner);
			chunkPrefabs[i].AddComponent<LevelChunk>();
			if ((bool)chunkLodCamera && i > 3)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(chunkLodQuadTemplate, chunkPrefabs[i].transform);
				chunkPrefabs[i].SetActive(value: true);
				chunkLodCamera.transform.parent = chunkPrefabs[i].transform;
				chunkLodCamera.transform.localPosition = new Vector3(0f, 500f, 0f);
				chunkLodCamera.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
				chunkLodCamera.target = gameObject2.GetComponent<MeshRenderer>();
				chunkLodCamera.CreateTexture();
				chunkLodCamera.transform.parent = null;
				chunkPrefabs[i].SetActive(value: false);
				gameObject2.SetActive(value: true);
				gameObject2.transform.localPosition = Vector3.zero;
				gameObject2.transform.localEulerAngles = new Vector3(90f, 0f, 0f);
				LODBase lodBase = chunkPrefabs[i].AddComponent<LODBase>();
				LODObject lODObject = chunkPrefabs[i].AddComponent<LODObject>();
				lODObject.lodBase = lodBase;
				lODObject.levels = new List<LODObject.LODObjectLevel>();
				LODObject.LODObjectLevel lODObjectLevel = new LODObject.LODObjectLevel();
				lODObjectLevel.maxDist = 23000f;
				lODObjectLevel.gameObjects = new GameObject[1] { chunkPrefabs[i].transform.Find("Meshes").gameObject };
				lODObject.levels.Add(lODObjectLevel);
				LODObject.LODObjectLevel lODObjectLevel2 = new LODObject.LODObjectLevel();
				lODObjectLevel2.maxDist = 100000f;
				lODObjectLevel2.gameObjects = new GameObject[1] { gameObject2 };
				lODObject.levels.Add(lODObjectLevel2);
			}
			chunkPools[i] = ObjectPool.CreateObjectPool(chunkPrefabs[i], 20, canGrow: true, destroyOnLoad: true);
		}
		specialChunkPools = new ObjectPool[specialChunkPrefabs.Count];
		for (int j = 0; j < specialChunkPools.Length; j++)
		{
			specialChunkPrefabs[j].AddComponent<LevelChunk>();
			specialChunkPools[j] = ObjectPool.CreateObjectPool(specialChunkPrefabs[j], 1, canGrow: true, destroyOnLoad: true);
		}
		if ((bool)gameObject)
		{
			gameObject.SetActive(value: true);
		}
	}

	private IEnumerator GenerationRoutine()
	{
		bool initialGeneration = true;
		yield return null;
		GenerateData();
		while (base.enabled)
		{
			for (int x = Mathf.Max(0, currentPlayerGrid.x - loadRange); x <= Mathf.Min(width, currentPlayerGrid.x + loadRange); x++)
			{
				for (int i = Mathf.Max(0, currentPlayerGrid.y - loadRange); i <= Mathf.Min(height, currentPlayerGrid.y + loadRange); i++)
				{
					IntVector2 grid = new IntVector2(x, i);
					GenerateGrid(grid);
				}
				if (!initialGeneration)
				{
					yield return null;
				}
			}
			mapReady = true;
			initialGeneration = false;
			yield return null;
		}
	}

	private void UpdateCurrentPlayerGrid()
	{
		if (playerTransform != null)
		{
			currentPlayerGrid = PlayerGrid();
		}
	}

	private void GenerateGrid(IntVector2 grid)
	{
		int x = grid.x;
		int y = grid.y;
		if (x >= 0 && x < width && y >= 0 && y < height && !gridsActive[x, y])
		{
			GameObject gameObject = null;
			ChunkMapInfo chunkMapInfo = mapInfo[x][y];
			if (chunkMapInfo.special)
			{
				gameObject = specialChunkPools[chunkMapInfo.prefabIdx].GetPooledObject();
			}
			else if (chunkMapInfo.prefabIdx != 0)
			{
				gameObject = chunkPools[chunkMapInfo.prefabIdx].GetPooledObject();
			}
			if (chunkMapInfo.prefabIdx != 0)
			{
				gameObject.SetActive(value: true);
				gameObject.transform.parent = base.transform;
				gameObject.transform.localRotation = Quaternion.Euler(0f, 90 * chunkMapInfo.rotation, 0f);
				Vector3 localPosition = new Vector3((float)x * tileSize, 0f, (float)y * tileSize);
				gameObject.transform.localPosition = localPosition;
				activeChunks.Add(grid, gameObject);
				gameObject.GetComponent<LevelChunk>().Init(this, grid);
			}
			gridsActive[x, y] = true;
			if (this.OnSpawnGrid != null)
			{
				this.OnSpawnGrid(grid);
			}
		}
	}

	public GameObject GenerateGridEditor(IntVector2 grid, IntVector2 editCenter, GameObject creatorObject)
	{
		int x = grid.x;
		int y = grid.y;
		ChunkMapInfo chunkMapInfo;
		GameObject gameObject;
		if (x >= 0 && x < width && y >= 0 && y < height)
		{
			chunkMapInfo = mapInfo[x][y];
			gameObject = ((!chunkMapInfo.special) ? UnityEngine.Object.Instantiate(chunkPrefabs[chunkMapInfo.prefabIdx]) : UnityEngine.Object.Instantiate(specialChunkPrefabs[chunkMapInfo.prefabIdx]));
		}
		else
		{
			chunkMapInfo = new ChunkMapInfo(grid, 0, 0);
			gameObject = UnityEngine.Object.Instantiate(chunkPrefabs[0]);
		}
		gameObject.SetActive(value: true);
		gameObject.transform.parent = creatorObject.transform;
		gameObject.transform.localRotation = Quaternion.Euler(0f, 90 * chunkMapInfo.rotation, 0f);
		Vector3 localPosition = new Vector3((float)(x - editCenter.x) * tileSize, 0f, (float)(y - editCenter.y) * tileSize);
		gameObject.transform.localPosition = localPosition;
		return gameObject;
	}

	private void DegenerateGrid(IntVector2 grid)
	{
		if (IsGridActive(grid))
		{
			gridsActive[grid.x, grid.y] = false;
			activeChunks[grid].SetActive(value: false);
			activeChunks[grid].transform.parent = null;
			activeChunks.Remove(grid);
			if (this.OnDespawnGrid != null)
			{
				this.OnDespawnGrid(grid);
			}
		}
	}

	private IntVector2 PlayerGrid()
	{
		return PositionToGrid(playerTransform.position);
	}

	public IntVector2 PositionToGrid(Vector3 position)
	{
		Vector3 toVector = (originOffset + position).toVector3;
		int x = Mathf.RoundToInt(toVector.x / tileSize);
		int y = Mathf.RoundToInt(toVector.z / tileSize);
		return new IntVector2(x, y);
	}

	public Vector3 GridToPosition(IntVector2 grid)
	{
		Vector3 result = default(Vector3);
		result.x = (float)grid.x * tileSize - originOffset.toVector3.x;
		result.y = 0f - originOffset.toVector3.y;
		result.z = (float)grid.y * tileSize - originOffset.toVector3.z;
		return result;
	}

	public bool IsGridActive(IntVector2 grid)
	{
		if (grid.x >= 0 && grid.y >= 0 && grid.x < width && grid.y < width)
		{
			return gridsActive[grid.x, grid.y];
		}
		return false;
	}

	public bool IsGridActive(Vector3 position)
	{
		return IsGridActive(PositionToGrid(position));
	}

	public ChunkMapInfo GetMapInfo(IntVector2 grid)
	{
		if (grid.x < 0 || grid.x >= width || grid.y < 0 || grid.y >= height)
		{
			return new ChunkMapInfo(grid, 0, 0);
		}
		return mapInfo[grid.x][grid.y];
	}

	public Vector3D PlayerPosition3D()
	{
		return originOffset + playerTransform.position;
	}

	public Vector3D WorldPosition3D(Vector3 position)
	{
		return originOffset + position;
	}

	public Vector3 Position3DToWorld(Vector3D position)
	{
		return (position - originOffset).toVector3;
	}
}
