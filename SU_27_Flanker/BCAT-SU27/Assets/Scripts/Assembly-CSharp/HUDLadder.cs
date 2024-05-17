using UnityEngine;
using UnityEngine.UI;

public class HUDLadder : MonoBehaviour
{
	public int ladderDegreesInterval;

	public float ladderSpacing;

	public float offset;

	public Transform vVectorTransform;

	public Transform ladderTransform;

	private FlightInfo flightInfo;

	public GameObject climbLadderTemplate;

	public GameObject descendLadderTemplate;

	private float pitchFactor;

	private Rigidbody rb;

	public bool constructLadder = true;

	private void Awake()
	{
		rb = GetComponentInParent<Rigidbody>();
		flightInfo = rb.GetComponentInChildren<FlightInfo>();
		if (!ladderTransform)
		{
			ladderTransform = base.transform;
		}
	}

	private void Start()
	{
		if (constructLadder)
		{
			ConstructLadder();
		}
		pitchFactor = ladderSpacing / (float)ladderDegreesInterval;
	}

	private void ConstructLadder()
	{
		int i = ladderDegreesInterval;
		int num = 1;
		for (; i <= 90; i += ladderDegreesInterval)
		{
			GameObject obj = Object.Instantiate(climbLadderTemplate);
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			obj.transform.localPosition = Vector3.up * ladderSpacing * num;
			obj.GetComponentInChildren<Text>().text = i.ToString();
			GameObject obj2 = Object.Instantiate(descendLadderTemplate);
			obj2.transform.SetParent(base.transform, worldPositionStays: false);
			obj2.transform.localPosition = Vector3.down * ladderSpacing * num;
			obj2.GetComponentInChildren<Text>().text = i.ToString();
			num++;
		}
		climbLadderTemplate.SetActive(value: false);
		descendLadderTemplate.SetActive(value: false);
	}

	private void LateUpdate()
	{
		ladderTransform.localPosition = Vector3.zero;
		Vector3 vector = Vector3.ProjectOnPlane(Vector3.up, rb.transform.forward);
		float num = Vector3.Angle(rb.transform.up, vector);
		num *= Mathf.Sign(Vector3.Dot(rb.transform.right, vector));
		ladderTransform.localRotation = Quaternion.Euler(0f, 0f, 0f - num);
		float num2 = Vector3.Angle(rb.transform.forward, Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up));
		num2 *= Mathf.Sign(Vector3.Dot(rb.transform.forward, Vector3.up));
		num2 += offset;
		Vector3 zero = Vector3.zero;
		zero += (0f - pitchFactor) * num2 * ladderTransform.parent.InverseTransformDirection(base.transform.up);
		ladderTransform.localPosition = zero;
		if (flightInfo.airspeed > 50f)
		{
			Vector3 vector2 = vVectorTransform.position - ladderTransform.parent.position;
			vector2 = Vector3.Project(vector2, ladderTransform.right);
			ladderTransform.position += vector2;
		}
	}
}
