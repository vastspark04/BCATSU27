using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VTOLVR.Multiplayer;

public class VTEventTarget
{
	public enum TargetTypes
	{
		Unit,
		UnitGroup,
		Objective,
		Timed_Events,
		Trigger_Events,
		Event_Sequences,
		System,
		Static_Object,
		Base
	}

	public class ActionParamInfo
	{
		public Type type;

		public object value;

		public string name;

		public List<ActionParamAttributeInfo> attributes;

		public const string NODE_NAME = "ParamInfo";

		public void SaveToConfigNode(ConfigNode node)
		{
			ConfigNode configNode = new ConfigNode("ParamInfo");
			configNode.SetValue("type", type.ToString());
			configNode.SetValue("value", VTSConfigUtils.WriteObject(type, value));
			configNode.SetValue("name", name);
			foreach (ActionParamAttributeInfo attribute in attributes)
			{
				attribute.SaveToConfigNode(configNode);
			}
			node.AddNode(configNode);
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			type = Type.GetType(node.GetValue("type"));
			value = VTSConfigUtils.ParseObject(type, node.GetValue("value"));
			name = node.GetValue("name");
			attributes = new List<ActionParamAttributeInfo>();
			foreach (ConfigNode node2 in node.GetNodes("ParamAttrInfo"))
			{
				ActionParamAttributeInfo item = new ActionParamAttributeInfo(node2);
				attributes.Add(item);
			}
		}
	}

	public class ActionParamAttributeInfo
	{
		public Type type;

		public object data;

		public const string NODE_NAME = "ParamAttrInfo";

		public void SaveToConfigNode(ConfigNode node)
		{
			ConfigNode configNode = new ConfigNode("ParamAttrInfo");
			configNode.SetValue("type", type.ToString());
			configNode.SetValue("data", VTSConfigUtils.WriteObject(type, data));
			node.AddNode(configNode);
		}

		public void LoadFromConfigNode(ConfigNode node)
		{
			type = Type.GetType(node.GetValue("type"));
			data = VTSConfigUtils.ParseObject(type, node.GetValue("data"));
		}

		public ActionParamAttributeInfo(Type attributeType, object data)
		{
			type = attributeType;
			this.data = data;
		}

		public ActionParamAttributeInfo(ConfigNode node)
		{
			LoadFromConfigNode(node);
		}
	}

	public TargetTypes targetType;

	public int targetID;

	public string eventName;

	public string methodName;

	public ActionParamInfo[] parameterInfos;

	public const string NODE_NAME = "EventTarget";

	public void Invoke()
	{
		if (VTScenario.current == null)
		{
			return;
		}
		object target = GetTarget();
		if (target != null)
		{
			switch (targetType)
			{
			case TargetTypes.Unit:
				FireAction(((UnitSpawner)target).spawnedUnit);
				break;
			case TargetTypes.UnitGroup:
				FireAction(((VTUnitGroup.UnitGroup)target).groupActions);
				break;
			default:
				FireAction(target);
				break;
			}
		}
		else
		{
			Debug.Log("Action '" + eventName + "' is missing a target object!");
		}
	}

	public void SaveToNode(ConfigNode node)
	{
		ConfigNode configNode = new ConfigNode("EventTarget");
		configNode.SetValue("targetType", targetType);
		configNode.SetValue("targetID", targetID);
		configNode.SetValue("eventName", eventName);
		configNode.SetValue("methodName", methodName);
		if (parameterInfos != null)
		{
			for (int i = 0; i < parameterInfos.Length; i++)
			{
				parameterInfos[i].SaveToConfigNode(configNode);
			}
		}
		node.AddNode(configNode);
	}

	public void LoadFromNode(ConfigNode evtNode)
	{
		targetType = ConfigNodeUtils.ParseEnum<TargetTypes>(evtNode.GetValue("targetType"));
		targetID = ConfigNodeUtils.ParseInt(evtNode.GetValue("targetID"));
		eventName = evtNode.GetValue("eventName");
		methodName = evtNode.GetValue("methodName");
		List<ConfigNode> nodes = evtNode.GetNodes("ParamInfo");
		parameterInfos = new ActionParamInfo[nodes.Count];
		for (int i = 0; i < nodes.Count; i++)
		{
			ActionParamInfo actionParamInfo = new ActionParamInfo();
			actionParamInfo.LoadFromConfigNode(nodes[i]);
			parameterInfos[i] = actionParamInfo;
		}
	}

	public string GetDisplayLabel()
	{
		return GetTargetLabel() + "->" + eventName;
	}

