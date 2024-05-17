using UnityEngine;

[RequireComponent(typeof(Health))]
public class PickupObjective : AccumulativeObjective
{
	private IPickupableUnit pickupable;

	private Health health;

	private bool complete;

	private void Start()
	{
		pickupable = base.gameObject.GetComponentImplementing<IPickupableUnit>();
		health = GetComponent<Health>();
		if (health.normalizedHealth == 0f)
		{
			AddFailed();
		}
		else
		{
			health.OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDeath()
	{
		AddFailed();
	}

	private void Update()
	{
		if (!complete && pickupable.GetWasPickedUp())
		{
			complete = true;
			AddCompleted();
		}
	}
}
