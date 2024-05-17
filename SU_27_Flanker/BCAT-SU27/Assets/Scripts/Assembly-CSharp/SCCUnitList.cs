using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SCCUnitList : ScenarioConditionalComponent
{
	[SCCField]
	public UnitReferenceList unitList;

	[SCCField]
	public string methodName;

	[SCCField]
	public List<string> methodParameters;

	[SCCField]
	public bool isNot;

	public MethodInfo method;

	public object[] parameters;

	protected override void OnGatherReferences()
	{
		base.OnGatherReferences();
		if (unitList == null || string.IsNullOrEmpty(methodName))
		{
			return;
		}
		method = typeof(SCCUnitList).GetMethod(methodName);
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
		if (method != null && unitList != null)
		{
			bool flag = (bool)method.Invoke(this, parameters);
			if (isNot)
			{
				return !flag;
			}
			return flag;
		}
		return false;
	}

	[SCCUnitProperty("All Alive", true)]
	public bool SCC_AllAlive()
	{
		for (int i = 0; i < unitList.units.Count; i++)
		{
			Actor actor = unitList.units[i].GetActor();
			if (!actor || !actor.alive)
			{
				return false;
			}
		}
		return true;
	}

	[SCCUnitProperty("Num Alive", new string[] { "Comparison", "Count" }, false)]
	public bool SCC_NumAlive(IntComparisons comparison, [VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)][VTRangeParam(0f, 100f)] float count)
	{
		int num = Mathf.RoundToInt(count);
		int num2 = 0;
		for (int i = 0; i < unitList.units.Count; i++)
		{
			Actor actor = unitList.units[i].GetActor();
			if ((bool)actor && actor.alive)
			{
				num2++;
			}
		}
		return comparison switch
		{
			IntComparisons.Equals => num2 == num, 
			IntComparisons.Greater_Than => num2 > num, 
			IntComparisons.Less_Than => num2 < num, 
			_ => false, 
		};
	}

	[SCCUnitProperty("Any Near Waypoint", new string[] { "Waypoint", "Radius" }, true)]
	public bool SCC_AnyNearWaypoint(Waypoint wpt, [VTRangeParam(10f, 200000f)] float radius)
	{
		float num = radius * radius;
		for (int i = 0; i < unitList.units.Count; i++)
		{
			UnitReference unitReference = unitList.units[i];
			Actor actor = unitReference.GetActor();
			UnitSpawner spawner = unitReference.GetSpawner();
			if ((bool)spawner && spawner.spawned && (bool)actor && actor.alive && actor.gameObject.activeInHierarchy && wpt.GetTransform() != null)
			{
				Vector3 vector = actor.position - wpt.worldPosition;
				vector.y = 0f;
				if (vector.sqrMagnitude < num)
				{
					return true;
				}
			}
		}
		return false;
	}

	[SCCUnitProperty("Any Unit Detected", new string[] { "By Team" }, true)]
	public bool SCC_AnyUnitDetected(Teams team)
	{
		for (int i = 0; i < unitList.units.Count; i++)
		{
			Actor actor = unitList.units[i].GetActor();
			if ((bool)actor && actor.alive && ((team == Teams.Allied) ? actor.detectedByAllied : actor.detectedByEnemy))
			{
				return true;
			}
		}
		return false;
	}

	[SCCUnitProperty("All Units Detected", new string[] { "By Team" }, true)]
	public bool SCC_AllUnitDetected(Teams team)
	{
		for (int i = 0; i < unitList.units.Count; i++)
		{
			Actor actor = unitList.units[i].GetActor();
			if (!actor || !actor.alive || !actor.gameObject.activeInHierarchy || !((team == Teams.Allied) ? actor.detectedByAllied : actor.detectedByEnemy))
			{
				return false;
			}
		}
		return true;
	}
}
