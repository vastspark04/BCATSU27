using UnityEngine;

[RequireComponent(typeof(Health))]
public class KillObjective : AccumulativeObjective
{
	private void Start()
	{
		Health component = GetComponent<Health>();
		if (component.normalizedHealth <= 0f)
		{
			AddCompleted();
		}
		else
		{
			component.OnDeath.AddListener(H_OnDeath);
		}
	}

	private void H_OnDeath()
	{
		AddCompleted();
	}

	public void ForceCheckCompleted()
	{
		if (GetComponent<Health>().normalizedHealth <= 0f)
		{
			AddCompleted();
		}
	}
}
