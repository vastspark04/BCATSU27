using System.Collections.Generic;
using UnityEngine;

public class ApplyCityLODsScript : MonoBehaviour
{
	public List<GameObject> newPrefabs;

	public GameObject pixelParent;

	[ContextMenu("Apply script")]
	public void Apply()
	{
		CityBuilderPixel[] componentsInChildren = pixelParent.GetComponentsInChildren<CityBuilderPixel>();
		foreach (CityBuilderPixel cityBuilderPixel in componentsInChildren)
		{
			LODGroup lODGroup = cityBuilderPixel.GetComponent<LODGroup>();
			if (!lODGroup)
			{
				lODGroup = cityBuilderPixel.gameObject.AddComponent<LODGroup>();
			}
			LOD[] array = new LOD[2];
			List<Renderer> list = new List<Renderer>();
			List<Renderer> list2 = new List<Renderer>();
			array[0].screenRelativeTransitionHeight = 0.1f;
			array[1].screenRelativeTransitionHeight = 0.03f;
			foreach (GameObject newPrefab in newPrefabs)
			{
				Transform[] componentsInChildren2 = cityBuilderPixel.GetComponentsInChildren<Transform>();
				foreach (Transform transform in componentsInChildren2)
				{
					if (!transform || !transform.gameObject.name.StartsWith(newPrefab.name))
					{
						continue;
					}
					GameObject gameObject = Object.Instantiate(newPrefab);
					gameObject.name = newPrefab.name;
					gameObject.transform.parent = transform.parent;
					gameObject.transform.position = transform.position;
					gameObject.transform.rotation = transform.rotation;
					LODGroup component = gameObject.GetComponent<LODGroup>();
					if ((bool)component)
					{
						LOD[] lODs = component.GetLODs();
						Renderer[] renderers = lODs[0].renderers;
						foreach (Renderer item in renderers)
						{
							list.Add(item);
						}
						renderers = lODs[1].renderers;
						foreach (Renderer item2 in renderers)
						{
							list2.Add(item2);
						}
						Object.DestroyImmediate(component);
					}
					int num = cityBuilderPixel.objectTransforms.IndexOf(transform);
					if (num >= 0)
					{
						cityBuilderPixel.objectTransforms[num] = gameObject.transform;
					}
					Object.DestroyImmediate(transform.gameObject);
				}
			}
			array[0].renderers = list.ToArray();
			array[1].renderers = list2.ToArray();
			lODGroup.SetLODs(array);
			lODGroup.RecalculateBounds();
		}
	}

	[ContextMenu("Check Nulls")]
	public void CheckNulls()
	{
		LODGroup[] componentsInChildren = pixelParent.GetComponentsInChildren<LODGroup>();
		foreach (LODGroup lODGroup in componentsInChildren)
		{
			LOD[] lODs = lODGroup.GetLODs();
			for (int j = 0; j < lODs.Length; j++)
			{
				Renderer[] renderers = lODs[j].renderers;
				for (int k = 0; k < renderers.Length; k++)
				{
					if (renderers[k] == null)
					{
						Debug.Log("Null renderer in " + lODGroup.gameObject.name, lODGroup.gameObject);
					}
				}
			}
		}
	}
}
