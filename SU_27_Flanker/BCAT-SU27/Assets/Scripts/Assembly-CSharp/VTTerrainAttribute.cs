using System;

public class VTTerrainAttribute : Attribute
{
	public string displayName;

	public float min;

	public float max;

	public VTTerrainAttribute(string name, float min, float max)
	{
		displayName = name;
		this.min = min;
		this.max = max;
	}
}
