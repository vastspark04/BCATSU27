using UnityEngine;

public class ShipRocker : MonoBehaviour
{
	public float xRate;

	public float yRate;

	public float xAmplitude;

	public float yAmplitude;

	private Rigidbody rb;

	private Quaternion startRot;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		startRot = rb.rotation;
	}

	private void Update()
	{
		float x = Mathf.Sin(xRate * Time.time) * xAmplitude;
		float z = Mathf.Sin(yRate * Time.time) * yAmplitude;
		Quaternion quaternion = Quaternion.Euler(x, 0f, z);
		rb.MoveRotation(startRot * quaternion);
	}
}
