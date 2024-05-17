using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SCCUnit : ScenarioConditionalComponent
{
	[SCCField]
	public UnitReference unit;

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
		return "unit: " + unit.GetDisplayName() + " method: " + methodName + " isNot: " + isNot;
	}

	protected override void OnGatherReferences()
	{
		base.OnGatherReferences();
		if (!unit.GetSpawner())
		{
			return;
		}
		Type type = unit.GetSpawner().prefabUnitSpawn.GetType();
		if (string.IsNullOrEmpty(methodName))
		{
			return;
		}
		method = type.GetMethod(methodName);
		try
		{
			if (method != null)
			{
				if (methodParameters == null || methodParameters.Count <= 0)
				{
					return;
				}
				ParameterInfo[] array = method.GetParameters();
				parameters = new object[array.Length];
				Debug.Log("Parsing parameters for SCC " + methodName);
				int i = 0;
				for (int j = 0; j < array.Length; j++)
				{
					Type parameterType = array[j].ParameterType;
					Debug.Log(" - " + parameterType.Name);
					if (parameterType == typeof(UnitReferenceList))
					{
						Debug.Log("   - its a UnitReferenceList, handling...");
						string text = string.Empty;
						for (; !string.IsNullOrEmpty(methodParameters[i]); i++)
						{
							Debug.Log($"    - {i}={methodParameters[i]}");
							text = text + methodParameters[i] + ";";
						}
						Debug.Log("   - s_list == '" + text + "'");
						parameters[j] = VTSConfigUtils.ParseObject(parameterType, text);
						i++;
					}
					else
					{
						parameters[j] = VTSConfigUtils.ParseObject(parameterType, methodParameters[i]);
						i++;
					}
				}
			}
			else
			{
				Debug.LogError("SCCUnit: Missing method - " + type.Name + "." + methodName + "()");
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Exception when trying to load SCCUnit references for {unit.GetDisplayName()}: \n{arg}");
		}
	}

	public override bool GetCondition()
	{
		if ((bool)unit.GetUnit() && method != null)
		{
			bool flag = (bool)method.Invoke(unit.GetUnit(), parameters);
			if (isNot)
			{
				return !flag;
			}
			return flag;
		}
		return false;
	}
}
