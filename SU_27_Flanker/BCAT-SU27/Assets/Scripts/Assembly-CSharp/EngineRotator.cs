using UnityEngine;

public class EngineRotator : MonoBehaviour
{
	public ModuleEngine engine;

	public Transform rotationTransform;

	public Vector3 axis;

	public float lerpRate = 1f;

	public float minSpeed;

	public float maxSpeed;

	private float currentSpeed;

	private LODBase lodBase;

	private void Awake()
	{
		if (!rotationTransform)
		{
			rotationTransform = base.transform;
		}
		axis = rotationTransform.parent.InverseTransformDirection(rotationTransform.TransformDirection(axis));
		lodBase = GetComponentInParent<LODBase>();
	}

	private void Update()
	{
		if (!lodBase || !(lodBase.sqrDist > 1000f))
		{
			float b = 0f;
			if (engine.engineEnabled && !engine.failed)
			{
				b = Mathf.Clamp(maxSpeed * engine.finalThrottle, minSpeed, maxSpeed);
			}
			currentSpeed = Mathf.Lerp(currentSpeed, b, lerpRate * Time.deltaTime);
			if (currentSpeed > 0.01f)
			{
				Quaternion quaternion = Quaternion.AngleAxis(currentSpeed * Time.deltaTime, axis);
				rotationTransform.localRotation = quaternion * rotationTransform.localRotation;
			}
		}
	}
}
