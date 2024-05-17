using UnityEngine;

public class HeliWingFlapper : MonoBehaviour
{
	public float flapFactor;

	public Wing[] sampleWings;

	public float maximumFlap = 3f;

	private Quaternion origRot;

	private Vector3 localAxis;

	public float currentFlap { get; private set; }

	private void Start()
	{
		origRot = base.transform.localRotation;
		localAxis = base.transform.parent.InverseTransformDirection(base.transform.right);
	}

	private void FixedUpdate()
	{
		Vector3 zero = Vector3.zero;
		for (int i = 0; i < sampleWings.Length; i++)
		{
			zero += sampleWings[i].liftVector;
		}
		float num2 = (currentFlap = Mathf.Clamp(Vector3.Dot(zero, base.transform.parent.up) * flapFactor, 0f - maximumFlap, maximumFlap));
		base.transform.localRotation = Quaternion.AngleAxis(0f - num2, localAxis) * origRot;
	}
}
