using System;
using System.Collections.Generic;

[AttributeUsage(AttributeTargets.Field)]
public class UnitSpawnAttribute : Attribute
{
	public string name;

	public List<string> uiOptions;

	public UnitSpawnAttribute(string name, params string[] uiOptionsParams)
	{
		this.name = name;
		SetupUIOptions(uiOptionsParams);
	}

	protected void SetupUIOptions(string[] uiOptionsParams)
	{
		if (uiOptionsParams != null)
		{
			uiOptions = new List<string>();
			for (int i = 0; i < uiOptionsParams.Length; i++)
			{
				uiOptions.Add(uiOptionsParams[i]);
			}
		}
	}

	public UnitSpawnAttribute(string name)
	{
		this.name = name;
	}

	public UnitSpawnAttribute()
	{
		name = string.Empty;
	}
}
