using System.Collections.Generic;
using UnityEngine;

public class Location : MonoBehaviour
{
	public static List<Location> locations = new List<Location>();

	public string locationName;

	public float radius;

	public float sqrRadius { get; private set; }

	private void Awake()
	{
		locations.Add(this);
		sqrRadius = radius * radius;
	}

	private void OnDestroy()
	{
		if (locations != null)
		{
			locations.Remove(this);
		}
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.gray;
		Gizmos.DrawWireSphere(base.transform.position, radius);
	}
}
