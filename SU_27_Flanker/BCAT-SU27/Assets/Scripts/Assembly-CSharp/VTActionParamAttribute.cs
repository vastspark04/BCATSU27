using System;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
public class VTActionParamAttribute : Attribute
{
	public Type type;

	public object data;

	public VTActionParamAttribute()
	{
		type = null;
		data = null;
	}

	public VTActionParamAttribute(Type type, object data)
	{
		this.type = type;
		this.data = data;
	}
}
