using System.Collections.Generic;
using UnityEngine;

public class TakeoffPathTest : MonoBehaviour
{
	public AirbaseNavigation navSys;

	[HideInInspector]
	public List<AirbaseNavNode> path = new List<AirbaseNavNode>();

	private void OnDrawGizmosSelected()
	{
		GetPath();
		Gizmos.color = Color.green;
		if (path != null && path.Count > 0)
		{
			Gizmos.DrawLine(base.transform.position, path[0].position);
			for (int i = 1; i < path.Count; i++)
			{
				Gizmos.DrawLine(path[i].position + 10f * Vector3.up, path[i - 1].position + 10f * Vector3.up);
			}
		}
	}

	[ContextMenu("Get Path")]
	public void GetPath()
	{
		if (navSys != null)
		{
			path = navSys.GetTakeoffPath(base.transform.position, base.transform.forward, out var _, 100f);
		}
	}

	[ContextMenu("Get Parking Path")]
	public void GetParkingPath()
	{
		if (navSys != null)
		{
			path = navSys.GetParkingPath_OLD(base.transform.position, base.transform.forward, 15f);
		}
	}
}
