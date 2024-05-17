using System.Collections;
using UnityEngine;

public class VTOLMidFlightStarter : MonoBehaviour
{
	public float startSpeed;

	public Transform startTransform;

	private void Start()
	{
		StartCoroutine(StartupRoutine());
	}

	private IEnumerator StartupRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		yield return null;
		VTOLQuickStart componentInChildren = FlightSceneManager.instance.playerActor.GetComponentInChildren<VTOLQuickStart>();
		if ((bool)componentInChildren)
		{
			componentInChildren.QuickStart();
			componentInChildren.gearLever.RemoteSetState(1);
			TiltController componentInChildren2 = FlightSceneManager.instance.playerActor.GetComponentInChildren<TiltController>();
			if ((bool)componentInChildren2)
			{
				componentInChildren2.SetTiltImmediate(90f);
			}
			FlightInfo flightInfo = FlightSceneManager.instance.playerActor.GetComponentInChildren<FlightInfo>();
			flightInfo.transform.position = startTransform.position;
			flightInfo.transform.rotation = startTransform.rotation;
			Vector3 vel = startTransform.forward;
			while (flightInfo.airspeed < startSpeed)
			{
				flightInfo.rb.angularVelocity = Vector3.zero;
				vel += 78.48f * Time.fixedDeltaTime * flightInfo.transform.forward;
				flightInfo.rb.velocity = vel;
				flightInfo.transform.position = startTransform.position;
				flightInfo.transform.rotation = startTransform.rotation;
				yield return new WaitForFixedUpdate();
			}
		}
	}
}
