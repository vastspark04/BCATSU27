using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeParticleMaster : MonoBehaviour
{
	private FastNoise fastNoise;

	public int seed = 1230;

	public GameObject[] treeLODTemplates;

	public float[] lodRanges;

	public float maxTreesPerTri;

	public TreePopulator.ColorChannels colorChannel;

	public TreePopulator.NoiseFunction[] noiseFunctions;

	public GameObject boundsObject;

	public int treeBlocksPerChunk = 8;

	public Transform[] terrainTransforms;

	private float treeBlockSize;

	private float tSize;

	private LOD[] boundsLods;

	private List<LODGroup> lodGrps = new List<LODGroup>();

	private void Start()
	{
		StartCoroutine(Setup());
	}

	private IEnumerator Setup()
	{
		tSize = terrainTransforms[0].GetComponent<Renderer>().bounds.size.x;
		treeBlockSize = tSize / (float)treeBlocksPerChunk;
		boundsObject.transform.localScale = treeBlockSize * Vector3.one;
		fastNoise = new FastNoise(seed);
		TreePopulator.NoiseProfile noiseProfile = new TreePopulator.NoiseProfile();
		noiseProfile.fastNoise = fastNoise;
		noiseProfile.noiseFunctions = noiseFunctions;
		LOD lOD = default(LOD);
		lOD.renderers = new Renderer[1] { boundsObject.GetComponent<Renderer>() };
		boundsLods = new LOD[1] { lOD };
		for (int i = 0; i < terrainTransforms.Length; i++)
		{
			Transform transform = terrainTransforms[i];
			GenerateTrees(transform.GetComponent<MeshFilter>(), noiseProfile);
		}
		boundsObject.SetActive(value: false);
		yield return null;
		foreach (LODGroup lodGrp in lodGrps)
		{
			lodGrp.RecalculateBounds();
		}
	}

	private void GenerateTrees(MeshFilter mf, TreePopulator.NoiseProfile noiseProfile)
	{
		List<Vector3> list = TreePopulator.GenerateTreePoints(mf, maxTreesPerTri, colorChannel, noiseProfile);
		for (int i = 0; i < treeBlocksPerChunk; i++)
		{
			for (int j = 0; j < treeBlocksPerChunk; j++)
			{
				GameObject gameObject = null;
				LOD[] array = null;
				LODGroup lODGroup = null;
				int num = -1;
				for (int k = 0; k < treeLODTemplates.Length; k++)
				{
					if (num < 0)
					{
						num = 0;
					}
					else if (num == 0)
					{
						continue;
					}
					GameObject gameObject2 = null;
					ParticleSystem particleSystem = null;
					Renderer renderer = null;
					int count = list.Count;
					for (int l = 0; l < count; l++)
					{
						Vector3 position = mf.transform.position;
						position.x -= tSize / 2f;
						position.z -= tSize / 2f;
						position.x += treeBlockSize * ((float)i + 0.5f);
						position.z += treeBlockSize * ((float)j + 0.5f);
						Vector3 vector = mf.transform.TransformPoint(list[l]);
						if (!(Mathf.Abs(vector.x - position.x) > treeBlockSize / 2f) && !(Mathf.Abs(vector.z - position.z) > treeBlockSize / 2f) && !(WaterPhysics.GetAltitude(vector) < 5f))
						{
							if (gameObject == null)
							{
								gameObject = new GameObject("TreeBlock");
								gameObject.transform.parent = mf.transform;
								mf.gameObject.GetComponent<Collider>().Raycast(new Ray(position + 10000f * Vector3.up, Vector3.down), out var hitInfo, 12000f);
								gameObject.transform.position = hitInfo.point;
								array = new LOD[lodRanges.Length];
								lODGroup = gameObject.AddComponent<LODGroup>();
							}
							if (gameObject2 == null)
							{
								gameObject2 = Object.Instantiate(treeLODTemplates[k], gameObject.transform);
								gameObject2.transform.localPosition = Vector3.zero;
								gameObject2.transform.localRotation = Quaternion.identity;
								gameObject2.transform.localScale = Vector3.one;
								gameObject2.SetActive(value: true);
								particleSystem = gameObject2.GetComponent<ParticleSystem>();
								renderer = gameObject2.GetComponent<Renderer>();
								array[k].renderers = new Renderer[1] { renderer };
								array[k].screenRelativeTransitionHeight = lodRanges[k];
							}
							ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
							emitParams.startSize3D = new Vector3(particleSystem.main.startSizeX.constant, particleSystem.main.startSizeY.constant, particleSystem.main.startSizeZ.constant);
							emitParams.position = gameObject.transform.InverseTransformPoint(vector);
							particleSystem.Emit(emitParams, 1);
							num++;
						}
					}
					if (num == 0 && (bool)gameObject2)
					{
						Debug.Log("a lodGo was created unecessarily");
						Object.Destroy(gameObject2);
					}
				}
				if (num > 0)
				{
					lODGroup.SetLODs(array);
					lodGrps.Add(lODGroup);
				}
				else
				{
					Object.Destroy(gameObject);
				}
			}
		}
	}
}
