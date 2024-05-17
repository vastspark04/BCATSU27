using UnityEngine;

public class OmniWing : MonoBehaviour, IParentRBDependent
{
	public float liftCoefficient;

	public float dragCoefficient;

	public float liftArea;

	public Rigidbody rb;

	private Transform wingTransform;

	public Aerodynamics aeroProfile;

	private float liftConstant;

	private float dragConstant;

	private Vector3 rbOffset;

	private WindMaster windMaster;

	public Color gizmoColor = new Color(1f, 0.5f, 0.25f, 0.25f);

	private void Start()
	{
		wingTransform = base.transform;
		if (aeroProfile == null)
		{
			aeroProfile = AerodynamicsController.fetch.wingAero;
		}
		liftConstant = 0.5f * liftCoefficient * liftArea;
		dragConstant = 0.5f * dragCoefficient * liftArea;
	}

	private void OnDrawGizmos()
	{
		Vector3 position = base.transform.position;
		Gizmos.color = gizmoColor;
		Gizmos.DrawSphere(position, 0.5f);
	}

	private void FixedUpdate()
	{
		if (!rb || rb.isKinematic)
		{
			return;
		}
		Vector3 vector = rb.position + rb.transform.TransformVector(rbOffset);
		float num = AerodynamicsController.fetch.AtmosDensityAtPosition(vector);
		Vector3 pointVelocity = rb.GetPointVelocity(vector);
		Vector3 normalized = Vector3.Project(pointVelocity, wingTransform.forward).normalized;
		if (WindVolumes.windEnabled)
		{
			if (!windMaster)
			{
				windMaster = rb.gameObject.GetComponent<WindMaster>();
				if (!windMaster)
				{
					windMaster = rb.gameObject.AddComponent<WindMaster>();
				}
			}
			pointVelocity -= windMaster.wind;
		}
		float sqrMagnitude = pointVelocity.sqrMagnitude;
		float time = Vector3.Angle(normalized, pointVelocity);
		float num2 = num * sqrMagnitude;
		float num3 = liftConstant * num2 * aeroProfile.liftCurve.Evaluate(time);
		float num4 = dragConstant * num2 * aeroProfile.dragCurve.Evaluate(time) * AerodynamicsController.fetch.DragMultiplierAtSpeed(Mathf.Sqrt(sqrMagnitude), vector);
		Vector3 vector2 = -Vector3.ProjectOnPlane(pointVelocity, normalized).normalized;
		Vector3 force = (0f - num4) * pointVelocity.normalized + vector2 * num3;
		rb.AddForceAtPosition(force, vector);
	}

	public float CalculateDragMagAtSpeed(float speed)
	{
		return 0.5f * dragCoefficient * liftArea * 0.021f * speed * speed * aeroProfile.dragCurve.Evaluate(0f);
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
		rbOffset = rb.transform.InverseTransformPoint(base.transform.position);
		windMaster = rb.gameObject.GetComponent<WindMaster>();
		if (!windMaster)
		{
			windMaster = rb.gameObject.AddComponent<WindMaster>();
		}
	}
}
