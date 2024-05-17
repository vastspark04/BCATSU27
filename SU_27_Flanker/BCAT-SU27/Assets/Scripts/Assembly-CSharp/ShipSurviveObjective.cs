using System.Collections;
using UnityEngine;

public class ShipSurviveObjective : MissionObjective
{
	private Health h;

	private EjectionSeat e;

	protected override void Awake()
	{
		base.Awake();
		h = GetComponentInParent<Health>();
		h.OnDeath.AddListener(OnDeath);
		EjectionSeat componentInChildren = GetComponentInChildren<EjectionSeat>(includeInactive: true);
		if ((bool)componentInChildren)
		{
			componentInChildren.OnEject.AddListener(OnDeathDelayed);
		}
	}

	private void OnDeath()
	{
		if (base.isPlayersMission)
		{
			EndMission.AddText(VTLStaticStrings.mission_vehicleDestroyed, red: true);
		}
		FailObjective();
	}

	private void OnDestroy()
	{
		if ((bool)h)
		{
			h.OnDeath.RemoveListener(OnDeath);
		}
		if ((bool)e)
		{
			e.OnEject.RemoveListener(OnDeathDelayed);
		}
	}

	private void OnDeathDelayed()
	{
		StartCoroutine(EjectDeath());
	}

	private IEnumerator EjectDeath()
	{
		yield return new WaitForSeconds(5f);
		if (!base.failed)
		{
			EndMission.AddText(VTLStaticStrings.mission_ejected, red: true);
			FailObjective();
		}
	}

	public override string GetObjectiveTitle()
	{
		return "Survive the mission.";
	}
}
