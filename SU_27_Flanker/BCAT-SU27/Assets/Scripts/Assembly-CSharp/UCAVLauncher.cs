using System.Collections;
using UnityEngine;

public class UCAVLauncher : MonoBehaviour
{
	public AIPilot aiPilot;

	public Rigidbody planeRb;

	public SolidBooster booster;

	public RotationToggle wingRotator;

	public ModuleEngine engine;

	public bool orbitSelf = true;

	public bool launched;

	public bool finishedLaunchSequence;

	private Coroutine launchRoutine;

	private Coroutine railRoutine;

	private bool qlStopped;

	private void Start()
	{
		if (!qlStopped)
		{
			if ((bool)wingRotator)
			{
				wingRotator.SetDefault();
				wingRotator.SetNormalizedRotationImmediate(1f);
			}
			if ((bool)aiPilot)
			{
				aiPilot.actor.role = Actor.Roles.GroundArmor;
				aiPilot.autoPilot.enabled = false;
				aiPilot.autoPilot.flightInfo.ForceUpdateNow();
				aiPilot.autoPilot.flightInfo.enabled = false;
			}
		}
	}

	public void QL_StopLaunchRoutines()
	{
		qlStopped = true;
		finishedLaunchSequence = true;
		launched = true;
		if (launchRoutine != null)
		{
			StopCoroutine(launchRoutine);
		}
		if (railRoutine != null)
		{
			StopCoroutine(railRoutine);
		}
	}

	public void LaunchUCAV()
	{
		if (!launched && (bool)aiPilot && !aiPilot.enabled)
		{
			launched = true;
			launchRoutine = StartCoroutine(LaunchRoutine());
		}
	}

	private IEnumerator LaunchRoutine()
	{
		if (!planeRb)
		{
			yield break;
		}
		planeRb.transform.parent = null;
		planeRb.gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(planeRb);
		planeRb.isKinematic = false;
		planeRb.interpolation = RigidbodyInterpolation.Interpolate;
		if (!engine.engineEnabled)
		{
			engine.ToggleEngine();
		}
		booster.Fire();
		aiPilot.autoPilot.enabled = true;
		aiPilot.autoPilot.flightInfo.enabled = true;
		aiPilot.autoPilot.targetPosition = aiPilot.transform.position + aiPilot.transform.forward * 1000f + aiPilot.transform.up * 5f;
		aiPilot.autoPilot.inputLimiter = 0f;
		aiPilot.actor.role = Actor.Roles.Air;
		aiPilot.actor.parentActor = null;
		railRoutine = StartCoroutine(PlaneOnRailRoutine());
		if ((bool)booster)
		{
			yield return new WaitForSeconds(booster.burnTime / 4f);
		}
		if ((bool)wingRotator)
		{
			wingRotator.SetDefault();
		}
		if ((bool)booster)
		{
			yield return new WaitForSeconds(3f * booster.burnTime / 4f);
		}
		if (!aiPilot)
		{
			finishedLaunchSequence = true;
			yield break;
		}
		aiPilot.autoPilot.inputLimiter = 1f;
		aiPilot.initialSpeed = -1f;
		aiPilot.enabled = true;
		yield return null;
		aiPilot.kPlane.enabled = true;
		aiPilot.kPlane.ResetForces();
		aiPilot.kPlane.SetToKinematic();
		if (orbitSelf)
		{
			aiPilot.commandState = AIPilot.CommandStates.Orbit;
			aiPilot.orbitTransform = base.transform;
		}
		finishedLaunchSequence = true;
	}

	private IEnumerator PlaneOnRailRoutine()
	{
		Vector3 fwd = planeRb.transform.forward;
		Vector3 up = planeRb.transform.up;
		float t = Time.time;
		while ((bool)planeRb && Time.time - t < booster.burnTime)
		{
			planeRb.velocity = Vector3.Project(planeRb.velocity, fwd);
			planeRb.angularVelocity = Vector3.zero;
			planeRb.rotation = Quaternion.LookRotation(fwd, up);
			yield return null;
		}
	}
}
