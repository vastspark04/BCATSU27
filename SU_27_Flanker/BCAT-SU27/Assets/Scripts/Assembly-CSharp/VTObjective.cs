using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class VTObjective
{
	public enum StartModes
	{
		Immediate,
		Triggered,
		PreReqs,
		Final
	}

	public enum ObjectiveTypes
	{
		Destroy,
		Pick_Up,
		Drop_Off,
		Fly_To,
		Join,
		Land,
		Refuel,
		Protect,
		Conditional
	}

	public const string NODE_NAME = "Objective";

	public string objectiveName = "New Objective";

	public string objectiveInfo;

	public int objectiveID;

	public int orderID;

	public bool required = true;

	public float completionReward;

	public Waypoint waypoint;

	public bool autoSetWaypoint;

	public Teams team;

	public StartModes startMode;

	public ObjectiveTypes objectiveType;

	public VTEventInfo startEvent;

	public VTEventInfo completeEvent;

	public VTEventInfo failedEvent;

	public Dictionary<string, string> fields;

	public List<int> preReqObjectives;

	public VTObjectiveModule module { get; private set; }

	public VTObjective()
	{
		startEvent = new VTEventInfo();
		startEvent.eventName = "Start Event";
		completeEvent = new VTEventInfo();
		completeEvent.eventName = "Completed Event";
		failedEvent = new VTEventInfo();
		failedEvent.eventName = "Failed Event";
		fields = new Dictionary<string, string>();
		preReqObjectives = new List<int>();
		CreateModule();
	}

	public void SaveToParentNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("Objective");
		node.AddNode(configNode);
		configNode.SetValue("objectiveName", objectiveName);
		configNode.SetValue("objectiveInfo", objectiveInfo);
		configNode.SetValue("objectiveID", objectiveID);
		configNode.SetValue("orderID", orderID);
		configNode.SetValue("required", required);
		configNode.SetValue("completionReward", completionReward);
		configNode.SetValue("waypoint", VTSConfigUtils.WriteObject(typeof(Waypoint), waypoint));
		configNode.SetValue("autoSetWaypoint", autoSetWaypoint);
		configNode.SetValue("startMode", startMode);
		configNode.SetValue("objectiveType", objectiveType);
		ConfigNode node2 = new ConfigNode("startEvent");
		configNode.AddNode(node2);
		startEvent.SaveToNode(node2);
		ConfigNode node3 = new ConfigNode("failEvent");
		configNode.AddNode(node3);
		failedEvent.SaveToNode(node3);
		ConfigNode node4 = new ConfigNode("completeEvent");
		configNode.AddNode(node4);
		completeEvent.SaveToNode(node4);
		WriteModuleDataToDict();
		ConfigNode configNode2 = new ConfigNode("fields");
		configNode.AddNode(configNode2);
		foreach (string key in fields.Keys)
		{
			configNode2.SetValue(key, fields[key]);
		}
		if (startMode == StartModes.PreReqs)
		{
			configNode.SetValue("preReqObjectives", ConfigNodeUtils.WriteList(preReqObjectives));
		}
	}

	public void LoadFromNode(ConfigNode oNode)
	{
		objectiveName = oNode.GetValue("objectiveName");
		objectiveInfo = oNode.GetValue("objectiveInfo");
		objectiveID = ConfigNodeUtils.ParseInt(oNode.GetValue("objectiveID"));
		if (oNode.HasValue("orderID"))
		{
			orderID = oNode.GetValue<int>("orderID");
		}
		else
		{
			orderID = objectiveID;
		}
		required = ConfigNodeUtils.ParseBool(oNode.GetValue("required"));
		completionReward = ConfigNodeUtils.ParseFloat(oNode.GetValue("completionReward"));
		waypoint = VTSConfigUtils.ParseObject<Waypoint>(oNode.GetValue("waypoint"));
		autoSetWaypoint = ConfigNodeUtils.ParseBool(oNode.GetValue("autoSetWaypoint"));
		startMode = ConfigNodeUtils.ParseEnum<StartModes>(oNode.GetValue("startMode"));
		objectiveType = ConfigNodeUtils.ParseEnum<ObjectiveTypes>(oNode.GetValue("objectiveType"));
		CreateModule();
		ConfigNode node = oNode.GetNode("startEvent");
		startEvent.LoadFromInfoNode(node.GetNode("EventInfo"));
		ConfigNode node2 = oNode.GetNode("failEvent");
		failedEvent.LoadFromInfoNode(node2.GetNode("EventInfo"));
		ConfigNode node3 = oNode.GetNode("completeEvent");
		completeEvent.LoadFromInfoNode(node3.GetNode("EventInfo"));
		foreach (ConfigNode.ConfigValue value in oNode.GetNode("fields").GetValues())
		{
			fields.Add(value.name, value.value);
		}
		ParseFieldsDictToModule();
		if (startMode == StartModes.PreReqs && oNode.HasValue("preReqObjectives"))
		{
			preReqObjectives = ConfigNodeUtils.ParseList<int>(oNode.GetValue("preReqObjectives"));
		}
	}

	public void BeginScenario()
	{
		if (startMode == StartModes.Immediate && !QuicksaveManager.isQuickload)
		{
			BeginObjective();
		}
		else if (startMode == StartModes.Final)
		{
			startMode = StartModes.PreReqs;
			preReqObjectives = new List<int>();
			Debug.Log("Converting final objective to a prereq objective.");
			foreach (VTObjective objective in VTScenario.current.objectives.GetObjectives(team))
			{
				if (objective.required && objective.objectiveID != objectiveID)
				{
					Debug.Log(" - adding " + objective.objectiveName + " as a prereq");
					preReqObjectives.Add(objective.objectiveID);
				}
			}
		}
		if (startMode == StartModes.PreReqs)
		{
			MissionManager instance = MissionManager.instance;
			instance.OnMissionUpdated = (UnityAction)Delegate.Combine(instance.OnMissionUpdated, new UnityAction(OnMissionsUpdated));
		}
	}

	public void Dispose()
	{
		module.DestroyObject();
		if (startMode == StartModes.PreReqs && (bool)MissionManager.instance)
		{
			MissionManager instance = MissionManager.instance;
			instance.OnMissionUpdated = (UnityAction)Delegate.Remove(instance.OnMissionUpdated, new UnityAction(OnMissionsUpdated));
		}
	}

	public void SetObjectiveType(ObjectiveTypes t)
	{
		if (objectiveType != t)
		{
			objectiveType = t;
			CreateModule();
		}
	}

	private void CreateModule()
	{
		switch (objectiveType)
		{
		case ObjectiveTypes.Destroy:
			module = new VTOMKillMission();
			break;
		case ObjectiveTypes.Fly_To:
			module = new VTOMFlyTo();
			break;
		case ObjectiveTypes.Join:
			module = new VTOMJoinUnit();
			break;
		case ObjectiveTypes.Pick_Up:
			module = new VTOMPickUp();
			break;
		case ObjectiveTypes.Drop_Off:
			module = new VTOMDropOff();
			break;
		case ObjectiveTypes.Land:
			module = new VTOMLandAt();
			break;
		case ObjectiveTypes.Refuel:
			module = new VTOMRefuel();
			break;
		case ObjectiveTypes.Protect:
			module = new VTOMDefendUnit();
			break;
		case ObjectiveTypes.Conditional:
			module = new VTOMConditional();
			break;
		}
		module.objective = this;
	}

	private void WriteModuleDataToDict()
	{
		fields.Clear();
		FieldInfo[] array = module.GetType().GetFields();
		foreach (FieldInfo fieldInfo in array)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = customAttributes[j];
				object value = fieldInfo.GetValue(module);
				string value2 = VTSConfigUtils.WriteObject(fieldInfo.FieldType, value);
				fields.Add(fieldInfo.Name, value2);
			}
		}
	}

	private void ParseFieldsDictToModule()
	{
		FieldInfo[] array = module.GetType().GetFields();
		foreach (FieldInfo fieldInfo in array)
		{
			object[] customAttributes = fieldInfo.GetCustomAttributes(typeof(UnitSpawnAttribute), inherit: true);
			for (int j = 0; j < customAttributes.Length; j++)
			{
				_ = customAttributes[j];
				if (fields.ContainsKey(fieldInfo.Name))
				{
					string s = fields[fieldInfo.Name];
					object value = VTSConfigUtils.ParseObject(fieldInfo.FieldType, s);
					fieldInfo.SetValue(module, value);
				}
			}
		}
	}

	private void OnMissionsUpdated()
	{
		if (QuicksaveManager.quickloading || (QuicksaveManager.isQuickload && !PlayerSpawn.qLoadPlayerComplete) || ((bool)module.objectiveBehaviour && module.objectiveBehaviour.started))
		{
			return;
		}
		bool flag = true;
		if (preReqObjectives != null)
		{
			foreach (int preReqObjective in preReqObjectives)
			{
				VTObjective objective = VTScenario.current.objectives.GetObjective(preReqObjective);
				if (objective != null && (objective.module.objectiveBehaviour == null || (!objective.module.objectiveBehaviour.cancelled && !objective.module.objectiveBehaviour.completed)))
				{
					flag = false;
					break;
				}
			}
		}
		if (flag)
		{
			BeginObjective();
		}
	}

	[VTEvent("Begin Objective", "Begins the objective if it has not already begun.")]
	public void BeginObjective()
	{
		if (!module.objectiveBehaviour)
		{
			module.SetupObjective();
		}
		if ((bool)module.objectiveBehaviour && !module.objectiveBehaviour.started)
		{
			module.objectiveBehaviour.BeginMission();
		}
	}

	[VTEvent("Complete Objective", "Completes the objective if it has not already finished.")]
	public void CompleteObjective()
	{
		if (!module.objectiveBehaviour)
		{
			module.SetupObjective();
		}
		if ((bool)module.objectiveBehaviour && !module.objectiveBehaviour.objectiveFinished)
		{
			module.objectiveBehaviour.CompleteObjective();
		}
	}

	[VTEvent("Fail Objective", "Fails the objective if it has begun and finished.  This will trigger a total mission failure if the objective is marked as Required.")]
	public void FailObjective()
	{
		if (!module.objectiveBehaviour)
		{
			module.SetupObjective();
		}
		if ((bool)module.objectiveBehaviour && !module.objectiveBehaviour.objectiveFinished)
		{
			module.objectiveBehaviour.FailObjective();
		}
	}

	[VTEvent("Cancel Objective", "Removes the objective without completing or failing it.")]
	public void CancelObjective()
	{
		if (startMode == StartModes.Final)
		{
			Debug.LogError("Cannot cancel a final objective! (" + objectiveName + "[" + objectiveID + "])");
		}
		else
		{
			if ((bool)module.objectiveBehaviour)
			{
				MissionManager.instance.CancelObjective(module.objectiveBehaviour);
			}
			Dispose();
		}
	}
}
