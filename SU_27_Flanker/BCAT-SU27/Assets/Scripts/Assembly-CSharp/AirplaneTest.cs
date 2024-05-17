using UnityEngine;

public class AirplaneTest : MonoBehaviour
{
	public float speed;

	public float altitude;

	public float turnRate;

	public float tiltToTurnRatio;

	private Rigidbody rb;

	private bool dead;

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		rb.rotation = Quaternion.AngleAxis(Mathf.Clamp((0f - tiltToTurnRatio) * turnRate, -90f, 90f), base.transform.forward) * rb.rotation;
	}

	private void FixedUpdate()
	{
		if (!dead)
		{
			Quaternion rot = Quaternion.AngleAxis(turnRate * Time.fixedDeltaTime, Vector3.up) * rb.rotation;
			Vector3 position = new Vector3(rb.position.x, WaterPhysics.instance.height + altitude, rb.position.z);
			position += speed * rb.transform.forward * Time.fixedDeltaTime;
			rb.MovePosition(position);
			rb.MoveRotation(rot);
		}
	}

	public void Die()
	{
		rb.isKinematic = false;
		rb.velocity = base.transform.forward * speed;
		dead = true;
	}
}
