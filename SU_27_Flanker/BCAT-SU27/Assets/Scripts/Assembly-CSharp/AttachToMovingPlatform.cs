using System.Collections;
using UnityEngine;

public class AttachToMovingPlatform : MonoBehaviour
{
	public float rayRadius = 1f;

	private void Awake()
	{
		GetComponent<UnitSpawn>().OnSpawnedUnit += UnitSpawn_OnSpawnedUnit;
	}

	private void UnitSpawn_OnSpawnedUnit()
	{
		if (Physics.Linecast(base.transform.position + rayRadius * Vector3.up, base.transform.position + rayRadius * Vector3.down, out var hitInfo, 1, QueryTriggerInteraction.Ignore))
		{
			MovingPlatform component = hitInfo.collider.gameObject.GetComponent<MovingPlatform>();
			if ((bool)component)
			{
				StartCoroutine(OnPlatformRoutine(component));
			}
		}
	}

	private IEnumerator OnPlatformRoutine(MovingPlatform mp)
	{
		Vector3 localPos = mp.transform.InverseTransformPoint(base.transform.position);
		Vector3 localFwd = mp.transform.InverseTransformDirection(base.transform.forward);
		Vector3 localUp = mp.transform.InverseTransformDirection(base.transform.up);
		while (base.enabled && (bool)mp)
		{
			base.transform.position = mp.transform.TransformPoint(localPos);
			Vector3 forward = mp.transform.TransformDirection(localFwd);
			Vector3 upwards = mp.transform.TransformDirection(localUp);
			base.transform.rotation = Quaternion.LookRotation(forward, upwards);
			yield return null;
		}
	}
}
