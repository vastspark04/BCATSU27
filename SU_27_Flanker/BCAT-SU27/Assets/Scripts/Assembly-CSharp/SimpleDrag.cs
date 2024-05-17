using UnityEngine;

public class SimpleDrag : MonoBehaviour, IParentRBDependent
{
	public float area;

	public Vector3 offsetFromCoM;

	public Rigidbody rb;

	private WindMaster windMaster;

	private void Start()
	{
		OnStart();
	}

	protected virtual void OnStart()
	{
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)rb)
		{
			Gizmos.DrawSphere(rb.transform.TransformPoint(rb.centerOfMass + offsetFromCoM), 0.1f);
		}
		else
		{
			Gizmos.DrawSphere(base.transform.TransformPoint(offsetFromCoM), 0.1f);
		}
	}

	private void FixedUpdate()
	{
		if (!rb || rb.isKinematic || !(area > 0f))
		{
			return;
		}
		Vector3 vector = rb.worldCenterOfMass + rb.transform.TransformVector(offsetFromCoM);
		Vector3 pointVelocity = rb.GetPointVelocity(vector);
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
		float magnitude = pointVelocity.magnitude;
		float num = CalculateDragForceMagnitude(magnitude);
		OnApplyDrag(num * magnitude);
		Vector3 force = -pointVelocity * num;
		if (!float.IsNaN(force.x))
		{
			rb.AddForceAtPosition(force, vector);
		}
	}

	protected virtual float CalculateDragForceMagnitude(float spd)
	{
		return 0.5f * AerodynamicsController.fetch.AtmosDensityAtPosition(base.transform.position) * area * spd * AerodynamicsController.fetch.DragMultiplierAtSpeed(spd, base.transform.position);
	}

	public float CalculateDragForceMagnitudeAtSeaLevel(float speed)
	{
		return 0.0105f * area * speed * speed;
	}

	protected virtual void OnApplyDrag(float dragMagnitude)
	{
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
		windMaster = rb.gameObject.GetComponent<WindMaster>();
		if (!windMaster)
		{
			windMaster = rb.gameObject.AddComponent<WindMaster>();
		}
	}

	public void SetDragArea(float dragArea)
	{
		area = dragArea;
	}

	public void SetZOffset(float offset)
	{
		offsetFromCoM.z = offset;
	}
}
