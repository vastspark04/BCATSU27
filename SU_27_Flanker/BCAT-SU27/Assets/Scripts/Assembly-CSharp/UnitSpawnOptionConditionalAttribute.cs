using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true)]
public class UnitSpawnOptionConditionalAttribute : Attribute
{
	public string conditionalMethodName;

	public UnitSpawnOptionConditionalAttribute(string conditionalMethod)
	{
		conditionalMethodName = conditionalMethod;
	}
}
