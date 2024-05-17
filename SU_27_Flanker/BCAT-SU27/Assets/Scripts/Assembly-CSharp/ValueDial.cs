using UnityEngine;

[RequireComponent(typeof(RotationToggle))]
public class ValueDial : MonoBehaviour
{
	private RotationToggle rotator;

	public float value;

	public float minValue;

	public float maxValue;

	private float diff;

	private void Start()
	{
		rotator = GetComponent<RotationToggle>();
		rotator.manual = true;
		diff = maxValue - minValue;
	}

	private void Update()
	{
		float normalizedRotation = (value - minValue) / diff;
		rotator.SetNormalizedRotation(normalizedRotation);
	}
}
