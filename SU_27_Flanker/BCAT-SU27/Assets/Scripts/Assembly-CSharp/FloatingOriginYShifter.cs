using UnityEngine;

public class FloatingOriginYShifter : MonoBehaviour
{
	private void Start()
	{
		if ((bool)FloatingOrigin.instance)
		{
			FloatingOrigin.instance.OnOriginShift += OnOriginShift;
		}
	}

	private void OnOriginShift(Vector3 offset)
	{
		base.transform.position += offset.y * Vector3.up;
	}
}
