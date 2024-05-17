using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

public class VTTMapTrees : MonoBehaviour
{
	public class TreeChunk
	{
		public List<Vector3> treePoints;

		public GameObject treeObj;

		public List<float> treeScales;

		public List<Vector3> colliderSizes;

		public IntVector2 subGrid;

		public List<TreeRemover> treeRemovers = new List<TreeRemover>();

		public TreeLOD[] treeLODGroups;

		public int completedLODs;
	}

	public class ChunkJobData
	{
		public IntVector2 groupGrid;

		public IntVector2 subGrid;

		public List<Vector3> treePoints;

		public List<float> treeScales;

		public List<TreeRemover> treeRemovers;
	}

	public struct TreeRemover
	{
		public Vector3 point;

		public float radius;

		public TreeRemover(Vector3 point, float radius)
		{
			this.point = point;
			this.radius = radius;
		}
	}

	public class TreeLOD
	{
		public GameObject gameObject;

		public MeshRenderer mr;

		public MeshFilter mf;
	}

	public class TreeChunkGroup
	{
		public List<TreeChunk> chunks;
	}

	[Serializable]
	public class TreeProfile
	{
		public Mesh mesh;

		public Material material;

		public float lodRadius;

		public bool billboard;

		public Vector3 colliderSize;

		[Range(0f, 1f)]
		public float lodHeight;

		[Range(0f, 1f)]
		public float lodFade;
	}

	private class TreeWorker
	{
		public VTTMapTrees master;

		private Thread workThread;

		private bool enabled;

		private int maxJobsPerCycle = 128;

		private object enablerLock = new object();

		private Queue<TreeJob> jobQueue = new Queue<TreeJob>();

		private Queue<TreeJob> finishedJobs = new Queue<TreeJob>();

		public void Begin()
		{
			lock (enablerLock)
			{
				if (enabled)
				{
					return;
				}
				enabled = true;
			}
			workThread = new Thread(WorkLoop);
			workThread.Priority = System.Threading.ThreadPriority.BelowNormal;
			workThread.Start();
		}

		public void Stop()
		{
			lock (enablerLock)
			{
				if (enabled)
				{
					if (jobQueue != null)
					{
						while (jobQueue.Count > 0)
						{
							TreeJob treeJob = jobQueue.Dequeue();
							if (treeJob == null)
							{
								continue;
							}
							if (treeJob.masterMesh != null)
							{
								treeJob.masterMesh.Clear();
								treeJob.masterMesh = null;
							}
							if (treeJob.treeLODMeshes == null)
							{
								continue;
							}
							for (int i = 0; i < treeJob.treeLODMeshes.Length; i++)
							{
								if (treeJob.treeLODMeshes[i] != null)
								{
									treeJob.treeLODMeshes[i].Clear();
									treeJob.treeLODMeshes = null;
								}
							}
							treeJob.treeLODMeshes = null;
						}
						jobQueue = null;
					}
					if (finishedJobs != null)
					{
						while (finishedJobs.Count > 0)
						{
							TreeJob treeJob2 = finishedJobs.Dequeue();
							if (treeJob2 == null)
							{
								continue;
							}
							if (treeJob2.masterMesh != null)
							{
								treeJob2.masterMesh.Clear();
								treeJob2.masterMesh = null;
							}
							if (treeJob2.treeLODMeshes == null)
							{
								continue;
							}
							for (int j = 0; j < treeJob2.treeLODMeshes.Length; j++)
							{
								if (treeJob2.treeLODMeshes[j] != null)
								{
									treeJob2.treeLODMeshes[j].Clear();
									treeJob2.treeLODMeshes = null;
								}
							}
							treeJob2.treeLODMeshes = null;
						}
						finishedJobs = null;
					}
				}
				enabled = false;
			}
		}

		private void WorkLoop()
		{
			int num = 0;
			while (jobQueue != null)
			{
				master.RequestJobs(jobQueue);
				while (jobQueue.Count > 0)
				{
					lock (enablerLock)
					{
						if (!enabled)
						{
							return;
						}
					}
					if (jobQueue == null)
					{
						return;
					}
					TreeJob treeJob = jobQueue.Dequeue();
					try
					{
						treeJob.DoJob();
					}
					catch (Exception ex)
					{
						Debug.LogError(ex.ToString());
					}
					if (finishedJobs == null)
					{
						return;
					}
					finishedJobs.Enqueue(treeJob);
					num++;
					if (num >= maxJobsPerCycle)
					{
						num = 0;
						if (finishedJobs == null)
						{
							return;
						}
						if (finishedJobs.Count > 0)
						{
							master.SendFinishedJobs(finishedJobs);
						}
						Thread.Sleep(100);
					}
				}
				lock (enablerLock)
				{
					if (!enabled)
					{
						break;
					}
				}
				if (finishedJobs == null)
				{
					break;
				}
				if (finishedJobs.Count > 0)
				{
					master.SendFinishedJobs(finishedJobs);
				}
				Thread.Sleep(100);
				lock (enablerLock)
				{
					if (!enabled)
					{
						break;
					}
				}
			}
		}
	}

	public class TreeJob
	{
		public ChunkJobData chunk;

		public VTTerrainMesh masterMesh;

		public VTTerrainMesh[] treeLODMeshes;

		public Vector3[] treeColliderSizes;

		public bool billboard;

		public int targetLOD;

		public int maxVertCount;

