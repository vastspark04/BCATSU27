using System.Collections;
using UnityEngine;

public class TestInstancedTrees : MonoBehaviour
{
	private class TreeInfo
	{
		public int lod = -1;

		public Vector3 worldPosition;

		public int matrixIdx;

		public bool isFree;
	}

	public Transform playerTf;

	public Mesh treeMesh;

	public Mesh treeMeshLOD1;

	public Mesh treeMeshLOD2;

	public Material treeMat;

	public float radius;

	public int count;

	public int updatesPerFrame = 100;

	private Matrix4x4[][] matricesLOD;

	private TreeInfo[] trees;

	private void Start()
	{
		StartCoroutine(UpdateMatricesRoutine());
	}

	private IEnumerator UpdateMatricesRoutine()
	{
		matricesLOD = new Matrix4x4[3][];
		for (int i = 0; i < 3; i++)
		{
			matricesLOD[i] = new Matrix4x4[count];
		}
		int treeCount = count * 3;
		trees = new TreeInfo[treeCount];
		int num = 0;
		float[] sqrRanges = new float[3];
		for (int j = 0; j < 3; j++)
		{
			int num2 = 0;
			while (num2 < count)
			{
				TreeInfo obj = (trees[num] = new TreeInfo());
				obj.matrixIdx = num2;
				obj.isFree = true;
				obj.lod = j;
				matricesLOD[j][num2] = Matrix4x4.identity;
				num2++;
				num++;
			}
			sqrRanges[j] = Mathf.Pow((float)(j + 1) * radius, 2f);
		}
		int tIdx = 0;
		while (base.enabled)
		{
			for (int k = 0; k < updatesPerFrame; k++)
			{
				Vector3 position = playerTf.position;
				TreeInfo treeInfo = trees[tIdx];
				if (treeInfo.isFree)
				{
					Vector2 insideUnitCircle = Random.insideUnitCircle;
					Vector3 vector = new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * radius;
					vector += (float)treeInfo.lod * radius * vector.normalized;
					vector.x += position.x;
					vector.z += position.z;
					Quaternion q;
					if (treeInfo.lod == 2)
					{
						Vector3 forward = vector - position;
						forward.y = 0f;
						q = Quaternion.LookRotation(forward);
					}
					else
					{
						q = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
					}
					Vector3 s = Random.Range(0.8f, 1.5f) * Vector3.one;
					matricesLOD[treeInfo.lod][treeInfo.matrixIdx] = Matrix4x4.TRS(vector, q, s);
					treeInfo.worldPosition = vector;
					treeInfo.isFree = false;
				}
				else
				{
					Vector3 vector2 = treeInfo.worldPosition - position;
					vector2.y = 0f;
					float sqrMagnitude = vector2.sqrMagnitude;
					if (sqrMagnitude > sqrRanges[treeInfo.lod] || (treeInfo.lod > 0 && sqrMagnitude < sqrRanges[treeInfo.lod - 1]))
					{
						treeInfo.isFree = true;
						matricesLOD[treeInfo.lod][treeInfo.matrixIdx] = Matrix4x4.identity;
					}
				}
				tIdx = (tIdx + 1) % treeCount;
			}
			yield return null;
		}
	}

	private void Update()
	{
		Graphics.DrawMeshInstanced(treeMesh, 0, treeMat, matricesLOD[0]);
		Graphics.DrawMeshInstanced(treeMeshLOD1, 0, treeMat, matricesLOD[1]);
		Graphics.DrawMeshInstanced(treeMeshLOD2, 0, treeMat, matricesLOD[2]);
	}
}
