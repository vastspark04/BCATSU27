using System.Collections;
using UnityEngine;

public class TreeMaster : MonoBehaviour
{
	private FastNoise fastNoise;

	public int lodUpdatesPerFrame = 5;

	public Mesh[] treeLODs;

	public float[] lodRanges;

	private float[] lodRangesSqr;

	public Vector2 scaleRange;

	public float maxTreesPerTri = 6f;

	public TreeGenerator[] generators;

	public TreePopulator.NoiseFunction[] noiseFunctions;

	[ContextMenu("Get All Generators")]
	public void GetAllGenerators()
	{
		generators = Object.FindObjectsOfType<TreeGenerator>();
	}

	private IEnumerator Start()
	{
		TreeGenerator[] array = generators;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		yield break;
	}

	private IEnumerator LODRoutine()
	{
		int updateCount = 0;
		int genCount = generators.Length;
		Vector3 playerPos = VRHead.position;
		int lodCount = lodRanges.Length;
		while (base.enabled)
		{
			for (int i = 0; i < genCount; i++)
			{
				TreeGenerator treeGenerator = generators[i];
				float sqrMagnitude = (treeGenerator.transform.position - playerPos).sqrMagnitude;
				int num = -1;
				for (int j = 0; j < lodCount; j++)
				{
					if (sqrMagnitude < lodRangesSqr[j])
					{
						num = j;
						break;
					}
				}
				if (num >= 0)
				{
					treeGenerator.treeMesh = treeLODs[num];
					if (treeGenerator.cull)
					{
						treeGenerator.cull = false;
						treeGenerator.RecalculateMatrices();
					}
				}
				else
				{
					treeGenerator.cull = true;
				}
				updateCount++;
				if (updateCount >= lodUpdatesPerFrame)
				{
					updateCount = 0;
					yield return null;
					playerPos = VRHead.position;
				}
			}
			yield return null;
		}
	}
}
