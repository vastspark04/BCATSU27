using System.Collections.Generic;
using UnityEngine;

public class TreeGenerator : MonoBehaviour
{
	public MeshFilter terrainMesh;

	public Mesh treeMesh;

	public Material treeMaterial;

	public Vector2 scaleRange;

	public float[] lodRanges;

	private float[] lodSqrRanges;

	public int updatesPerFrame = 100;

	public float maxTreesPerTri;

	public TreePopulator.ColorChannels colorChannel;

	private Matrix4x4[][] matrices;

	private int batchCount;

	private int treeCount;

	[HideInInspector]
	private List<Vector3> positions;

	[HideInInspector]
	private List<Quaternion> rotations = new List<Quaternion>();

	[HideInInspector]
	private List<Vector3> scales = new List<Vector3>();

	private bool ready;

	public bool cull = true;

	[ContextMenu("Get MeshFilter")]
	public void GetMeshfilter()
	{
		terrainMesh = GetComponent<MeshFilter>();
	}

	public void Generate(TreePopulator.NoiseProfile noiseProfile)
	{
		ready = true;
		positions = TreePopulator.GenerateTreePoints(terrainMesh, maxTreesPerTri, colorChannel, noiseProfile);
		treeCount = positions.Count;
		batchCount = treeCount / 1023 + 1;
		matrices = new Matrix4x4[batchCount][];
		for (int i = 0; i < batchCount; i++)
		{
			if (i == batchCount - 1)
			{
				matrices[i] = new Matrix4x4[treeCount % 1023];
			}
			else
			{
				matrices[i] = new Matrix4x4[1023];
			}
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (num3 < treeCount)
		{
			if (num2 == 1023)
			{
				num++;
				num2 = 0;
			}
			rotations.Add(Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up));
			float y = Random.Range(scaleRange.x, scaleRange.y);
			float num4 = Random.Range(scaleRange.x, scaleRange.y);
			scales.Add(new Vector3(num4, y, num4));
			matrices[num][num2] = Matrix4x4.TRS(terrainMesh.transform.TransformPoint(positions[num3]), rotations[num3], scales[num3]);
			num3++;
			num2++;
		}
		FloatingOrigin.instance.OnPostOriginShift += FloatingOrigin_instance_OnOriginShift;
	}

	private void FloatingOrigin_instance_OnOriginShift(Vector3 offset)
	{
		RecalculateMatrices();
	}

	public void RecalculateMatrices()
	{
		if (!ready || cull)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		while (num3 < treeCount)
		{
			if (num2 == 1023)
			{
				num++;
				num2 = 0;
			}
			matrices[num][num2] = Matrix4x4.TRS(terrainMesh.transform.TransformPoint(positions[num3]), rotations[num3], scales[num3]);
			num3++;
			num2++;
		}
	}

	private void LateUpdate()
	{
		if (ready && !cull)
		{
			for (int i = 0; i < batchCount; i++)
			{
				Graphics.DrawMeshInstanced(treeMesh, 0, treeMaterial, matrices[i]);
			}
		}
	}
}
