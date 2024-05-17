using UnityEngine;

public class WaterBuoyancy : MonoBehaviour, IParentRBDependent
{
	public float drag;

	public float buoyancy;

	public float impactDamageFactor;

	private Rigidbody rb;

	private bool splashed;

	public Health health;

	private bool started;

	private Transform myTransform;

	public void SetParentRigidbody(Rigidbody prb)
	{
		rb = prb;
		if ((bool)prb)
		{
			base.enabled = true;
		}
		WaterBuoyancyMaster waterBuoyancyMaster = GetComponentInParent<WaterBuoyancyMaster>();
		if (!waterBuoyancyMaster)
		{
			waterBuoyancyMaster = base.transform.parent.gameObject.AddComponent<WaterBuoyancyMaster>();
		}
		waterBuoyancyMaster.bPointsDirty = true;
	}

	private void Awake()
	{
		myTransform = base.transform;
		rb = GetComponentInParent<Rigidbody>();
		if (!rb)
		{
			base.enabled = false;
		}
		else if (!GetComponentInParent<WaterBuoyancyMaster>())
		{
			base.transform.parent.gameObject.AddComponent<WaterBuoyancyMaster>();
		}
	}

	private void Start()
	{
		started = true;
	}

	private void FixedUpdate()
	{
		if (!WaterPhysics.instance)
		{
			return;
		}
		if (started && myTransform.position.y < WaterPhysics.instance.height)
		{
			if (!rb)
			{
				base.enabled = false;
				return;
			}
			if (!rb.isKinematic)
			{
				Vector3 pointVelocity = rb.GetPointVelocity(myTransform.position);
				Vector3 vector = -pointVelocity * pointVelocity.magnitude * drag;
				Vector3 vector2 = Vector3.up * (WaterPhysics.instance.height - myTransform.position.y) * buoyancy;
				rb.AddForceAtPosition(vector + vector2, myTransform.position);
			}
			if (!splashed)
			{
				splashed = true;
				if ((bool)health)
				{
					health.Damage(rb.velocity.magnitude * impactDamageFactor, rb.worldCenterOfMass, Health.DamageTypes.Impact, null, "(Crashed into water)");
				}
				Vector3 position = myTransform.position;
				position.y = WaterPhysics.instance.height;
				BulletHitManager.instance.CreateSplash(position);
			}
		}
		else
		{
			splashed = false;
		}
	}
}
