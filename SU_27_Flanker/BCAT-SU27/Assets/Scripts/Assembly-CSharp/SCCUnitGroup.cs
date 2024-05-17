using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SCCUnitGroup : ScenarioConditionalComponent
{
	[SCCField]
	public VTUnitGroup.UnitGroup unitGroup;

	[SCCField]
	public string methodName;

	[SCCField]
	public List<string> methodParameters;

	[SCCField]
	public bool isNot;

	public MethodInfo method;

	public object[] parameters;

	public override string GetDebugString()
	{
		return "group: " + unitGroup.team.ToString() + " " + unitGroup.groupID.ToString() + " method: " + methodName + " isNot: " + isNot;
	}

	protected override void OnGatherReferences()
	{
		base.OnGatherReferences();
		if (unitGroup == null)
		{
			return;
		}
		if (unitGroup.groupActions == null)
		{
			Debug.LogError("group actions is null for: " + unitGroup.team.ToString() + " : " + unitGroup.groupID);
		}
		Type type = unitGroup.groupActions.GetType();
		if (string.IsNullOrEmpty(methodName))
		{
			return;
		}
		method = type.GetMethod(methodName);
		if (methodParameters != null && methodParameters.Count > 0)
		{
			parameters = new object[methodParameters.Count];
			ParameterInfo[] array = method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
			{
				Type parameterType = array[i].ParameterType;
				parameters[i] = VTSConfigUtils.ParseObject(parameterType, methodParameters[i]);
			}
		}
	}

	public override bool GetCondition()
	{
		if (method != null && unitGroup != null)
		{
			bool flag = (bool)method.Invoke(unitGroup.groupActions, parameters);
			if (isNot)
			{
				return !flag;
			}
			return flag;
		}
		return false;
	}
}
