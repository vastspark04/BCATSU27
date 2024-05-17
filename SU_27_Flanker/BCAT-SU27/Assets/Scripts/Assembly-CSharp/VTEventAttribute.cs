using System;

[AttributeUsage(AttributeTargets.Method)]
public class VTEventAttribute : Attribute
{
	public string eventName;

	public string description;

	public string[] paramNames;

	public VTEventAttribute(string name)
	{
		eventName = name;
		description = "No description...";
	}

	public VTEventAttribute(string name, string description)
	{
		eventName = name;
		this.description = description;
	}

	public VTEventAttribute(string name, string description, params string[] paramNames)
	{
		eventName = name;
		this.description = description;
		this.paramNames = paramNames;
	}
}
