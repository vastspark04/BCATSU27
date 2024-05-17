using UnityEngine;

public class FloatingOriginShifter : MonoBehaviour
{
	public float threshold;

	private float sqrThreshold;

	private Transform myTransform;

	public Rigidbody rb;

	public static FloatingOriginShifter instance { get; private set; }

	private void OnEnable()
	{
		if ((bool)instance && instance != this)
		{
			instance.enabled = false;
		}
		instance = this;
		myTransform = base.transform;
	}

	private void Start()
	{
		sqrThreshold = threshold * threshold;
	}

	private void Update()
	{
		if (!rb && myTransform.position.sqrMagnitude > sqrThreshold)
		{
			FloatingOrigin.instance.ShiftOrigin(myTransform.position);
		}
	}

	private void FixedUpdate()
	{
		if ((bool)rb && rb.position.sqrMagnitude > sqrThreshold)
		{
			FloatingOrigin.instance.ShiftOrigin(rb.position);
		}
	}
}
