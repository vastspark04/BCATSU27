using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
public class UnitSpawnAttributeConditional : Attribute
{
	public string conditionalMethodName;

	public UnitSpawnAttributeConditional(string conditionalMethod)
	{
		conditionalMethodName = conditionalMethod;
	}
}
