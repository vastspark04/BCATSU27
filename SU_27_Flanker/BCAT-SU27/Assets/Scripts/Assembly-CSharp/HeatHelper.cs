using UnityEngine;

public class HeatHelper : MonoBehaviour
{
	private static HeatHelper instance;

	public Light sun;

	private void Awake()
	{
		instance = this;
	}

	public static Vector3 GetSunDirection()
	{
		return instance.sun.transform.forward;
	}
}