		private int currVertCount;

		private int currTriCount;

		public void DoJob()
		{
			int vtxOffset = 0;
			int triOffset = 0;
			int num = 0;
			currVertCount = 0;
			currTriCount = 0;
			if (masterMesh != null)
			{
				_ = masterMesh.vertCount;
			}
			if (masterMesh == null)
			{
				masterMesh = new VTTerrainMesh();
			}
			for (int i = 0; i < chunk.treePoints.Count; i++)
			{
				Vector3 vector = chunk.treePoints[i];
				if (num + maxVertCount >= 64999)
				{
					continue;
				}
				num += maxVertCount;
				if (i >= chunk.treeScales.Count)
				{
					chunk.treeScales.Add(chunk.treeScales[i - 1]);
				}
				float scale = chunk.treeScales[i];
				int num2 = i % treeLODMeshes.Length;
				bool flag = false;
				if (chunk.treeRemovers != null)
				{
					for (int j = 0; j < chunk.treeRemovers.Count; j++)
					{
						if (flag)
						{
							break;
						}
						TreeRemover treeRemover = chunk.treeRemovers[j];
						Vector3 point = treeRemover.point;
						point.y = vector.y;
						if ((point - vector).sqrMagnitude < treeRemover.radius * treeRemover.radius)
						{
							flag = true;
						}
					}
				}
				if (!flag)
				{
					CreateTree(treeLODMeshes[num2], masterMesh, vector, billboard, scale, ref vtxOffset, ref triOffset);
				}
			}
			if (currVertCount < masterMesh.verts.Count)
			{
				int count = masterMesh.verts.Count - currVertCount;
				masterMesh.verts.RemoveRange(currVertCount, count);
				masterMesh.normals.RemoveRange(currVertCount, count);
				masterMesh.uvs.RemoveRange(currVertCount, count);
				masterMesh.tangents.RemoveRange(currVertCount, count);
			}
			if (currTriCount < masterMesh.triangles.Count)
			{
				int count2 = masterMesh.triangles.Count - currTriCount;
				masterMesh.triangles.RemoveRange(currTriCount, count2);
			}
		}

		private void CreateTree(VTTerrainMesh treeMesh, VTTerrainMesh masterMesh, Vector3 position, bool billboard, float scale, ref int vtxOffset, ref int triOffset)
		{
			for (int i = 0; i < treeMesh.vertCount; i++)
			{
				Vector3 vector = treeMesh.verts[i];
				Vector3 vector2;
				Vector3 vector3;
				if (!billboard)
				{
					Quaternion quaternion = Quaternion.AngleAxis(Mathf.LerpUnclamped(0f, 900f, scale), Vector3.up);
					vector2 = quaternion * (scale * vector) + position;
					vector3 = quaternion * treeMesh.normals[i];
				}
				else
				{
					vector2 = scale * vector + position;
					vector3 = treeMesh.normals[i];
				}
				if (billboard)
				{
					Vector3 vector4 = new Vector3(0.15f * vector.x, 0.2f * vector.y + 4f, 0.15f * vector.z);
					vector3 = (vector3 + vector4).normalized;
				}
				Vector3 vector5 = vector2 - position;
				Vector4 vector6 = new Vector4(vector5.x, vector5.y, vector5.z, billboard ? 1 : 0);
				int num = i + vtxOffset;
				if (num < masterMesh.verts.Count)
				{
					masterMesh.verts[num] = vector2;
					masterMesh.uvs[num] = treeMesh.uvs[i];
					masterMesh.normals[num] = vector3;
					masterMesh.tangents[num] = vector6;
				}
				else
				{
					masterMesh.verts.Add(vector2);
					masterMesh.uvs.Add(treeMesh.uvs[i]);
					masterMesh.normals.Add(vector3);
					masterMesh.tangents.Add(vector6);
				}
				currVertCount++;
			}
			for (int j = 0; j < treeMesh.triangles.Count; j++)
			{
				int num2 = j + triOffset;
				if (num2 < masterMesh.triangles.Count)
				{
					masterMesh.triangles[num2] = treeMesh.triangles[j] + vtxOffset;
				}
				else
				{
					masterMesh.triangles.Add(treeMesh.triangles[j] + vtxOffset);
				}
				currTriCount++;
			}
			triOffset += treeMesh.triangles.Count;
			vtxOffset += treeMesh.vertCount;
		}
	}

	public VTMapGenerator mapGenerator;

	private float maxTreesPerTri = 10f;

	public TreeProfile[] treeProfiles;

	private VTTerrainMesh[][] lodVTMeshes;

	private Vector3[] colliderSizes;

	public MinMax treeScale = new MinMax(1.5f, 2.5f);

	private int subdivisions = 3;

	private TreeChunkGroup[,] treeChunks;

	private int gridSize;

	private float chunkSize;

	private FastNoise scaleNoiseModule;

	private Stack<TreeJob> jobPool = new Stack<TreeJob>();

	private int preloadRadius = 7;

	private List<IntVector2> preloadedChunks = new List<IntVector2>();

	private float treeChunkWidth;

	[Header("Aku Trees")]
	public List<AkuTreeData> akuTreeDatas;

	public Transform akuGridStartTf;

	public int akuGridSize;

	public int akuMaxTreesPerTri = 2;

	public int akuSubdivs = 8;

	public int akuPreloadRad = 4;

	private bool initialSetupComplete;

