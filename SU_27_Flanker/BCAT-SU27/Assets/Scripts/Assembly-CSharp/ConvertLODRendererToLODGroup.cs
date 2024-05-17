using System.Collections.Generic;
using UnityEngine;

public class ConvertLODRendererToLODGroup : MonoBehaviour
{
	public LODRenderer lodR;

	public LODGroup lodG;

	public int fromIdx;

	public int toIdx;

	[ContextMenu("Apply")]
	public void Apply()
	{
		LOD[] array = lodG.GetLODs();
		if (array.Length <= toIdx)
		{
			List<LOD> list = new List<LOD>();
			int i;
			for (i = 0; i < array.Length; i++)
			{
				list.Add(array[i]);
			}
			for (; i <= toIdx; i++)
			{
				list.Add(new LOD(Mathf.Lerp(list[i - 1].screenRelativeTransitionHeight, 1f, 0.5f), new Renderer[0]));
			}
			array = list.ToArray();
		}
		LOD lOD = array[toIdx];
		List<Renderer> list2 = new List<Renderer>();
		Renderer[] renderers = lOD.renderers;
		foreach (Renderer item in renderers)
		{
			list2.Add(item);
		}
		renderers = lodR.levels[fromIdx].renderers;
		foreach (Renderer renderer in renderers)
		{
			renderer.enabled = true;
			list2.Add(renderer);
		}
		lOD.renderers = list2.ToArray();
		array[toIdx] = lOD;
		lodG.SetLODs(array);
	}
}
