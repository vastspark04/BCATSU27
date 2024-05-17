using UnityEngine;
using UnityEngine.UI;

public class HUDVelVector : MonoBehaviour
{
	private FlightInfo flightInfo;

	private Rigidbody rb;

	private Image img;

	private float depth;

	private Transform myTransform;

	private void Awake()
	{
		myTransform = base.transform;
	}

	private void Start()
	{
		img = GetComponent<Image>();
		flightInfo = GetComponentInParent<FlightInfo>();
		rb = flightInfo.rb;
		depth = GetComponentInParent<CollimatedHUDUI>().depth;
	}

	private void FixedUpdate()
	{
		if (flightInfo.airspeed > 0.5f)
		{
			Ray ray = new Ray(VRHead.instance.transform.position, rb.velocity);
			myTransform.position = ray.GetPoint(depth);
			myTransform.rotation = Quaternion.LookRotation(ray.direction, myTransform.parent.up);
			if (!img.enabled)
			{
				img.enabled = true;
			}
		}
		else if (img.enabled)
		{
			img.enabled = false;
			myTransform.localPosition = Vector3.zero;
		}
	}
}
