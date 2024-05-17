using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SCCStaticObject : ScenarioConditionalComponent
{
	[SCCField]
	public StaticObjectReference objectReference;

	[SCCField]
	public string methodName;

	[SCCField]
	public List<string> methodParameters;

	[SCCField]
	public bool isNot;

	public MethodInfo method;

	public object[] parameters;

	private VTStaticObject staticObject;

	public override string GetDebugString()
	{
		return "staticObject: " + objectReference.GetDisplayName() + " method: " + methodName + " isNot: " + isNot;
	}

	protected override void OnGatherReferences()
	{
		base.OnGatherReferences();
		staticObject = objectReference.GetStaticObject();
		if ((bool)staticObject)
		{
			Type type = staticObject.GetType();
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
		else
		{
			Debug.LogError("SCCStaticObject node is missing a static object!");
		}
	}

	public override bool GetCondition()
	{
		if ((bool)staticObject && method != null)
		{
			bool flag = (bool)method.Invoke(staticObject, parameters);
			if (isNot)
			{
				return !flag;
			}
			return flag;
		}
		return false;
	}
}