	private bool useColliders = true;

	private List<CapsuleCollider> colliderPool;

	private float treeThresh = 0.75f;

	private int maxVertCount = 1;

	private int akuChunkSize = -1;

	private Stack<Mesh> meshPool = new Stack<Mesh>();

	private List<Mesh> meshesToCleanOnDestroy = new List<Mesh>();

	private int meshPoolJobsToProcess;

	private Queue<TreeJob> unfinishedJobs = new Queue<TreeJob>();

	private object unfinishedJobLock = new object();

	private Queue<TreeJob> finishedJobs = new Queue<TreeJob>();

	private object finishedJobLock = new object();

	private TreeWorker worker;

	public static VTTMapTrees fetch { get; private set; }

	private void Awake()
	{
		fetch = this;
	}

	public TreeChunkGroup GetTreeGroup(IntVector2 grid)
	{
		return treeChunks[grid.x, grid.y];
	}

	public TreeChunk GetChunk(Vector3 worldPosition)
	{
		IntVector2 intVector = VTMapGenerator.fetch.ChunkGridAtPos(worldPosition);
		if (intVector.x >= 0 && intVector.y >= 0 && intVector.x < treeChunks.GetLength(0) && intVector.y < treeChunks.GetLength(1))
		{
			TreeChunkGroup treeChunkGroup = treeChunks[intVector.x, intVector.y];
			if (treeChunkGroup != null)
			{
				Vector3 vector = worldPosition - VTMapGenerator.fetch.GridToWorldPos(intVector);
				float num = chunkSize / (float)subdivisions;
				int x = Mathf.FloorToInt(vector.x / num);
				int y = Mathf.FloorToInt(vector.z / num);
				IntVector2 intVector2 = new IntVector2(x, y);
				foreach (TreeChunk chunk in treeChunkGroup.chunks)
				{
					if (chunk.subGrid == intVector2)
					{
						return chunk;
					}
				}
				return null;
			}
			return null;
		}
		IntVector2 intVector3 = intVector;
		Debug.LogWarning("Tried to get a tree chunk out of bounds: " + intVector3.ToString() + " gridSize: " + treeChunks.GetLength(0));
		return null;
	}

	public void GetChunks(List<TreeChunk> chunkList, Vector3 worldPosition, float radius)
	{
		chunkList.Clear();
		TreeChunk chunk = GetChunk(worldPosition);
		if (chunk != null)
		{
			chunkList.Add(chunk);
		}
		for (int i = 0; i < 4; i++)
		{
			Vector3 worldPosition2 = worldPosition + Quaternion.AngleAxis(90 * i, Vector3.up) * new Vector3(0f, 0f, radius);
			TreeChunk chunk2 = GetChunk(worldPosition2);
			if (chunk2 != null && !chunkList.Contains(chunk2))
			{
				chunkList.Add(chunk2);
			}
		}
	}

	public TreeChunk GetChunk(IntVector2 groupGrid, IntVector2 subGrid)
	{
		TreeChunkGroup treeChunkGroup = treeChunks[groupGrid.x, groupGrid.y];
		if (treeChunkGroup != null)
		{
			foreach (TreeChunk chunk in treeChunkGroup.chunks)
			{
				if (chunk.subGrid == subGrid)
				{
					return chunk;
				}
			}
		}
		return null;
	}

	private void Start()
	{
		scaleNoiseModule = new FastNoise();
		scaleNoiseModule.SetNoiseType(FastNoise.NoiseType.WhiteNoise);
		if ((bool)mapGenerator)
		{
			mapGenerator.OnChunkGenerated += OnChunkComplete;
			mapGenerator.OnChunkRecalculated += OnChunkRecalculated;
			chunkSize = mapGenerator.chunkSize;
		}
		else if (akuTreeDatas.Count > 0 && akuGridStartTf != null)
		{
			lodVTMeshes = new VTTerrainMesh[treeProfiles.Length][];
			colliderSizes = new Vector3[1] { treeProfiles[0].colliderSize };
			for (int i = 0; i < lodVTMeshes.Length; i++)
			{
				lodVTMeshes[i] = new VTTerrainMesh[1];
				lodVTMeshes[i][0] = new VTTerrainMesh(treeProfiles[i].mesh);
			}
			gridSize = akuGridSize;
			subdivisions = akuSubdivs;
			preloadRadius = akuPreloadRad;
			treeChunks = new TreeChunkGroup[gridSize, gridSize];
			foreach (AkuTreeData akuTreeData in akuTreeDatas)
			{
				CreateChunkForAkuTrees(akuTreeData);
			}
		}
		SetupWorker();
		StartCoroutine(MeshPoolTreesRoutine());
	}

