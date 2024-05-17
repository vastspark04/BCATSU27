using UnityEngine;

public class ControlSurface : MonoBehaviour
{
	public float maxDeflection;

	public float actuatorSpeed;

	public bool invert;

	public float AoAFactor;

	private float def;

	private Rigidbody rb;

	private void Start()
	{
		rb = GetComponentInParent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		float num = def * maxDeflection;
		if (invert)
		{
			num = 0f - num;
		}
		Quaternion to = Quaternion.Euler(num, 0f, 0f);
		base.transform.localRotation = Quaternion.RotateTowards(base.transform.localRotation, to, actuatorSpeed * Time.fixedDeltaTime);
	}

	public void SetDeflection(float deflection)
	{
		deflection = Mathf.Clamp(deflection, -1f, 1f);
		def = deflection;
	}

	public void SetAoA(float normAoA)
	{
		Vector3 vector = base.transform.parent.InverseTransformVector(rb.velocity);
		vector.x = 0f;
		float num = Vector3.Angle(vector, Vector3.forward) * Mathf.Sign(Vector3.Dot(vector, Vector3.up)) / maxDeflection;
		if (rb.velocity.sqrMagnitude < 1f)
		{
			num = 0f;
		}
		SetDeflection(AoAFactor * num + normAoA);
	}
}
