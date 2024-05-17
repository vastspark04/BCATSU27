using System.Collections.Generic;

public struct GlobalValue : IConfigValue, IEqualityComparer<GlobalValue>
{
	private int idPlusOne;

	public int id
	{
		get
		{
			return idPlusOne - 1;
		}
		set
		{
			idPlusOne = value + 1;
		}
	}

	public ScenarioGlobalValues.GlobalValueData data => VTScenario.current.globalValues.GetValueData(id);

	public string name
	{
		get
		{
			return data.name;
		}
		set
		{
			data.name = value;
		}
	}

	public string description
	{
		get
		{
			return data.description;
		}
		set
		{
			data.description = value;
		}
	}

	public int initialValue
	{
		get
		{
			return data.initialValue;
		}
		set
		{
			data.initialValue = value;
		}
	}

	public int currentValue
	{
		get
		{
			return data.currentValue;
		}
		set
		{
			data.currentValue = value;
		}
	}

	public static GlobalValue none
	{
		get
		{
			GlobalValue result = default(GlobalValue);
			result.id = -1;
			return result;
		}
	}

	public void ConstructFromValue(string s)
	{
		id = ConfigNodeUtils.ParseInt(s);
	}

	public string WriteValue()
	{
		return id.ToString();
	}

	public bool Equals(GlobalValue x, GlobalValue y)
	{
		return x.id.Equals(y.id);
	}

	public int GetHashCode(GlobalValue obj)
	{
		return obj.id.GetHashCode();
	}
}
