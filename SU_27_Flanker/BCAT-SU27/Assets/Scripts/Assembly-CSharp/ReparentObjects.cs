using System.Collections.Generic;
using UnityEngine;

public class ReparentObjects : MonoBehaviour
{
	public List<Transform> transformsToReparent;

	private void Start()
	{
		foreach (Transform item in transformsToReparent)
		{
			item.transform.SetParent(base.transform, worldPositionStays: true);
		}
	}
}
