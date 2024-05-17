using System;

public static class VTSConfigUtils
{
	private static bool IsSameOrSubclass(Type baseType, Type unknownType)
	{
		if (!unknownType.IsSubclassOf(baseType))
		{
			return baseType == unknownType;
		}
		return true;
	}

	public static string WriteObject<T>(T o)
	{
		return WriteObject(typeof(T), o);
	}

	public static string WriteObject(Type type, object o)
	{
		VTScenario current = VTScenario.current;
		if (o == null)
		{
			return "null";
		}
		if (typeof(IConfigValue).IsAssignableFrom(type))
		{
			return ((IConfigValue)o).WriteValue();
		}
		if (type == typeof(UnitReference))
		{
			UnitReference unitReference = (UnitReference)o;
			if (unitReference.GetSubUnitIdx() >= 0)
			{
				return unitReference.unitID + ":" + unitReference.GetSubUnitIdx();
			}
			return unitReference.unitID.ToString();
		}
		if (type == typeof(FollowPath))
		{
			return current.paths.GetPathID((FollowPath)o).ToString();
		}
		if (IsSameOrSubclass(typeof(Waypoint), type))
		{
			if (o == null)
			{
				o = -1;
				return (-1).ToString();
			}
			if (o is UnitWaypoint)
			{
				return "unit:" + ((UnitWaypoint)o).unitSpawner.unitInstanceID;
			}
			return ((Waypoint)o).id.ToString();
		}
		if (type == typeof(MinMax))
		{
			return WriteMinMax((MinMax)o);
		}
		if (type == typeof(VTUnitGroup.UnitGroup))
		{
			return WriteUnitGroup((VTUnitGroup.UnitGroup)o);
		}
		if (type == typeof(AirportReference))
		{
			return ((AirportReference)o).id;
		}
		if (type == typeof(WingmanVoiceProfile))
		{
			return ((WingmanVoiceProfile)o).name;
		}
		if (type == typeof(AWACSVoiceProfile))
		{
			return ((AWACSVoiceProfile)o).name;
		}
		if (type == typeof(ScenarioConditional))
		{
			return ((ScenarioConditional)o).id.ToString();
		}
		return ConfigNodeUtils.WriteObject(o);
	}

	public static T ParseObject<T>(string s)
	{
		return (T)ParseObject(typeof(T), s);
	}

	public static object ParseObject(Type type, string s)
	{
		VTScenario current = VTScenario.current;
		if (s == "null" && !type.IsValueType)
		{
			return null;
		}
		if (typeof(IConfigValue).IsAssignableFrom(type))
		{
			object obj = InstantiateConfigValue(type);
			((IConfigValue)obj).ConstructFromValue(s);
			return obj;
		}
		if (type == typeof(UnitReference))
		{
			if (s.Contains(":"))
			{
				string[] array = s.Split(':');
				int id = ConfigNodeUtils.ParseInt(array[0]);
				int subIdx = ConfigNodeUtils.ParseInt(array[1]);
				return new UnitReference(id, subIdx);
			}
			return new UnitReference(ConfigNodeUtils.ParseInt(s));
		}
		if (type == typeof(FollowPath))
		{
			if (string.IsNullOrEmpty(s))
			{
				return null;
			}
			int num = ConfigNodeUtils.ParseInt(s);
			if (num < 0)
			{
				return null;
			}
			return current.paths.GetPath(num);
		}
		if (type == typeof(Waypoint))
		{
			if (s.Contains(":"))
			{
				string[] array2 = s.Split(':');
				if (array2[0] == "unit")
				{
					int unitID = ConfigNodeUtils.ParseInt(array2[1]);
					UnitSpawner unit = current.units.GetUnit(unitID);
					if ((bool)unit)
					{
						return unit.waypoint;
					}
					return null;
				}
				return ParseWaypoint(array2[1]);
			}
			return ParseWaypoint(s);
		}
		if (type == typeof(MinMax))
		{
			return ParseMinMax(s);
		}
		if (type == typeof(VTUnitGroup.UnitGroup))
		{
			return ParseUnitGroup(s);
		}
		if (type == typeof(AirportReference))
		{
			return new AirportReference(s);
		}
		if (type == typeof(WingmanVoiceProfile))
		{
			return VTResources.GetWingmanVoiceProfile(s);
		}
		if (type == typeof(AWACSVoiceProfile))
		{
			return VTResources.GetAWACSVoice(s);
		}
		if (type == typeof(ScenarioConditional))
		{
			int id2 = ConfigNodeUtils.ParseInt(s);
			return current.conditionals.GetConditional(id2);
		}
		return ConfigNodeUtils.ParseObject(type, s);
	}

	private static Waypoint ParseWaypoint(string s)
	{
		int num = ConfigNodeUtils.ParseInt(s);
		if (num < 0)
		{
			return null;
		}
		return VTScenario.current.waypoints.GetWaypoint(num);
	}

	public static VTUnitGroup.UnitGroup ParseUnitGroup(string s)
	{
		if (s.Contains("None") || s.Contains("null"))
		{
			return null;
		}
		string[] array = s.Split(':');
		Teams team = ConfigNodeUtils.ParseEnum<Teams>(array[0]);
		PhoneticLetters groupID = ConfigNodeUtils.ParseEnum<PhoneticLetters>(array[1]);
		return VTScenario.current.groups.GetUnitGroup(team, groupID);
	}

	public static string WriteUnitGroup(VTUnitGroup.UnitGroup unitGroup)
	{
		if (unitGroup != null)
		{
			return $"{unitGroup.team}:{unitGroup.groupID}";
		}
		return "None";
	}

	private static object InstantiateConfigValue(Type type)
	{
		return Activator.CreateInstance(type);
	}

	public static string WriteMinMax(MinMax mm)
	{
		return $"({mm.min},{mm.max})";
	}

	public static MinMax ParseMinMax(string s)
	{
		string[] array = s.Substring(1, s.Length - 2).Split(',');
		return new MinMax(float.Parse(array[0]), float.Parse(array[1]));
	}

	private static object GetDefaultValueForType(Type t)
	{
		if (t.IsValueType)
		{
			return Activator.CreateInstance(t);
		}
		return null;
	}
}
