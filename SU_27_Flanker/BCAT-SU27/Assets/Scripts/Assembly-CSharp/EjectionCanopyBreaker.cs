using System;
using UnityEngine;

public class EjectionCanopyBreaker : MonoBehaviour
{
	[Serializable]
	public class DebrisObject
	{
		public GameObject gameObject;

		public Vector3 ejectDirection;

		public float mass;

		public float drag = 0.001f;

		public float ejectSpeed;

		public float ejectTorque;

		public bool drawGizmo;
	}

	public Rigidbody parentRb;

	public ParticleSystem[] detCoordParticles;

	public DebrisObject[] debrisObjects;

	public GameObject[] enableOnFire;

	public GameObject[] disableOnFire;

	public void Fire()
	{
		enableOnFire.SetActive(active: true);
		disableOnFire.SetActive(active: false);
		for (int i = 0; i < debrisObjects.Length; i++)
		{
			DebrisObject debrisObject = debrisObjects[i];
			Rigidbody rigidbody = debrisObject.gameObject.AddComponent<Rigidbody>();
			rigidbody.mass = debrisObject.mass;
			rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			rigidbody.velocity = parentRb.GetPointVelocity(rigidbody.position) + debrisObject.gameObject.transform.TransformDirection(debrisObject.ejectDirection).normalized * debrisObject.ejectSpeed;
			rigidbody.AddTorque(UnityEngine.Random.onUnitSphere * debrisObject.ejectTorque, ForceMode.Impulse);
			SimpleDrag simpleDrag = rigidbody.gameObject.AddComponent<SimpleDrag>();
			simpleDrag.SetParentRigidbody(rigidbody);
			simpleDrag.SetDragArea(debrisObject.drag);
		}
		detCoordParticles.Play();
	}

	private void OnDestroy()
	{
		DebrisObject[] array = debrisObjects;
		foreach (DebrisObject debrisObject in array)
		{
			if (debrisObject != null && (bool)debrisObject.gameObject)
			{
				UnityEngine.Object.Destroy(debrisObject.gameObject);
			}
		}
	}
}
