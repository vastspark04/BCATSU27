using System.Collections;
using UnityEngine;

public class VTMapCustom : VTMap
{
	public class AsyncSaveOp
	{
		public Texture2D texture;

		public bool done;

		public float progress;

		private AsyncSaveBehaviour behaviour;

		public AsyncSaveOp(AsyncSaveBehaviour b)
		{
			behaviour = b;
		}

		public void Cancel()
		{
			if ((bool)behaviour)
			{
				behaviour.Cancel();
			}
		}
	}

	public class AsyncSaveBehaviour : MonoBehaviour
	{
		private const int maxPixPerFrame = 1910;

		private AsyncSaveOp saveOp;

		private VTMapCustom map;

		private Coroutine saveRoutine;

		private Texture2D tex;

		public void Begin(AsyncSaveOp op, VTMapCustom map)
		{
			saveOp = op;
			this.map = map;
			saveRoutine = StartCoroutine(SaveRoutine());
		}

		public void Cancel()
		{
			if (saveRoutine != null)
			{
				StopCoroutine(saveRoutine);
			}
			if (tex != null)
			{
				Object.Destroy(tex);
			}
			Object.Destroy(base.gameObject);
		}

		private IEnumerator SaveRoutine()
		{
			VTMapGenerator.VTMapTypes mapType = map.mapType;
			int mapSize = map.mapSize;
			ConfigNode terrainSettings = map.terrainSettings;
			string seed = map.seed;
			float hm_maxHeight = map.hm_maxHeight;
			VTTerrainJob job = VTMapGenerator.GetJobInstanceFromType(mapType);
			job.chunkSize = 3072f;
			job.mapSize = mapSize;
			job.gridPosition = new IntVector2(0, 0);
			job.noiseModule = new FastNoise(VTMapGenerator.StringSeedToInt(seed));
			job.ApplySettings(terrainSettings);
			if (job is VTTHeightMap)
			{
				((VTTHeightMap)job).heightMap = VTMapGenerator.fetch.hmBdt;
			}
			int texSize = 20 * mapSize + 1;
			tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, mipChain: false, linear: true);
			float progressMax = texSize * texSize;
			float progressCurr = 0f;
			int currPix = 0;
			for (int x = 0; x < texSize; x++)
			{
				for (int y = 0; y < texSize; y++)
				{
					Vector2 uv = new Vector2(x, y) / texSize;
					uv *= (float)mapSize;
					float height = job.GetHeight(uv);
					height /= hm_maxHeight;
					tex.SetPixel(x, y, new Color(height, 0f, 0f, 1f));
					progressCurr += 1f;
					float progress = progressCurr / progressMax;
					saveOp.progress = progress;
					currPix++;
					if (currPix >= 1910)
					{
						currPix = 0;
						yield return null;
					}
				}
			}
			tex.Apply();
			saveOp.texture = tex;
			saveOp.done = true;
			Object.Destroy(base.gameObject);
		}
	}

	public VTMapGenerator.VTMapTypes mapType;

	public string seed;

	public ConfigNode terrainSettings;

	public VTMapPrefabs prefabs;

	public float hm_maxHeight = 6000f;

	public Texture2D heightMap;

	public Texture2D[] splitHeightmaps;

	private ConfigNode mapConfig;

	public BezierRoadSystem roadSystem;

	public MapGenBiome.Biomes biome;

	public VTMapGenerator.EdgeModes edgeMode;

	public CardinalDirections coastSide;

	public string mapDir;

	public bool isSteamWorkshopMap;

	public bool isSWPreviewOnly;

	public void LoadFlightSceneObjects()
	{
		roadSystem = new BezierRoadSystem();
		prefabs = new VTMapPrefabs();
		if (mapConfig != null)
		{
			prefabs.LoadFromMapConfig(mapConfig);
			roadSystem.LoadFromConfigNode(mapConfig, mapSize);
		}
	}

	public ConfigNode SaveToConfigNode()
	{
		ConfigNode configNode = new ConfigNode("VTMapCustom");
		configNode.SetValue("mapID", mapID);
		configNode.SetValue("mapName", mapName);
		configNode.SetValue("mapDescription", mapDescription);
		configNode.SetValue("mapType", mapType);
		configNode.SetValue("edgeMode", edgeMode);
		if (edgeMode == VTMapGenerator.EdgeModes.Coast)
		{
			configNode.SetValue("coastSide", coastSide);
		}
		configNode.SetValue("biome", biome);
		if (!string.IsNullOrEmpty(seed))
		{
			configNode.SetValue("seed", seed);
		}
		configNode.SetValue("mapSize", mapSize);
		configNode.AddNode(terrainSettings);
		if (prefabs != null)
		{
			prefabs.SaveToMapConfig(configNode);
		}
		if (roadSystem != null)
		{
			roadSystem.SaveToConfigNode(configNode);
		}
		return configNode;
	}

	public void LoadFromConfigNode(ConfigNode mapNode, string mapDir)
	{
		this.mapDir = mapDir;
		mapConfig = mapNode;
		ConfigNodeUtils.TryParseValue(mapNode, "mapID", ref mapID);
		ConfigNodeUtils.TryParseValue(mapNode, "mapName", ref mapName);
		ConfigNodeUtils.TryParseValue(mapNode, "mapDescription", ref mapDescription);
		ConfigNodeUtils.TryParseValue(mapNode, "mapType", ref mapType);
		ConfigNodeUtils.TryParseValue(mapNode, "seed", ref seed);
		ConfigNodeUtils.TryParseValue(mapNode, "mapSize", ref mapSize);
		ConfigNodeUtils.TryParseValue(mapNode, "edgeMode", ref edgeMode);
		ConfigNodeUtils.TryParseValue(mapNode, "biome", ref biome);
		if (edgeMode == VTMapGenerator.EdgeModes.Coast)
		{
			ConfigNodeUtils.TryParseValue(mapNode, "coastSide", ref coastSide);
		}
		terrainSettings = mapNode.GetNode("TerrainSettings");
	}

	public AsyncSaveOp SaveToHeightMap()
	{
		AsyncSaveBehaviour asyncSaveBehaviour = new GameObject().AddComponent<AsyncSaveBehaviour>();
		AsyncSaveOp asyncSaveOp = new AsyncSaveOp(asyncSaveBehaviour)
		{
			done = false,
			progress = 0f,
			texture = null
		};
		asyncSaveBehaviour.Begin(asyncSaveOp, this);
		return asyncSaveOp;
	}
}
