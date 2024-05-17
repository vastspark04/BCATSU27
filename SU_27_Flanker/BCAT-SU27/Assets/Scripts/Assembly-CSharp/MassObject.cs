using UnityEngine;

public class MassObject : MonoBehaviour, IMassObject
{
	public float mass;

	public float GetMass()
	{
		return mass;
	}
}
