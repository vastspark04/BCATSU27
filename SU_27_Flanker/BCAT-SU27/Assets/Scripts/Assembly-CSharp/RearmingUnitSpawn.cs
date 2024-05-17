using System.Collections;
using UnityEngine;

public class RearmingUnitSpawn : UnitSpawn
{
	public Transform[] alignToGroundTfs;

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		StartCoroutine(PlaceToGroundRoutine());
	}

	private IEnumerator PlaceToGroundRoutine()
	{
		yield return null;
		Transform[] array = alignToGroundTfs;
		foreach (Transform tf in array)
		{
			if ((bool)VTMapGenerator.fetch)
			{
				while (!VTMapGenerator.fetch.IsChunkColliderEnabled(tf.position))
				{
					yield return null;
				}
			}
			if (Physics.Raycast(tf.position + new Vector3(0f, 30f, 0f), Vector3.down, out var hitInfo, 60f, 1, QueryTriggerInteraction.Ignore))
			{
				if (Vector3.Dot(tf.forward, Vector3.up) > 0.9f)
				{
					tf.rotation = Quaternion.LookRotation(hitInfo.normal, tf.up);
				}
				else
				{
					Vector3 forward = Vector3.Cross(hitInfo.normal, -tf.right);
					tf.rotation = Quaternion.LookRotation(forward, hitInfo.normal);
				}
				Vector3 vector = (tf.position = hitInfo.point);
			}
			else
			{
				tf.gameObject.SetActive(value: false);
			}
		}
	}
}
