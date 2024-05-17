using UnityEngine;

[RequireComponent(typeof(Health))]
public class RescueObjective : AccumulativeObjective
{
	private IRescuable rescuable;

	private Health health;

	private bool rescued;

	private void Start()
	{
		rescuable = base.gameObject.GetComponentImplementing<IRescuable>();
		health = GetComponent<Health>();
		if (health.normalizedHealth == 0f)
		{
			Fail();
		}
		else
		{
			health.OnDeath.AddListener(Fail);
		}
	}

	private void Update()
	{
		if (!rescued && !base.done && rescuable.GetIsRescued())
		{
			rescued = true;
			AddCompleted();
		}
	}

	private void Fail()
	{
		AddFailed();
	}
}
