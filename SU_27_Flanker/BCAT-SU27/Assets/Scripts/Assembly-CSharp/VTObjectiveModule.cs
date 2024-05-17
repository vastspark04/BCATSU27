using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public abstract class VTObjectiveModule
{
	public VTObjective objective;

	protected GameObject objectiveObject;

	public MissionObjective objectiveBehaviour { get; protected set; }

	public void DestroyObject()
	{
		if (objectiveObject != null)
		{
			Object.Destroy(objectiveObject);
		}
	}

	public void SetupObjective()
	{
		if (objectiveObject != null)
		{
			Debug.Log("Tried setting up objective " + objective.objectiveName + " but it was already setup!");
			return;
		}
		Debug.Log("Setting up objective " + objective.objectiveName);
		objectiveObject = new GameObject(objective.objectiveName + objective.objectiveID);
		SetupMonobehaviour();
		if (!objectiveBehaviour)
		{
			Debug.LogError("An objective module of type " + GetType().ToString() + " did not generate a runtime MissionObjective component.");
			return;
		}
		objectiveBehaviour.objectiveID = objective.objectiveID;
		objectiveBehaviour.beginOnStart = false;
		objectiveBehaviour.isFinalMission = false;
		objectiveBehaviour.autoSetWaypointOnStart = objective.autoSetWaypoint;
		objectiveBehaviour.completionBudgetAward = objective.completionReward;
		objectiveBehaviour.objectiveName = objective.objectiveName;
		objectiveBehaviour.info = objective.objectiveInfo;
		objectiveBehaviour.required = objective.required;
		if (objective.waypoint != null && objective.waypoint.GetTransform() != null)
		{
			objectiveBehaviour.waypoint = objective.waypoint;
			objectiveBehaviour.waypointTransform = objective.waypoint.GetTransform();
		}
		if (VTScenario.isScenarioHost)
		{
			objectiveBehaviour.OnBegin = new UnityEvent();
			objectiveBehaviour.OnBegin.AddListener(objective.startEvent.Invoke);
			objectiveBehaviour.OnComplete = new UnityEvent();
			objectiveBehaviour.OnComplete.AddListener(objective.completeEvent.Invoke);
			objectiveBehaviour.OnFail = new UnityEvent();
			objectiveBehaviour.OnFail.AddListener(objective.failedEvent.Invoke);
			if (VTOLMPUtils.IsMultiplayer())
			{
				objectiveBehaviour.OnBegin.AddListener(MP_OnObjectiveBegin);
				objectiveBehaviour.OnComplete.AddListener(MP_OnObjectiveComplete);
				objectiveBehaviour.OnFail.AddListener(MP_OnObjectiveFail);
			}
		}
		objectiveBehaviour.team = objective.team;
	}

	private void MP_OnObjectiveBegin()
	{
		VTOLMPEventsManager.instance.ReportObjectiveStart(objective.objectiveID);
	}

	private void MP_OnObjectiveComplete()
	{
		VTOLMPEventsManager.instance.ReportObjectiveComplete(objective.objectiveID);
	}

	private void MP_OnObjectiveFail()
	{
		VTOLMPEventsManager.instance.ReportObjectiveFail(objective.objectiveID);
	}

	protected virtual void SetupMonobehaviour()
	{
	}

	public virtual string GetDescription()
	{
		return "TODO: Objective description.";
	}

	public virtual bool IsConfigurationComplete()
	{
		return false;
	}
}
