using System;
using System.Collections.Generic;
using UnityEngine;

public class LB_DirectionGenerator : MonoBehaviour
{
	[Serializable]
	public class Direction
	{
		[Range(0f, 180f)]
		public float bearing;

		[Range(-90f, 90f)]
		public float pitch;

		public bool mirror;

		public Vector3 ToVector3()
		{
			Vector3 forward = Vector3.forward;
			forward = Quaternion.AngleAxis(pitch, -Vector3.right) * forward;
			return (Quaternion.AngleAxis(bearing, Vector3.up) * forward).normalized;
		}
	}

	public List<LB_TrainingPilot> applyToPilots;

	public List<Direction> directions;

	public int debug_count;

	[ContextMenu("Apply")]
	private void Apply()
	{
		List<Vector3> list = new List<Vector3>();
		for (int i = 0; i < directions.Count; i++)
		{
			Vector3 vector = directions[i].ToVector3();
			list.Add(vector);
			if (directions[i].mirror)
			{
				Vector3 item = vector;
				item.x *= -1f;
				list.Add(item);
			}
		}
		foreach (LB_TrainingPilot applyToPilot in applyToPilots)
		{
			applyToPilot.directions = list.ToArray();
		}
	}

	private void OnDrawGizmos()
	{
		debug_count = 0;
		Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
		for (int i = 0; i < directions.Count; i++)
		{
			Vector3 vector = directions[i].ToVector3();
			Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.TransformDirection(vector) * 100f);
			debug_count++;
			if (directions[i].mirror)
			{
				Vector3 direction = vector;
				direction.x *= -1f;
				Gizmos.DrawLine(base.transform.position, base.transform.position + base.transform.TransformDirection(direction) * 100f);
				debug_count++;
			}
		}
	}
}
