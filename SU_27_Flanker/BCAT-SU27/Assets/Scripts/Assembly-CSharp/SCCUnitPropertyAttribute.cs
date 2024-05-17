using System;

public class SCCUnitPropertyAttribute : Attribute
{
	public string displayName;

	public string[] paramNames;

	public bool showIsNotOption;

	public SCCUnitPropertyAttribute(string displayName, string[] paramNames, bool showIsNotOption = false)
	{
		this.displayName = displayName;
		this.paramNames = paramNames;
		this.showIsNotOption = showIsNotOption;
	}

	public SCCUnitPropertyAttribute(string displayName, bool showIsNotOption = false)
	{
		this.displayName = displayName;
		paramNames = null;
		this.showIsNotOption = showIsNotOption;
	}
}
