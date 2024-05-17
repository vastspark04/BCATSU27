using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class AIEjectPilot : MonoBehaviour
{
	public MinMax delay = new MinMax(1f, 2f);

	public Rigidbody parentRb;

	public GameObject seatedPilot;

	public GameObject hangingPilot;

	public GameObject canopyObject;

	public SolidBooster canopyBooster;

	public float canopyMass;

	public UnityEvent OnBegin;

	public float ejectDelay;

	public UnityEvent OnEjectedPilot;

	public float parachuteDelay;

	public SimpleDrag parachuteDrag;

	public Transform parachuteTransform;

	public float ejectionMass;

	public SolidBooster booster;

	private Rigidbody ejectedPilotRB;

	[ContextMenu("Eject")]
	public void BeginEjectSequence()
	{
		parachuteDrag.enabled = false;
		base.gameObject.SetActive(value: true);
		StartCoroutine(EjectRoutine());
	}

	private IEnumerator EjectRoutine()
	{
		yield return new WaitForSeconds(delay.Random());
		seatedPilot.SetActive(value: true);
		hangingPilot.SetActive(value: false);
		if (OnBegin != null)
		{
			OnBegin.Invoke();
		}
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(EjectCanopy);
		yield return new WaitForSeconds(ejectDelay);
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(EjectPilot);
		yield return new WaitForSeconds(parachuteDelay);
		seatedPilot.SetActive(value: false);
		hangingPilot.SetActive(value: true);
		parachuteDrag.enabled = true;
		float pd = parachuteDrag.area;
		parachuteDrag.SetDragArea(0f);
		float t = 0f;
		float deployRate = 0.5f;
		while (t < 1f)
		{
			t = Mathf.Clamp01(t + deployRate * Time.deltaTime);
			parachuteTransform.localScale = new Vector3(t, t, Mathf.Clamp01(t * 8f));
			parachuteDrag.SetDragArea(t * pd);
			yield return null;
		}
		Object.Destroy(base.gameObject, 30f);
		while (!ejectedPilotRB)
		{
			yield return null;
		}
		while (ejectedPilotRB.velocity.y > -1f)
		{
			yield return null;
		}
		while (ejectedPilotRB.velocity.y < -0.8f)
		{
			yield return null;
		}
		parachuteTransform.gameObject.SetActive(value: false);
		Object.Destroy(base.gameObject, 5f);
	}

	private void EjectPilot()
	{
		Rigidbody rigidbody = (ejectedPilotRB = base.gameObject.AddComponent<Rigidbody>());
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.mass = ejectionMass;
		rigidbody.angularDrag = 2f;
		base.transform.parent = null;
		base.gameObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rigidbody);
		rigidbody.velocity = parentRb.GetPointVelocity(base.transform.position);
		parachuteDrag.SetParentRigidbody(rigidbody);
		booster.SetParentRigidbody(rigidbody);
		booster.Fire();
		if (OnEjectedPilot != null)
		{
			OnEjectedPilot.Invoke();
		}
	}

	private void EjectCanopy()
	{
		if ((bool)canopyObject)
		{
			Rigidbody rigidbody = canopyObject.GetComponent<Rigidbody>();
			if (!rigidbody)
			{
				rigidbody = canopyObject.AddComponent<Rigidbody>();
				canopyObject.AddComponent<FloatingOriginTransform>().SetRigidbody(rigidbody);
			}
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.mass = canopyMass;
			rigidbody.isKinematic = false;
			canopyObject.transform.parent = null;
			if ((bool)parentRb)
			{
				rigidbody.velocity = parentRb.GetPointVelocity(canopyObject.transform.position);
			}
			if ((bool)canopyBooster)
			{
				canopyBooster.SetParentRigidbody(rigidbody);
				canopyBooster.Fire();
			}
			Object.Destroy(canopyObject, 10f);
		}
	}
}
