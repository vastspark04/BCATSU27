using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class VTMapCities : MonoBehaviour
{
	public class CityMeshDestroyer : MonoBehaviour
	{
		public Mesh mesh;

		private void OnDestroy()
		{
			if (mesh != null)
			{
				Object.Destroy(mesh);
			}
		}
	}

	public VTMapGenerator mapGenerator;

	public Texture2D noiseTexture;

	private const int PIXEL_LEVELS = 5;

	private List<GameObject>[] pixelPrefabs = new List<GameObject>[5];

	public static FlightSceneManager.FlightSceneLoadItem loadItem;

	private float loadValueMult = 0.05f;

	private Dictionary<IntVector2, Dictionary<IntVector2, CityBuilderPixel>> previewObjects = new Dictionary<IntVector2, Dictionary<IntVector2, CityBuilderPixel>>();

	private List<ObjectPool>[] previewPools = new List<ObjectPool>[5];

	private Dictionary<IntVector2, Transform> pixelParents = new Dictionary<IntVector2, Transform>();

	private Dictionary<IntVector2, List<CityBuilderPixel>> allFinalPixels = new Dictionary<IntVector2, List<CityBuilderPixel>>();

	private FastNoise noiseModule;

	private bool hasSetupLoadItem;

	private int processedCityPixels;

	private int totalCityPixels;

	private LODGroup[] lodGroupBuffer = new LODGroup[100];

	private List<LODGroup>[,] lodSubsections = new List<LODGroup>[2, 2];

	private Coroutine queuedActionRoutine;

	private Queue<UnityAction> queuedActions = new Queue<UnityAction>();

	public int maxQueuedActionsPerFrame = 15;

	public static VTMapCities instance { get; private set; }

	private bool isMapEditor => VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor;

	private void AddFinalPixel(IntVector2 chunkGrid, CityBuilderPixel pixel)
	{
		if (allFinalPixels.TryGetValue(chunkGrid, out var value))
		{
			value.Add(pixel);
			return;
		}
		value = new List<CityBuilderPixel>();
		value.Add(pixel);
		allFinalPixels.Add(chunkGrid, value);
	}

	public List<CityBuilderPixel> GetPixels(IntVector2 chunkGrid)
	{
		if (allFinalPixels.TryGetValue(chunkGrid, out var value))
		{
			return value;
		}
		return null;
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		mapGenerator.OnChunkGeneratedEarly += MapGenerator_OnChunkGenerated;
		mapGenerator.OnChunkRecalculatedEarly += MapGenerator_OnChunkRecalculated;
		LoadPixelPrefabs();
	}

	private void InsertionSortPixel(GameObject pixPrefabObj, List<CityBuilderPixel> list)
	{
		GameObject gameObject = Object.Instantiate(pixPrefabObj);
		gameObject.name = pixPrefabObj.name;
		Collider[] componentsInChildren = gameObject.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if ((bool)collider.gameObject.GetComponent<MeshRenderer>())
			{
				Object.Destroy(collider);
			}
			else
			{
				Object.Destroy(collider.gameObject);
			}
		}
		gameObject.gameObject.SetActive(value: false);
		CityBuilderPixel component = gameObject.GetComponent<CityBuilderPixel>();
		if (list.Count == 0)
		{
			list.Add(component);
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			if (component.gameObject.name.CompareTo(list[j].gameObject.name) <= 0)
			{
				list.Insert(j, component);
				return;
			}
		}
		list.Add(component);
	}

	private void LoadPixelPrefabs()
	{
		for (int i = 0; i < 5; i++)
		{
			pixelPrefabs[i] = new List<GameObject>();
		}
		List<CityBuilderPixel> list = new List<CityBuilderPixel>();
		GameObject[] array = Resources.LoadAll<GameObject>("VTMapEditor/CityPixels");
		foreach (GameObject pixPrefabObj in array)
		{
			InsertionSortPixel(pixPrefabObj, list);
		}
		foreach (CityBuilderPixel item in list)
		{
			pixelPrefabs[item.cityLevel].Add(item.gameObject);
		}
		if (VTMapManager.nextLaunchMode != VTMapManager.MapLaunchModes.MapEditor)
		{
			return;
		}
		for (int k = 0; k < 5; k++)
		{
			previewPools[k] = new List<ObjectPool>();
			foreach (GameObject item2 in pixelPrefabs[k])
			{
				previewPools[k].Add(ObjectPool.CreateObjectPool(item2, 5, canGrow: true, destroyOnLoad: true));
			}
		}
	}

	private void MapGenerator_OnChunkRecalculated(VTMapGenerator.VTTerrainChunk chunk)
	{
		ClearChunk(chunk);
		ProcessChunk(chunk);
	}

	private void MapGenerator_OnChunkGenerated(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (noiseModule == null)
		{
			noiseModule = new FastNoise(mapGenerator.noiseSeed);
		}
		ProcessChunk(chunk);
	}

	private void ClearChunk(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (pixelParents.TryGetValue(chunk.grid, out var value))
		{
			pixelParents.Remove(chunk.grid);
			Object.Destroy(value.gameObject);
		}
		if (!previewObjects.TryGetValue(chunk.grid, out var value2))
		{
			return;
		}
		foreach (CityBuilderPixel value3 in value2.Values)
		{
			value3.gameObject.SetActive(value: false);
			value3.transform.parent = base.transform;
		}
		value2.Clear();
		previewObjects.Remove(chunk.grid);
	}

	public void UpdatePreview(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (pixelParents.ContainsKey(chunk.grid))
		{
			ClearChunk(chunk);
		}
		ProcessChunk(chunk, preview: true);
	}

	private int GetTotalCityPixels()
	{
		int num = 0;
		BDTexture hmBdt = mapGenerator.hmBdt;
		for (int i = 0; i < hmBdt.width - 1; i++)
		{
			for (int j = 0; j < hmBdt.height - 1; j++)
			{
				BDColor pixel = hmBdt.GetPixel(i, j);
				BDColor pixel2 = hmBdt.GetPixel(i + 1, j);
				BDColor pixel3 = hmBdt.GetPixel(i + 1, j + 1);
				BDColor pixel4 = hmBdt.GetPixel(i, j + 1);
				if (pixel.g > 0.1f && pixel2.g > 0.1f && pixel3.g > 0.1f && pixel4.g > 0.1f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void ProcessChunk(VTMapGenerator.VTTerrainChunk chunk, bool preview = false)
	{
		if (chunk.grid == new IntVector2(3, 14))
		{
			IntVector2 grid = chunk.grid;
			Debug.Log("Processing city chunk " + grid.ToString());
		}
		if (allFinalPixels.ContainsKey(chunk.grid))
		{
			allFinalPixels.Remove(chunk.grid);
		}
		int num = 20;
		_ = mapGenerator.chunkSize / (float)num;
		List<CityBuilderPixel> list = new List<CityBuilderPixel>();
		for (int i = 0; i < chunk.terrainMeshes[0].vertCount; i++)
		{
			Vector2 vector = chunk.terrainMeshes[0].uvs[i];
			if (!(vector.x >= 0f) || !(vector.y >= 0f) || !(vector.x < 0.99f) || !(vector.y < 0.99f))
			{
				continue;
			}
			Vector3 vector2 = chunk.terrainMeshes[0].verts[i];
			IntVector2 intVector = VTTerrainTextureConverter.VertToPixel(vector2, chunk.grid.x, chunk.grid.y, mapGenerator.chunkSize, mapGenerator.gridSize, num);
			CityBuilderPixel value = null;
			if (preview && previewObjects.TryGetValue(chunk.grid, out var value2))
			{
				value2.TryGetValue(intVector, out value);
			}
			if (intVector.x >= mapGenerator.hmBdt.width - 1 || intVector.y >= mapGenerator.hmBdt.height - 1)
			{
				continue;
			}
			BDColor pixel = mapGenerator.hmBdt.GetPixel(intVector.x, intVector.y);
			BDColor pixel2 = mapGenerator.hmBdt.GetPixel(intVector.x + 1, intVector.y);
			BDColor pixel3 = mapGenerator.hmBdt.GetPixel(intVector.x + 1, intVector.y + 1);
			BDColor pixel4 = mapGenerator.hmBdt.GetPixel(intVector.x, intVector.y + 1);
			if (pixel.g > 0.1f && pixel2.g > 0.1f && pixel3.g > 0.1f && pixel4.g > 0.1f)
			{
				int value3 = Mathf.FloorToInt((pixel.g - 0.2f) / 0.8f * 5f);
				value3 = Mathf.Clamp(value3, 0, 4);
				if ((bool)value && value.cityLevel == value3)
				{
					continue;
				}
				int value4 = Mathf.FloorToInt(noiseTexture.GetPixel(intVector.x % noiseTexture.width, intVector.y % noiseTexture.height).r * (float)pixelPrefabs[value3].Count);
				value4 = Mathf.Clamp(value4, 0, pixelPrefabs[value3].Count - 1);
				GameObject gameObject;
				if (preview)
				{
					gameObject = previewPools[value3][value4].GetPooledObject();
					gameObject.SetActive(value: true);
				}
				else
				{
					gameObject = Object.Instantiate(pixelPrefabs[value3][value4]);
				}
				gameObject.transform.rotation = Quaternion.identity;
				CityBuilderPixel component = gameObject.GetComponent<CityBuilderPixel>();
				component.SetRotation(intVector);
				list.Add(component);
				if (preview)
				{
					if ((bool)value)
					{
						value.gameObject.SetActive(value: false);
						value.transform.parent = base.transform;
						previewObjects[chunk.grid][intVector] = component;
					}
					else
					{
						if (!previewObjects.ContainsKey(chunk.grid))
						{
							previewObjects.Add(chunk.grid, new Dictionary<IntVector2, CityBuilderPixel>());
						}
						previewObjects[chunk.grid].Add(intVector, component);
					}
					gameObject.transform.parent = chunk.lodObjects[0].transform;
				}
				else
				{
					if (pixelParents.TryGetValue(chunk.grid, out var value5))
					{
						gameObject.transform.parent = value5;
					}
					else
					{
						Transform transform = new GameObject("CityParent").transform;
						transform.parent = chunk.lodObjects[0].transform;
						transform.localPosition = Vector3.zero;
						transform.localRotation = Quaternion.identity;
						gameObject.transform.parent = transform;
						pixelParents.Add(chunk.grid, transform);
					}
					AddFinalPixel(chunk.grid, component);
				}
				gameObject.transform.localPosition = vector2;
			}
			else if (preview && (bool)value)
			{
				value.gameObject.SetActive(value: false);
				value.transform.parent = base.transform;
				previewObjects[chunk.grid].Remove(intVector);
			}
		}
		if (preview)
		{
			foreach (CityBuilderPixel item in list)
			{
				item.PlaceObjectsToSurface(chunk.lodObjects[0].GetComponent<MeshCollider>());
			}
			return;
		}
		if (list.Count > 0)
		{
			if (!hasSetupLoadItem)
			{
				hasSetupLoadItem = true;
				loadItem = FlightSceneManager.instance.AddLoadItem();
				totalCityPixels = GetTotalCityPixels();
				loadItem.maxValue = (float)totalCityPixels * loadValueMult;
			}
			StartCoroutine(PlaceWhenReady(list, chunk.lodObjects[0], chunk.grid));
		}
	}

	private IEnumerator PlaceWhenReady(List<CityBuilderPixel> pixels, GameObject lodObject, IntVector2 chunkGrid)
	{
		if (chunkGrid == new IntVector2(3, 14))
		{
			IntVector2 intVector = chunkGrid;
			Debug.Log("Beginning city PlaceWhenReady on " + intVector.ToString());
		}
		mapGenerator.BakeCollider(chunkGrid);
		MeshCollider component = lodObject.GetComponent<MeshCollider>();
		foreach (CityBuilderPixel pixel in pixels)
		{
			pixel.PlaceObjectsToSurface(component);
		}
		yield return null;
		bool ready = false;
		while (!ready)
		{
			ready = true;
			for (int i = 0; i < pixels.Count && ready; i++)
			{
				CityBuilderPixel cityBuilderPixel = pixels[i];
				if (!cityBuilderPixel)
				{
					IntVector2 intVector = chunkGrid;
					Debug.Log("Chunk " + intVector.ToString() + " was missing a pixel. Aborting.");
					yield break;
				}
				if (!cityBuilderPixel.IsPlacementComplete())
				{
					ready = false;
				}
			}
			yield return null;
		}
		if (chunkGrid == new IntVector2(3, 14))
		{
			IntVector2 intVector = chunkGrid;
			Debug.Log("Ready to combine city chunk " + intVector.ToString());
		}
		Transform value = null;
		if (pixelParents.TryGetValue(chunkGrid, out value))
		{
			ClearLODSubsections();
			float num = mapGenerator.chunkSize / 2f;
			float num2 = num / 2f;
			for (int j = 0; j < pixels.Count; j++)
			{
				Vector3 localPosition = pixels[j].transform.localPosition;
				LODGroup component2 = pixels[j].GetComponent<LODGroup>();
				if (localPosition.x > 0f - num)
				{
					if (localPosition.y < num)
					{
						lodSubsections[0, 0].Add(component2);
					}
					else
					{
						lodSubsections[0, 1].Add(component2);
					}
				}
				else if (localPosition.y < num)
				{
					lodSubsections[1, 0].Add(component2);
				}
				else
				{
					lodSubsections[1, 1].Add(component2);
				}
			}
			new List<Collider>();
			for (int k = 0; k < 2; k++)
			{
				for (int l = 0; l < 2; l++)
				{
					if (lodSubsections[k, l].Count == 0)
					{
						continue;
					}
					GameObject gameObject = new GameObject($"Subsection {k},{l}");
					gameObject.transform.parent = value;
					gameObject.transform.localRotation = Quaternion.identity;
					gameObject.transform.localPosition = Vector3.zero;
					MeshCombiner2 meshCombiner = gameObject.gameObject.AddComponent<MeshCombiner2>();
					meshCombiner.destroyNonColliders = true;
					LOD[] array = new LOD[2];
					array[0].screenRelativeTransitionHeight = 0.99f;
					array[1].screenRelativeTransitionHeight = 0.07f;
					for (int m = 0; m < lodGroupBuffer.Length; m++)
					{
						if (m < lodSubsections[k, l].Count)
						{
							lodGroupBuffer[m] = lodSubsections[k, l][m];
							lodSubsections[k, l][m].transform.parent = gameObject.transform;
						}
						else
						{
							lodGroupBuffer[m] = null;
						}
					}
					meshCombiner.CombineMeshesLOD(array, gameObject.gameObject, gameObject.gameObject, lodGroupBuffer);
					Vector3 vector = value.position + new Vector3(num * (float)k, 0f, num * (float)l) + new Vector3(num2, 0f, num2);
					Bounds bounds = new Bounds(vector, new Vector3(num, num, num));
					Vector3 center = Vector3.zero;
					if (array[1].renderers.Length != 0)
					{
						MeshFilter component3 = array[1].renderers[0].GetComponent<MeshFilter>();
						component3.sharedMesh.RecalculateBounds();
						center = component3.transform.InverseTransformPoint(vector);
						center.z = component3.sharedMesh.bounds.center.z;
					}
					else
					{
						Debug.LogError("City subsection has LOD with no renderers", gameObject);
					}
					for (int n = 0; n < array.Length; n++)
					{
						Renderer[] renderers = array[n].renderers;
						foreach (Renderer renderer in renderers)
						{
							bounds.center = center;
							Mesh sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
							sharedMesh.bounds = bounds;
							renderer.gameObject.AddComponent<CityMeshDestroyer>().mesh = sharedMesh;
							if (n > 0)
							{
								renderer.shadowCastingMode = ShadowCastingMode.Off;
							}
						}
					}
					LODGroup lODGroup = gameObject.gameObject.AddComponent<LODGroup>();
					lODGroup.SetLODs(array);
					lODGroup.RecalculateBounds();
					Object.Destroy(meshCombiner);
					if (!isMapEditor)
					{
						Collider[] componentsInChildren = gameObject.GetComponentsInChildren<Collider>();
						for (int num3 = 0; num3 < componentsInChildren.Length; num3++)
						{
							Object.Destroy(componentsInChildren[num3]);
						}
						Renderer[] renderers = array[1].renderers;
						for (int num3 = 0; num3 < renderers.Length; num3++)
						{
							renderers[num3].gameObject.AddComponent<MeshCollider>();
						}
					}
				}
			}
		}
		else
		{
			Debug.LogErrorFormat("Chunk {0} was missing a city pixel parent object.", chunkGrid);
		}
		if (chunkGrid == new IntVector2(3, 14))
		{
			IntVector2 intVector = chunkGrid;
			Debug.Log("Finished PlaceWhenReady on city chunk " + intVector.ToString());
		}
		if (hasSetupLoadItem && ((loadItem != null) & !loadItem.done))
		{
			int count = pixels.Count;
			processedCityPixels += count;
			loadItem.currentValue = (float)processedCityPixels * loadValueMult;
			if (processedCityPixels == totalCityPixels)
			{
				loadItem.done = true;
			}
		}
	}

	private void ClearLODSubsections()
	{
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < 2; j++)
			{
				if (lodSubsections[i, j] != null)
				{
					lodSubsections[i, j].Clear();
				}
				else
				{
					lodSubsections[i, j] = new List<LODGroup>();
				}
			}
		}
	}

	public void AddQueuedAction(UnityAction a)
	{
		queuedActions.Enqueue(a);
		if (queuedActionRoutine == null)
		{
			queuedActionRoutine = StartCoroutine(QueuedActionsRoutine());
		}
	}

	private IEnumerator QueuedActionsRoutine()
	{
		yield return null;
		int num = 0;
		while (queuedActions.Count > 0)
		{
			queuedActions.Dequeue()();
			num++;
			if (num >= maxQueuedActionsPerFrame)
			{
				yield return null;
				num = 0;
			}
		}
		queuedActionRoutine = null;
	}
}
