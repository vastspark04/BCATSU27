using UnityEngine;

public class AccumulativeObjective : MonoBehaviour
{
	public string objectiveID;

	protected AccumulativeMission mission;

	protected bool done { get; private set; }

	public void SetMission(AccumulativeMission m)
	{
		mission = m;
	}

	protected void AddCompleted()
	{
		if (!done)
		{
			if ((bool)mission)
			{
				mission.AddCompleted(this);
			}
			done = true;
		}
	}

	protected void AddFailed()
	{
		if (!done)
		{
			if ((bool)mission)
			{
				mission.AddFailed(this);
			}
			done = true;
		}
	}

	public string GetObjectiveID()
	{
		if (string.IsNullOrEmpty(objectiveID))
		{
			return base.gameObject.name;
		}
		return objectiveID;
	}
}
