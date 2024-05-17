using System.Collections;
using UnityEngine;

public class MissileFairing : MonoBehaviour, IParentRBDependent
{
	private Rigidbody parentRB;

	public Collider collider;

	public Vector3 localJettisonVelocity;

	public Vector3 localJettisonTorque;

	public SimpleDrag drag;

	public float lifetime = 6f;

	private bool hasJetted;

	public void Jettison()
	{
		if (!hasJetted)
		{
			hasJetted = true;
			FloatingOrigin.instance.AddQueuedFixedUpdateAction(OnJettison);
		}
	}

	private void OnJettison()
	{
		Rigidbody rigidbody = base.gameObject.AddComponent<Rigidbody>();
		rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
		rigidbody.mass = 0.001f;
		if ((bool)parentRB)
		{
			rigidbody.velocity = parentRB.velocity;
		}
		rigidbody.velocity += base.transform.TransformVector(localJettisonVelocity);
		collider.enabled = true;
		drag.rb = rigidbody;
		drag.enabled = true;
		rigidbody.AddRelativeTorque(localJettisonTorque, ForceMode.Impulse);
		base.gameObject.AddComponent<FloatingOriginTransform>().rb = rigidbody;
		StartCoroutine(LifetimeRoutine());
	}

	private IEnumerator LifetimeRoutine()
	{
		yield return new WaitForSeconds(lifetime);
		Object.Destroy(base.gameObject);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		parentRB = rb;
	}
}
