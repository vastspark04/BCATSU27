using System.Collections;

public class VTOMConditional : VTObjectiveModule
{
	public class VTOMConditionalBehaviour : MissionObjective
	{
		public ScenarioConditional successCondition;

		public ScenarioConditional failCondition;

		public override void OnBeginMission()
		{
			base.OnBeginMission();
			StartCoroutine(ConditionCheckRoutine());
		}

		private IEnumerator ConditionCheckRoutine()
		{
			for (int i = 0; i < 4; i++)
			{
				yield return null;
			}
			while (!base.objectiveFinished)
			{
				if (!QuicksaveManager.isQuickload || PlayerSpawn.qLoadPlayerComplete)
				{
					if (successCondition != null && successCondition.GetCondition())
					{
						CompleteObjective();
						if (base.isPlayersMission)
						{
							EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_completed}", red: false);
						}
					}
					else if (failCondition != null && failCondition.GetCondition())
					{
						FailObjective();
						if (base.isPlayersMission)
						{
							EndMission.AddText($"{objectiveName} {VTLStaticStrings.mission_failed}", red: true);
						}
					}
				}
				yield return null;
			}
		}
	}

	[UnitSpawn("Success Condition")]
	public ScenarioConditional successConditional;

	[UnitSpawn("Fail Condition")]
	public ScenarioConditional failConditional;

	protected override void SetupMonobehaviour()
	{
		VTOMConditionalBehaviour vTOMConditionalBehaviour = (VTOMConditionalBehaviour)(base.objectiveBehaviour = objectiveObject.AddComponent<VTOMConditionalBehaviour>());
		vTOMConditionalBehaviour.successCondition = successConditional;
		vTOMConditionalBehaviour.failCondition = failConditional;
	}

	public override string GetDescription()
	{
		return "Objective is completed when [Success Condition] returns true.  It is failed if there is a [Fail Condition] and it returns true.";
	}

	public override bool IsConfigurationComplete()
	{
		return true;
	}
}
