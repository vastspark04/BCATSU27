using System.Collections.Generic;

public class VehicleEquipmentList : IConfigValue
{
	public List<string> equipment = new List<string>();

	public void ConstructFromValue(string s)
	{
		equipment = ConfigNodeUtils.ParseList(s);
	}

	public string WriteValue()
	{
		return ConfigNodeUtils.WriteList(equipment);
	}
}
