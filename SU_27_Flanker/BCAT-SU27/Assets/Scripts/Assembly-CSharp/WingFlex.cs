using UnityEngine;

public class WingFlex : MonoBehaviour
{
	public Wing wingAero;

	public float flexFactor;

	public Vector3 axis;

	private Vector3 localAxis;

	private Quaternion origRotation;

	private Transform myTransform;

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void Start()
	{
		origRotation = base.transform.localRotation;
		localAxis = base.transform.parent.InverseTransformDirection(base.transform.TransformDirection(axis));
	}

	private void Update()
	{
		myTransform.localRotation = Quaternion.AngleAxis(wingAero.currentLiftForce * flexFactor, localAxis) * origRotation;
	}
}
