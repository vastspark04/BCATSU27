using System.Collections.Generic;
using UnityEngine;

public class UnitReferenceList : IConfigValue
{
	public List<UnitReference> units = new List<UnitReference>();

	public IUnitFilter[] unitFilters;

	[HideInInspector]
	public bool allowSubunits;

	public TeamOptions teamOption { get; protected set; }

	public UnitReferenceList()
	{
		teamOption = TeamOptions.BothTeams;
	}

	public UnitReferenceList(IUnitFilter[] unitFilters)
	{
		teamOption = TeamOptions.BothTeams;
		this.unitFilters = unitFilters;
	}

	public bool ContainsUnit(int unitID, int subIdx = -1)
	{
		foreach (UnitReference unit in units)
		{
			if (unit.unitID == unitID && unit.GetSubUnitIdx() == subIdx)
			{
				return true;
			}
		}
		return false;
	}

	public string WriteValue()
	{
		List<string> list = new List<string>();
		foreach (UnitReference unit in units)
		{
			string text = unit.unitID.ToString();
			if (unit.GetSubUnitIdx() >= 0)
			{
				text = text + ":" + unit.GetSubUnitIdx();
			}
			list.Add(text);
		}
		return ConfigNodeUtils.WriteList(list);
	}

	public void ConstructFromValue(string s)
	{
		foreach (string item in ConfigNodeUtils.ParseList(s))
		{
			if (item.Contains(":"))
			{
				string[] array = item.Split(':');
				int id = ConfigNodeUtils.ParseInt(array[0]);
				int subIdx = ConfigNodeUtils.ParseInt(array[1]);
				units.Add(new UnitReference(id, subIdx));
			}
			else
			{
				units.Add(new UnitReference(ConfigNodeUtils.ParseInt(item)));
			}
		}
	}
}
