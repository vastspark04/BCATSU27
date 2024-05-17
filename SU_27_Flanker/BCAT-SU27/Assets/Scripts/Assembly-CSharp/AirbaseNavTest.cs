using System.Collections.Generic;
using UnityEngine;

public class AirbaseNavTest : MonoBehaviour
{
	public bool parking;

	public AirbaseNavigation navigation;

	private List<AirbaseNavNode> nodes;

	public bool test;

	public Transform destTransform;

	private void OnDrawGizmosSelected()
	{
		if (!navigation)
		{
			return;
		}
		if (test)
		{
			if ((bool)destTransform)
			{
				nodes = navigation.GetPathTo(base.transform.position, destTransform.position);
			}
			else if (parking)
			{
				nodes = navigation.GetParkingPath_OLD(base.transform.position, base.transform.forward, 0f);
			}
			else
			{
				nodes = navigation.GetTakeoffPath(base.transform.position, base.transform.forward, out var _, 1f);
			}
		}
		if (nodes != null && nodes.Count > 1)
		{
			Gizmos.color = Color.green;
			Vector3 vector = new Vector3(0f, 2f, 0f);
			for (int i = 0; i < nodes.Count - 1; i++)
			{
				Gizmos.DrawLine(nodes[i].position + vector, nodes[i + 1].position + vector);
			}
		}
	}
}