	private void OnChunkComplete(VTMapGenerator.VTTerrainChunk chunk)
	{
		if (!initialSetupComplete)
		{
			gridSize = mapGenerator.gridSize;
			treeChunkWidth = mapGenerator.chunkSize / (float)subdivisions;
			treeChunks = new TreeChunkGroup[gridSize, gridSize];
			MapGenBiome.BiomeTreeProfile treeProfile = mapGenerator.biomeProfiles[(int)mapGenerator.biome].treeProfile;
			maxTreesPerTri = treeProfile.treesPerTri;
			treeScale = treeProfile.treeScale;
			colliderSizes = treeProfile.colliderSizes;
			lodVTMeshes = new VTTerrainMesh[treeProfiles.Length][];
			for (int i = 0; i < lodVTMeshes.Length; i++)
			{
				Mesh[] array = ((i == 0) ? treeProfile.meshes : treeProfile.lowPolyMeshes);
				lodVTMeshes[i] = new VTTerrainMesh[array.Length];
				for (int j = 0; j < lodVTMeshes[i].Length; j++)
				{
					maxVertCount = Mathf.Max(maxVertCount, array[j].vertexCount);
					lodVTMeshes[i][j] = new VTTerrainMesh(array[j]);
				}
				if (i == 0)
				{
					treeProfiles[i].material = treeProfile.treeMaterial;
				}
				else
				{
					treeProfiles[i].material = treeProfile.billboardTreeMaterial;
				}
			}
			switch (mapGenerator.biome)
			{
			case MapGenBiome.Biomes.Boreal:
				treeThresh = 0.55f;
				break;
			case MapGenBiome.Biomes.Desert:
				treeThresh = 0.15f;
				break;
			case MapGenBiome.Biomes.Arctic:
				treeThresh = 0.75f;
				break;
			case MapGenBiome.Biomes.Tropical:
				treeThresh = 0.55f;
				break;
			}
			initialSetupComplete = true;
		}
		List<CityBuilderPixel> cityPixels = null;
		if ((bool)VTMapCities.instance)
		{
			cityPixels = VTMapCities.instance.GetPixels(chunk.grid);
		}
		scaleNoiseModule.SetSeed(mapGenerator.noiseSeed);
		float num = chunkSize / (float)subdivisions;
		float num2 = num / 2f;
		TreeChunkGroup treeChunkGroup = new TreeChunkGroup();
		treeChunkGroup.chunks = new List<TreeChunk>();
		treeChunks[chunk.grid.x, chunk.grid.y] = treeChunkGroup;
		for (int k = 0; k < subdivisions; k++)
		{
			for (int l = 0; l < subdivisions; l++)
			{
				Vector3 center = new Vector3(num * (float)k + num2, 0f, num * (float)l + num2);
				Bounds bounds = new Bounds(center, num * Vector3.one);
				GameObject gameObject = new GameObject("Trees");
				gameObject.transform.parent = chunk.gridTransform;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				TreeChunk treeChunk = new TreeChunk();
				treeChunk.treePoints = TreePopulator.GenerateTreePoints(chunk.terrainMeshes[0], maxTreesPerTri, chunk.lodObjects[0].transform, gameObject.transform, bounds, treeThresh);
				treeChunk.subGrid = new IntVector2(k, l);
				CollectTreePointFromCities(gameObject, cityPixels, bounds, chunk, treeChunk);
				if (treeChunk.treePoints.Count > 0)
				{
					treeChunk.treeObj = gameObject;
					treeChunk.treeLODGroups = new TreeLOD[treeProfiles.Length];
					LODGroup lODGroup = gameObject.AddComponent<LODGroup>();
					LOD[] array2 = new LOD[treeProfiles.Length];
					for (int m = 0; m < treeProfiles.Length; m++)
					{
						GameObject gameObject2 = new GameObject("LOD " + m);
						gameObject2.transform.parent = gameObject.transform;
						gameObject2.transform.localPosition = Vector3.zero;
						MeshFilter mf = gameObject2.AddComponent<MeshFilter>();
						MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
						meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
						meshRenderer.lightProbeUsage = LightProbeUsage.Off;
						if (m > 0)
						{
							meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
						}
						meshRenderer.sharedMaterial = treeProfiles[m].material;
						array2[m] = new LOD(treeProfiles[m].lodHeight, new Renderer[1] { meshRenderer });
						array2[m].fadeTransitionWidth = treeProfiles[m].lodFade;
						TreeLOD treeLOD = new TreeLOD();
						treeLOD.gameObject = gameObject2;
						treeLOD.mf = mf;
						treeLOD.mr = meshRenderer;
						treeChunk.treeLODGroups[m] = treeLOD;
					}
					lODGroup.SetLODs(array2);
					lODGroup.fadeMode = LODFadeMode.CrossFade;
					treeChunk.colliderSizes = new List<Vector3>();
					treeChunk.treeScales = new List<float>();
					for (int n = 0; n < treeChunk.treePoints.Count; n++)
					{
						Vector3 vector = chunk.gridTransform.TransformPoint(treeChunk.treePoints[n]);
						float num3 = Mathf.Lerp(treeScale.min, treeScale.max, ((float)scaleNoiseModule.GetNoise(vector.x, vector.z) + 1f) / 2f);
						treeChunk.treeScales.Add(num3);
						int num4 = n % colliderSizes.Length;
						Vector3 item = colliderSizes[num4] * num3;
						treeChunk.colliderSizes.Add(item);
					}
					treeChunk.treeObj.SetActive(value: false);
					treeChunkGroup.chunks.Add(treeChunk);
				}
				else
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
		}
	}

	private void CollectTreePointFromCities(GameObject treeObj, List<CityBuilderPixel> cityPixels, Bounds bounds, VTMapGenerator.VTTerrainChunk chunk, TreeChunk treeChunk)
	{
		if (cityPixels == null)
		{
			return;
		}
		foreach (CityBuilderPixel cityPixel in cityPixels)
		{
			foreach (Vector3 treePosition in cityPixel.treePositions)
			{
				Vector3 vector = treeObj.transform.InverseTransformPoint(cityPixel.rotationTransform.TransformPoint(treePosition));
				Vector3 point = vector;
				point.y = 0f;
				if (bounds.Contains(point))
				{
					Vector3 position = treeObj.transform.TransformPoint(vector);
					Vector3 localPosition = chunk.lodObjects[0].transform.InverseTransformPoint(position);
					localPosition = chunk.terrainMeshes[0].ProjectPointOnTerrain(localPosition);
					vector = treeObj.transform.InverseTransformPoint(chunk.lodObjects[0].transform.TransformPoint(localPosition));
					treeChunk.treePoints.Add(vector);
				}
			}
		}
	}

	private void OnChunkRecalculated(VTMapGenerator.VTTerrainChunk chunk)
	{
		TreeChunkGroup treeChunkGroup = treeChunks[chunk.grid.x, chunk.grid.y];
		if (preloadedChunks.Contains(chunk.grid))
		{
			DespawnMeshPoolChunk(chunk.grid);
		}
		List<CityBuilderPixel> cityPixels = null;
		if ((bool)VTMapCities.instance)
		{
			cityPixels = VTMapCities.instance.GetPixels(chunk.grid);
		}
		float num = chunk.generator.chunkSize / (float)subdivisions;
		float num2 = num / 2f;
		foreach (TreeChunk chunk2 in treeChunkGroup.chunks)
		{
			Vector3 center = new Vector3(num * (float)chunk2.subGrid.x + num2, 0f, num * (float)chunk2.subGrid.y + num2);
			Bounds bounds = new Bounds(center, num * Vector3.one);
			chunk2.treePoints = TreePopulator.GenerateTreePoints(chunk.terrainMeshes[0], maxTreesPerTri, chunk.lodObjects[0].transform, chunk2.treeObj.transform, bounds);
			CollectTreePointFromCities(chunk2.treeObj, cityPixels, bounds, chunk, chunk2);
			chunk2.colliderSizes = new List<Vector3>();
			chunk2.treeScales = new List<float>();
			for (int i = 0; i < chunk2.treePoints.Count; i++)
			{
				Vector3 vector = chunk.gridTransform.TransformPoint(chunk2.treePoints[i]);
				float num3 = Mathf.Lerp(treeScale.min, treeScale.max, ((float)scaleNoiseModule.GetNoise(vector.x, vector.z) + 1f) / 2f);
				chunk2.treeScales.Add(num3);
				int num4 = i % colliderSizes.Length;
				Vector3 item = colliderSizes[num4] * num3;
				chunk2.colliderSizes.Add(item);
			}
		}
	}

	private void CreateChunkForAkuTrees(AkuTreeData treeData)
	{
		if (akuChunkSize < 0)
		{
			akuChunkSize = Mathf.RoundToInt(treeData.GetComponent<MeshFilter>().sharedMesh.bounds.size.x);
			treeChunkWidth = (float)akuChunkSize / (float)subdivisions;
		}
		IntVector2 intVector = WorldToGridPos(treeData.transform.position);
		TreeChunkGroup treeChunkGroup = new TreeChunkGroup();
		treeChunkGroup.chunks = new List<TreeChunk>();
		treeChunks[intVector.x, intVector.y] = treeChunkGroup;
		float num = akuChunkSize;
		Vector3 vector = new Vector3((0f - num) / 2f, 0f, (0f - num) / 2f);
		float num2 = num / (float)subdivisions;
		for (int i = 0; i < subdivisions; i++)
		{
			for (int j = 0; j < subdivisions; j++)
			{
				Bounds bounds = default(Bounds);
				bounds.center = vector + new Vector3((float)i * num2 + num2 / 2f, 0f, (float)j * num2 + num2 / 2f);
				bounds.size = new Vector3(num2, 1f, num2);
				GameObject gameObject = new GameObject("Trees");
				gameObject.transform.parent = treeData.transform;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.rotation = Quaternion.identity;
				TreeChunk treeChunk = new TreeChunk();
				treeChunk.treePoints = TreePopulator.GenerateTreePoints(treeData, akuMaxTreesPerTri, treeData.transform, gameObject.transform, bounds);
				treeChunk.subGrid = new IntVector2(i, j);
				if (treeChunk.treePoints.Count > 0)
				{
					treeChunk.treeObj = gameObject;
					treeChunk.treeLODGroups = new TreeLOD[treeProfiles.Length];
					LODGroup lODGroup = gameObject.AddComponent<LODGroup>();
					LOD[] array = new LOD[treeProfiles.Length];
					for (int k = 0; k < treeProfiles.Length; k++)
					{
						GameObject gameObject2 = new GameObject("LOD " + k);
						gameObject2.transform.parent = gameObject.transform;
						gameObject2.transform.localPosition = Vector3.zero;
						MeshFilter mf = gameObject2.AddComponent<MeshFilter>();
						MeshRenderer meshRenderer = gameObject2.AddComponent<MeshRenderer>();
						meshRenderer.sharedMaterial = treeProfiles[k].material;
						array[k] = new LOD(treeProfiles[k].lodHeight, new Renderer[1] { meshRenderer });
						array[k].fadeTransitionWidth = treeProfiles[k].lodFade;
						TreeLOD treeLOD = new TreeLOD();
						treeLOD.gameObject = gameObject2;
						treeLOD.mf = mf;
						treeLOD.mr = meshRenderer;
						treeChunk.treeLODGroups[k] = treeLOD;
					}
					lODGroup.SetLODs(array);
					lODGroup.fadeMode = LODFadeMode.CrossFade;
					treeChunk.treeScales = new List<float>();
					treeChunk.colliderSizes = new List<Vector3>();
					for (int l = 0; l < treeChunk.treePoints.Count; l++)
					{
						Vector3 vector2 = treeData.transform.TransformPoint(treeChunk.treePoints[l]);
						float num3 = Mathf.Lerp(treeScale.min, treeScale.max, ((float)scaleNoiseModule.GetNoise(vector2.x, vector2.z) + 1f) / 2f);
						treeChunk.treeScales.Add(num3);
						int num4 = l % colliderSizes.Length;
						Vector3 item = colliderSizes[num4] * num3;
						treeChunk.colliderSizes.Add(item);
					}
					treeChunk.treeObj.SetActive(value: false);
					treeChunkGroup.chunks.Add(treeChunk);
				}
				else
				{
					UnityEngine.Object.Destroy(gameObject);
				}
			}
		}
	}

	private void CreateMeshPool()
	{
		int num = 1 + 2 * preloadRadius * (2 * preloadRadius);
		int num2 = subdivisions * subdivisions * num * treeProfiles.Length;
		for (int i = 0; i < num2; i++)
		{
			Mesh item = new Mesh();
			meshesToCleanOnDestroy.Add(item);
			meshPool.Push(item);
		}
	}

	private void OnDestroy()
	{
		DestroyMeshes();
		if (worker != null)
		{
			worker.Stop();
			worker = null;
		}
	}

	private void DestroyMeshes()
	{
		if (meshesToCleanOnDestroy != null)
		{
			foreach (Mesh item in meshesToCleanOnDestroy)
			{
				if ((bool)item)
				{
					UnityEngine.Object.Destroy(item);
				}
			}
			meshesToCleanOnDestroy.Clear();
		}
		if (lodVTMeshes == null)
		{
			return;
		}
		for (int i = 0; i < lodVTMeshes.Length; i++)
		{
			if (lodVTMeshes[i] == null)
			{
				continue;
			}
			for (int j = 0; j < lodVTMeshes[i].Length; j++)
			{
				if (lodVTMeshes[i][j] != null)
				{
					lodVTMeshes[i][j].Clear();
				}
				lodVTMeshes[i][j] = null;
			}
		}
		lodVTMeshes = null;
	}

	private IntVector2 WorldToGridPos(Vector3 worldPos)
	{
		if ((bool)mapGenerator)
		{
			return mapGenerator.WorldToGridPos(worldPos);
		}
		Vector3 vector = worldPos - akuGridStartTf.position;
		int x = Mathf.RoundToInt(vector.x / (float)akuChunkSize);
		int y = Mathf.RoundToInt(vector.z / (float)akuChunkSize);
		return new IntVector2(x, y);
	}

	private IEnumerator MeshPoolTreesRoutine()
	{
		CreateMeshPool();
		int num = 1024;
		for (int i = 0; i < num; i++)
		{
			TreeJob treeJob = new TreeJob();
			treeJob.maxVertCount = treeProfiles[0].mesh.vertexCount;
			jobPool.Push(treeJob);
		}
		if ((bool)mapGenerator)
		{
			while (!mapGenerator.HasFinishedInitialGeneration())
			{
				yield return null;
			}
		}
		while (!VRHead.instance)
		{
			yield return null;
		}
		StartCoroutine(MeshPoolProcessJobsRoutine());
		yield return null;
		IntVector2 lastPlayerPos = WorldToGridPos(VRHead.position);
		if (lastPlayerPos.x >= 0 && lastPlayerPos.x < gridSize && lastPlayerPos.y >= 0 && lastPlayerPos.y < gridSize && treeChunks[lastPlayerPos.x, lastPlayerPos.y] != null)
		{
			while (jobPool.Count < 1)
			{
				yield return null;
			}
			CreateMeshPoolJobsForChunk(lastPlayerPos);
		}
		useColliders = GameSettings.CurrentSettings.GetBoolSetting("TREE_COLLISIONS");
		if (useColliders && VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Scenario)
		{
			StartCoroutine(ColliderRoutine());
		}
		while (base.enabled)
		{
			yield return StartCoroutine(ClearOutOfBoundsGrids(lastPlayerPos));
			yield return DispatchJobsForInRadiusChunks(lastPlayerPos);
			while (meshPoolJobsToProcess > 0)
			{
				yield return null;
			}
			meshPoolJobsToProcess = 0;
			yield return null;
			lastPlayerPos = WorldToGridPos(VRHead.position);
		}
	}

	private void CreateMeshPoolJobsForChunk(IntVector2 grid)
	{
		preloadedChunks.Add(grid);
		int num = treeProfiles.Length;
		TreeChunkGroup treeChunkGroup = treeChunks[grid.x, grid.y];
		for (int i = 0; i < treeChunkGroup.chunks.Count; i++)
		{
			TreeChunk treeChunk = treeChunkGroup.chunks[i];
			if (treeChunk != null)
			{
				for (int j = 0; j < num; j++)
				{
					TreeJob treeJob = jobPool.Pop();
					if (treeJob.chunk == null)
					{
						treeJob.chunk = new ChunkJobData();
						treeJob.chunk.treePoints = new List<Vector3>(treeChunk.treePoints.Count);
						int capacity = 10;
						if (treeChunk.treeRemovers != null)
						{
							capacity = treeChunk.treeRemovers.Count;
						}
						treeJob.chunk.treeRemovers = new List<TreeRemover>(capacity);
						treeJob.chunk.treeScales = new List<float>(treeChunk.treePoints.Count);
					}
					treeJob.chunk.groupGrid = grid;
					treeJob.chunk.subGrid = treeChunk.subGrid;
					treeJob.chunk.treePoints.Clear();
					treeJob.chunk.treeRemovers.Clear();
					treeJob.chunk.treeScales.Clear();
					for (int k = 0; k < treeChunk.treePoints.Count; k++)
					{
						treeJob.chunk.treePoints.Add(treeChunk.treePoints[k]);
						treeJob.chunk.treeScales.Add(treeChunk.treeScales[k]);
					}
					if (treeChunk.treeRemovers != null)
					{
						for (int l = 0; l < treeChunk.treeRemovers.Count; l++)
						{
							treeJob.chunk.treeRemovers.Add(treeChunk.treeRemovers[l]);
						}
					}
					treeJob.treeLODMeshes = lodVTMeshes[j];
					treeJob.billboard = treeProfiles[j].billboard;
					treeJob.maxVertCount = maxVertCount;
					treeJob.targetLOD = j;
					treeJob.treeColliderSizes = colliderSizes;
					lock (unfinishedJobLock)
					{
						unfinishedJobs.Enqueue(treeJob);
					}
					meshPoolJobsToProcess++;
				}
			}
			else
			{
				Debug.Log("Chunk was null!");
			}
		}
	}

	private IEnumerator MeshPoolProcessJobsRoutine()
	{
		worker.Begin();
		Queue<TreeJob> processQueue = new Queue<TreeJob>();
		int lodCount = treeProfiles.Length;
		while (base.enabled)
		{
			lock (finishedJobLock)
			{
				while (finishedJobs.Count > 0)
				{
					processQueue.Enqueue(finishedJobs.Dequeue());
				}
			}
			while (processQueue.Count > 0)
			{
				TreeJob treeJob = processQueue.Dequeue();
				if (treeJob != null)
				{
					int targetLOD = treeJob.targetLOD;
					if (meshPool.Count > 0)
					{
						TreeChunk chunk = GetChunk(treeJob.chunk.groupGrid, treeJob.chunk.subGrid);
						Mesh mesh = chunk.treeLODGroups[targetLOD].mf.sharedMesh;
						if (mesh == null)
						{
							mesh = meshPool.Pop();
						}
						treeJob.masterMesh.ApplyToMesh(mesh, recalculateBounds: false);
						Bounds bounds = mesh.bounds;
						bounds.size = new Vector3(treeChunkWidth, 2500f, treeChunkWidth);
						mesh.bounds = bounds;
						chunk.treeLODGroups[targetLOD].mf.mesh = mesh;
						chunk.completedLODs++;
						if (chunk.completedLODs == lodCount)
						{
							chunk.treeObj.SetActive(value: true);
							chunk.treeObj.GetComponent<LODGroup>().RecalculateBounds();
						}
					}
					else
					{
						Debug.Log("There was no available mesh in the mesh pool. Skipping tree chunk.");
					}
					meshPoolJobsToProcess--;
					jobPool.Push(treeJob);
					yield return null;
				}
				else
				{
					yield return null;
				}
			}
			yield return null;
		}
	}

	private IEnumerator DispatchJobsForInRadiusChunks(IntVector2 startGrid)
	{
		int maxDispatchesPerFrame = 64;
		int dispatches = 0;
		int maxChunkJobCount = treeProfiles.Length * subdivisions * subdivisions;
		for (int radius = 1; radius < preloadRadius; radius++)
		{
			for (int side = -1; side <= 1; side += 2)
			{
				for (int y2 = startGrid.y - radius; y2 <= startGrid.y + radius; y2++)
				{
					IntVector2 grid = new IntVector2(startGrid.x + side * radius, y2);
					if (grid.x < 0 || grid.x >= gridSize || grid.y < 0 || grid.y >= gridSize)
					{
						continue;
					}
					if (treeChunks[grid.x, grid.y] != null && !preloadedChunks.Contains(grid))
					{
						while (jobPool.Count < maxChunkJobCount)
						{
							yield return null;
						}
						CreateMeshPoolJobsForChunk(grid);
					}
					dispatches++;
					if (dispatches >= maxDispatchesPerFrame)
					{
						yield return null;
						dispatches = 0;
					}
				}
				for (int y2 = startGrid.x - (radius - 1); y2 <= startGrid.x + (radius - 1); y2++)
				{
					IntVector2 grid = new IntVector2(y2, startGrid.y + side * radius);
					if (grid.x < 0 || grid.x >= gridSize || grid.y < 0 || grid.y >= gridSize)
					{
						continue;
					}
					if (treeChunks[grid.x, grid.y] != null && !preloadedChunks.Contains(grid))
					{
						while (jobPool.Count < maxChunkJobCount)
						{
							yield return null;
						}
						CreateMeshPoolJobsForChunk(grid);
					}
					dispatches++;
					if (dispatches >= maxDispatchesPerFrame)
					{
						yield return null;
						dispatches = 0;
					}
				}
			}
		}
	}

	private IEnumerator ClearOutOfBoundsGrids(IntVector2 playerPos)
	{
		List<IntVector2> list = new List<IntVector2>();
		foreach (IntVector2 preloadedChunk in preloadedChunks)
		{
			if (IntVector2.MaxOffset(playerPos, preloadedChunk) <= preloadRadius)
			{
				continue;
			}
			TreeChunkGroup treeChunkGroup = treeChunks[preloadedChunk.x, preloadedChunk.y];
			list.Add(preloadedChunk);
			foreach (TreeChunk chunk in treeChunkGroup.chunks)
			{
				if (chunk == null)
				{
					continue;
				}
				TreeLOD[] treeLODGroups = chunk.treeLODGroups;
				foreach (TreeLOD treeLOD in treeLODGroups)
				{
					Mesh sharedMesh = treeLOD.mf.sharedMesh;
					if (sharedMesh != null)
					{
						treeLOD.mf.sharedMesh = null;
						meshPool.Push(sharedMesh);
					}
				}
				chunk.completedLODs = 0;
				chunk.treeObj.SetActive(value: false);
			}
		}
		foreach (IntVector2 item in list)
		{
			preloadedChunks.Remove(item);
		}
		yield return null;
	}

	public void DespawnMeshPoolChunk(IntVector2 grid)
	{
		if (!preloadedChunks.Contains(grid))
		{
			return;
		}
		foreach (TreeChunk chunk in treeChunks[grid.x, grid.y].chunks)
		{
			if (chunk == null)
			{
				continue;
			}
			TreeLOD[] treeLODGroups = chunk.treeLODGroups;
			foreach (TreeLOD treeLOD in treeLODGroups)
			{
				Mesh sharedMesh = treeLOD.mf.sharedMesh;
				if (sharedMesh != null)
				{
					treeLOD.mf.sharedMesh = null;
					meshPool.Push(sharedMesh);
				}
			}
			chunk.completedLODs = 0;
			chunk.treeObj.SetActive(value: false);
		}
		preloadedChunks.Remove(grid);
	}

	private void SetupColliderPool()
	{
		int num = 8000;
		colliderPool = new List<CapsuleCollider>(num);
		for (int i = 0; i < num; i++)
		{
			CapsuleCollider capsuleCollider = new GameObject("TreeCollider").AddComponent<CapsuleCollider>();
			capsuleCollider.direction = 1;
			capsuleCollider.radius = 1f;
			capsuleCollider.height = 40f;
			capsuleCollider.enabled = false;
			colliderPool.Add(capsuleCollider);
		}
	}

	private IEnumerator ColliderRoutine()
	{
		SetupColliderPool();
		while (!FlightSceneManager.instance.playerActor)
		{
			yield return null;
		}
		int maxMovesPerFrame = 125;
		int moves = 0;
		float aheadTime = (float)colliderPool.Count / (float)maxMovesPerFrame / 180f;
		while (base.enabled)
		{
			IntVector2 playerGrid = PredictPlayerGridForCollider(aheadTime);
			if (playerGrid.x > 0 && playerGrid.y > 0 && playerGrid.x < gridSize && playerGrid.y < gridSize)
			{
				TreeChunkGroup treeChunk = treeChunks[playerGrid.x, playerGrid.y];
				if (treeChunk != null)
				{
					int colIdx = 0;
					for (int c = 0; c < treeChunk.chunks.Count; c++)
					{
						if (colIdx >= colliderPool.Count)
						{
							break;
						}
						TreeChunk tc = treeChunk.chunks[c];
						for (int t = 0; t < tc.treePoints.Count; t++)
						{
							if (colIdx >= colliderPool.Count)
							{
								break;
							}
							while (!tc.treeObj.activeSelf)
							{
								yield return null;
							}
							Vector3 vector = tc.treePoints[t];
							CapsuleCollider capsuleCollider = colliderPool[colIdx];
							capsuleCollider.enabled = false;
							capsuleCollider.transform.parent = tc.treeObj.transform;
							Vector3 vector2 = tc.colliderSizes[t];
							capsuleCollider.radius = vector2.x;
							capsuleCollider.height = vector2.y;
							capsuleCollider.transform.localPosition = vector + new Vector3(0f, vector2.z, 0f);
							capsuleCollider.enabled = true;
							colIdx++;
							moves++;
							if (moves >= maxMovesPerFrame)
							{
								moves = 0;
								yield return null;
							}
						}
					}
				}
			}
			while (!FlightSceneManager.instance.playerActor || playerGrid == PredictPlayerGridForCollider(aheadTime))
			{
				yield return null;
			}
		}
	}

	private IntVector2 PredictPlayerGridForCollider(float time)
	{
		return WorldToGridPos(FlightSceneManager.instance.playerActor.position + FlightSceneManager.instance.playerActor.velocity * time);
	}

	public void RequestJobs(Queue<TreeJob> outputQueue)
	{
		lock (unfinishedJobLock)
		{
			while (unfinishedJobs.Count > 0)
			{
				outputQueue.Enqueue(unfinishedJobs.Dequeue());
			}
		}
	}

	private void SetupWorker()
	{
		worker = new TreeWorker();
		worker.master = this;
	}

	public void SendFinishedJobs(Queue<TreeJob> jobs)
	{
		lock (finishedJobLock)
		{
			while (jobs.Count > 0)
			{
				finishedJobs.Enqueue(jobs.Dequeue());
			}
		}
	}
}
