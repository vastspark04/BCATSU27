using UnityEngine;

public class SearchForLayer : MonoBehaviour
{
	public int layerToSearch;

	[ContextMenu("Search")]
	public void Search()
	{
		GameObject[] array = Resources.FindObjectsOfTypeAll<GameObject>();
		foreach (GameObject gameObject in array)
		{
			if (gameObject.layer == layerToSearch)
			{
				Debug.Log("Found: " + gameObject.name);
			}
		}
	}
}
