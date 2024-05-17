using System.Collections;
using UnityEngine;

public class TempAIPlaneSpawner : MonoBehaviour
{
	public void SpawnPlaneLanded(AIPilot pilot, Transform spawnTransform)
	{
		StartCoroutine(SpawnLandedRoutine(pilot, spawnTransform));
	}

	private IEnumerator SpawnLandedRoutine(AIPilot pilot, Transform spawnTransform)
	{
		pilot.gameObject.SetActive(value: false);
		pilot.transform.parent = spawnTransform;
		pilot.transform.localPosition = Vector3.zero;
		pilot.autoPilot.rb.interpolation = RigidbodyInterpolation.None;
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		Vector3[] array = new Vector3[3];
		int num = 0;
		float height = 0f;
		RaySpringDamper[] componentsInChildren = pilot.gameObject.GetComponentsInChildren<RaySpringDamper>();
		foreach (RaySpringDamper raySpringDamper in componentsInChildren)
		{
			float a = raySpringDamper.suspensionDistance + (pilot.transform.position.y - raySpringDamper.transform.position.y);
			height = Mathf.Max(a, height);
			if (num < 3)
			{
				array[num] = raySpringDamper.transform.position - raySpringDamper.suspensionDistance * raySpringDamper.transform.up;
				num++;
			}
		}
		Plane plane = new Plane(array[0], array[1], array[2]);
		Vector3 up2 = plane.normal * Mathf.Sign(Vector3.Dot(plane.normal, Vector3.up));
		up2 = Quaternion.FromToRotation(up2, pilot.transform.up) * Vector3.up;
		Vector3 fwd = Vector3.Cross(up2, -pilot.transform.right);
		if ((bool)pilot.wingRotator)
		{
			pilot.wingRotator.SetNormalizedRotationImmediate(1f);
		}
		RaycastHit hitInfo;
		while (!Physics.Raycast(spawnTransform.position, Vector3.down, out hitInfo, height * 2f, 1, QueryTriggerInteraction.Ignore))
		{
			Debug.Log("waiting for raycast");
			yield return null;
		}
		pilot.transform.position = hitInfo.point + height * Vector3.up;
		pilot.transform.rotation = Quaternion.LookRotation(fwd, up2);
		pilot.autoPilot.rb.velocity = Vector3.zero;
		pilot.autoPilot.rb.angularVelocity = Vector3.zero;
		pilot.autoPilot.rb.isKinematic = false;
		pilot.kPlane.SetVelocity(Vector3.zero);
		pilot.commandState = AIPilot.CommandStates.Park;
		pilot.transform.parent = null;
		pilot.gameObject.SetActive(value: true);
	}
}