	public string GetTargetLabel()
	{
		switch (targetType)
		{
		case TargetTypes.Unit:
			return VTScenario.current.units.GetUnit(targetID).GetUIDisplayName();
		case TargetTypes.UnitGroup:
		{
			VTUnitGroup.UnitGroup unitGroup = VTScenario.current.groups.GetUnitGroup(targetID);
			return unitGroup.team.ToString() + unitGroup.groupID;
		}
		case TargetTypes.Timed_Events:
			return VTScenario.current.timedEventGroups.GetGroup(targetID).groupName;
		case TargetTypes.Trigger_Events:
			return VTScenario.current.triggerEvents.GetEvent(targetID).eventName;
		case TargetTypes.Event_Sequences:
			return VTScenario.current.sequencedEvents.GetSequence(targetID).sequenceName;
		case TargetTypes.Objective:
			return VTScenario.current.objectives.GetObjective(targetID).objectiveName;
		case TargetTypes.System:
			return "System";
		case TargetTypes.Static_Object:
			return VTScenario.current.staticObjects.GetObject(targetID).GetUIDisplayName();
		case TargetTypes.Base:
			return VTScenario.current.bases.baseInfos[targetID].GetFinalName();
		default:
			return string.Empty;
		}
	}

	public bool TargetExists()
	{
		return GetTarget() != null;
	}

	public object GetTarget()
	{
		return targetType switch
		{
			TargetTypes.Unit => VTScenario.current.units.GetUnit(targetID), 
			TargetTypes.UnitGroup => VTScenario.current.groups.GetUnitGroup(targetID), 
			TargetTypes.Timed_Events => VTScenario.current.timedEventGroups.GetGroup(targetID), 
			TargetTypes.Trigger_Events => VTScenario.current.triggerEvents.GetEvent(targetID), 
			TargetTypes.Event_Sequences => VTScenario.current.sequencedEvents.GetSequence(targetID), 
			TargetTypes.Objective => VTScenario.current.objectives.GetObjective(targetID), 
			TargetTypes.System => targetID switch
			{
				1 => VTScenario.current.tutorialActions, 
				2 => VTScenario.current.globalValueActions, 
				_ => VTScenario.current.systemActions, 
			}, 
			TargetTypes.Static_Object => VTScenario.current.staticObjects.GetObject(targetID), 
			TargetTypes.Base => VTScenario.current.bases.baseInfos[targetID].basePrefab, 
			_ => null, 
		};
	}

	private void FireAction(object obj)
	{
		if (obj == null)
		{
			return;
		}
		MethodInfo method = obj.GetType().GetMethod(methodName);
		if (method != null)
		{
			if (obj is AIUnitSpawn && VTOLMPUtils.IsMultiplayer() && !VTOLMPLobbyManager.isLobbyHost)
			{
				return;
			}
			if (parameterInfos != null && parameterInfos.Length != 0)
			{
				object[] array = new object[parameterInfos.Length];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = parameterInfos[i].value;
				}
				try
				{
					method.Invoke(obj, array);
					return;
				}
				catch (Exception ex)
				{
					if (ex is TargetParameterCountException)
					{
						string text = "";
						ActionParamInfo[] array2 = parameterInfos;
						foreach (ActionParamInfo actionParamInfo in array2)
						{
							text = text + "[" + actionParamInfo.type.Name + " " + actionParamInfo.name + "]";
						}
						string text2 = "";
						ParameterInfo[] parameters = method.GetParameters();
						foreach (ParameterInfo parameterInfo in parameters)
						{
							text2 = text2 + "[" + parameterInfo.ParameterType.Name + " " + parameterInfo.Name + "]";
						}
						Debug.LogError("TargetParameterCountException! Method: " + method.Name + ", Input Parameters: " + text + "\nExpected Parameters: " + text2);
					}
					else
					{
						Debug.LogError(ex);
					}
					return;
				}
			}
			method.Invoke(obj, null);
		}
		else
		{
			Debug.LogError("The action " + methodName + " does not exist! (" + obj.ToString() + ")");
		}
	}

	public void DeleteEventTarget()
	{
		if (VTScenario.current == null)
		{
			return;
		}
		ActionParamInfo[] array = parameterInfos;
		foreach (ActionParamInfo actionParamInfo in array)
		{
			if (actionParamInfo != null && actionParamInfo.value is IScenarioResourceUser)
			{
				VTScenario.current.RemoveResourceUser((IScenarioResourceUser)actionParamInfo.value);
			}
			if (actionParamInfo.type == typeof(ConditionalActionReference))
			{
				ConditionalActionReference conditionalActionReference = (ConditionalActionReference)actionParamInfo.value;
				VTScenario.current.conditionalActions.DeleteAction(conditionalActionReference.id);
			}
		}
	}
}
