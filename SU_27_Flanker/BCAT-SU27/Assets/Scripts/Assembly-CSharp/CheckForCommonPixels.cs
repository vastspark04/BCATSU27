using System.Collections.Generic;
using UnityEngine;

public class CheckForCommonPixels : MonoBehaviour
{
	public List<GameObject> cityParents;

	[ContextMenu("Check")]
	public void Check()
	{
		int count = cityParents.Count;
		List<string> list = new List<string>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (GameObject cityParent in cityParents)
		{
			CityBuilderPixel[] componentsInChildren = cityParent.GetComponentsInChildren<CityBuilderPixel>();
			foreach (CityBuilderPixel cityBuilderPixel in componentsInChildren)
			{
				if (!list.Contains(cityBuilderPixel.gameObject.name))
				{
					list.Add(cityBuilderPixel.gameObject.name);
				}
			}
			foreach (string item in list)
			{
				if (dictionary.TryGetValue(item, out var value))
				{
					value = (dictionary[item] = value + 1);
				}
				else
				{
					dictionary[item] = 1;
				}
			}
			list.Clear();
		}
		Debug.Log("Common pixels: ");
		foreach (KeyValuePair<string, int> item2 in dictionary)
		{
			if (item2.Value == count)
			{
				Debug.Log("    " + item2.Key);
			}
		}
	}
}
