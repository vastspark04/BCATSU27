using UnityEngine;

public class MovingPlatform : MonoBehaviour, IParentRBDependent
{
	public Rigidbody rb;

	public Vector3 GetVelocity(Vector3 worldPoint)
	{
		if ((bool)rb)
		{
			return rb.GetPointVelocity(worldPoint);
		}
		return Vector3.zero;
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
	}
}
