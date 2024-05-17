using System;

public class UnitSpawnTooltipAttribute : Attribute
{
	public string tooltip;

	public UnitSpawnTooltipAttribute(string tooltip)
	{
		this.tooltip = tooltip;
	}
}
