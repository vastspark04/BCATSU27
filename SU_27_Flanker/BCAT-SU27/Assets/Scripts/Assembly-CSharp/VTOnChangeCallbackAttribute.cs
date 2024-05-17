using System;

[AttributeUsage(AttributeTargets.Field)]
public class VTOnChangeCallbackAttribute : Attribute
{
	public string methodName;

	public VTOnChangeCallbackAttribute(string methodName)
	{
		this.methodName = methodName;
	}
}
