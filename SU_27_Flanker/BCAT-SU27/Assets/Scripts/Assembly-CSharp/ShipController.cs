using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ShipController : MonoBehaviour
{
	public static ShipController instance;

	public float softCeiling;

	public EjectionSeat ejectionSeat;

	public GameObject cameraRig;

	public FlightInfo flightInfo;

	public UnityAction OnFlybyCameraEnter;

	public UnityAction OnFlybyCameraExit;

	private Transform origRigParent;

	private Vector3 origRigPos;

	private Quaternion origRigRot;

	private Coroutine flybyRoutine;

	public FuelTank fuelTank { get; private set; }

	public Health health { get; private set; }

	public float currentHealth => health.normalizedHealth * health.maxHealth;

	public Rigidbody rb { get; private set; }

	public float speed { get; private set; }

	public bool isExternalCamera { get; private set; }

	private void Awake()
	{
		instance = this;
		rb = GetComponent<Rigidbody>();
		health = GetComponent<Health>();
		fuelTank = GetComponent<FuelTank>();
	}

	private void Update()
	{
	}

	public void ReloadScene()
	{
		FlightSceneManager.instance.ReloadScene();
	}

	private void FixedUpdate()
	{
		if (flightInfo.altitudeASL > softCeiling)
		{
			float num = flightInfo.altitudeASL - softCeiling;
			if (rb.velocity.y > 0f)
			{
				Vector3 velocity = rb.velocity;
				velocity.y = Mathf.MoveTowards(velocity.y, 0f, 0.1f * num * Time.fixedDeltaTime);
				rb.velocity = velocity;
			}
		}
	}

	public void FlybyCam(VRHandController controller)
	{
		if (flybyRoutine == null)
		{
			flybyRoutine = StartCoroutine(FlybyRoutine(controller));
		}
	}

	private IEnumerator FlybyRoutine(VRHandController controller)
	{
		yield return new WaitForFixedUpdate();
		AudioController.instance.SetExteriorOpening("flyby", 1f);
		isExternalCamera = true;
		origRigPos = cameraRig.transform.localPosition;
		origRigRot = cameraRig.transform.localRotation;
		origRigParent = cameraRig.transform.parent;
		cameraRig.transform.parent = null;
		FloatingOrigin.instance.AddTransform(cameraRig.transform);
		Vector3 normalized = Vector3.Cross(Vector3.up, rb.velocity).normalized;
		cameraRig.transform.position = rb.position + rb.velocity * 3f + normalized * 30f;
		if (Physics.Linecast(base.transform.position, cameraRig.transform.position, out var hitInfo, 1))
		{
			cameraRig.transform.position = hitInfo.point + hitInfo.normal * 2f;
		}
		if ((bool)WaterPhysics.instance && cameraRig.transform.position.y < WaterPhysics.instance.height)
		{
			Vector3 position = cameraRig.transform.position;
			position.y = WaterPhysics.instance.height + 2f;
			cameraRig.transform.position = position;
		}
		if (OnFlybyCameraEnter != null)
		{
			OnFlybyCameraEnter();
		}
		float t = Time.time;
		while (Time.time - t < 30f)
		{
			cameraRig.transform.rotation = Quaternion.LookRotation(base.transform.position - cameraRig.transform.position);
			if (controller.GetThumbButtonDown() && controller.triggerClicked)
			{
				break;
			}
			yield return null;
		}
		isExternalCamera = false;
		AudioController.instance.SetExteriorOpening("flyby", 0f);
		cameraRig.transform.parent = origRigParent;
		cameraRig.transform.localPosition = origRigPos;
		cameraRig.transform.localRotation = origRigRot;
		FloatingOrigin.instance.RemoveTransform(cameraRig.transform);
		flybyRoutine = null;
		if (OnFlybyCameraExit != null)
		{
			OnFlybyCameraExit();
		}
	}

	public void KillPilot()
	{
		GetComponentInChildren<TempPilotDetacher>().DetachPilot();
	}
}
