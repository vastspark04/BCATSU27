using System;
using UnityEngine;

public class ProtectObjective : MissionObjective
{
	public float radius;

	public Actor protectActor;

	public VTOMDefendUnit.DefendCompletionModes completionMode;

	private Health health;

	public override void OnBeginMission()
	{
		base.OnBeginMission();
		if (!protectActor)
		{
			if (base.isPlayersMission)
			{
				EndMission.AddFailText("A protect objective is missing a target.");
			}
			FailObjective();
			return;
		}
		health = protectActor.GetComponentInParent<Health>();
		if (completionMode == VTOMDefendUnit.DefendCompletionModes.Waypoint && !waypointTransform)
		{
			if (base.isPlayersMission)
			{
				EndMission.AddFailText("Protect objective for " + protectActor.actorName + " is missing waypoint.");
			}
			FailObjective();
		}
	}

	private void Update()
	{
		if (!base.started || base.objectiveFinished || (QuicksaveManager.isQuickload && !PlayerSpawn.qLoadPlayerComplete))
		{
			return;
		}
		if (health.normalizedHealth <= 0f)
		{
			if (base.isPlayersMission)
			{
				string empty = string.Empty;
				try
				{
					empty = string.Format(VTLStaticStrings.mission_failedToDefend, protectActor.actorName);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("Failed to format localized text for protect objective: {0}\n{1}", VTLStaticStrings.mission_failedToDefend, ex);
					empty = $"Failed to defend {protectActor.actorName}";
				}
				EndMission.AddFailText(empty);
			}
			FailObjective();
		}
		else if (completionMode == VTOMDefendUnit.DefendCompletionModes.Waypoint && (protectActor.position - waypointTransform.position).sqrMagnitude < radius * radius)
		{
			string s;
			try
			{
				s = string.Format(VTLStaticStrings.mission_hasReached, protectActor.actorName, waypointTransform.gameObject.name);
			}
			catch (Exception ex2)
			{
				Debug.LogErrorFormat("Failed to format text for waypoint transform: {0}\n{1}", VTLStaticStrings.mission_hasReached, ex2);
				s = $"{protectActor.actorName} has reached {waypointTransform.gameObject.name}.";
			}
			if (base.isPlayersMission)
			{
				EndMission.AddText(s, red: false);
			}
			CompleteObjective();
		}
	}
}
