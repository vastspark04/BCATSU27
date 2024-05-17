public struct AirportReference
{
	private string _id;

	private int apId;

	private string type;

	public string id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
			if (!string.IsNullOrEmpty(_id))
			{
				string[] array = _id.Split(':');
				type = array[0];
				apId = ConfigNodeUtils.ParseInt(array[1]);
			}
		}
	}

	public AirportManager GetAirport()
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return VTScenario.current.GetAirport(id);
	}

	public string GetLabel()
	{
		if (string.IsNullOrEmpty(id))
		{
			return "None";
		}
		if (type == "unit")
		{
			UnitSpawner unit = VTScenario.current.units.GetUnit(apId);
			if ((bool)unit)
			{
				return unit.GetUIDisplayName();
			}
			_id = string.Empty;
			return "Missing!";
		}
		if (type == "map")
		{
			VTMapEdScenarioBasePrefab componentInParent = GetAirport().GetComponentInParent<VTMapEdScenarioBasePrefab>();
			if ((bool)componentInParent)
			{
				return "MAP : " + VTScenario.current.bases.baseInfos[componentInParent.id].GetFinalName();
			}
			return "MAP : " + GetAirport().airportName;
		}
		return "None";
	}

	public AirportReference(string id)
	{
		_id = string.Empty;
		apId = 0;
		type = string.Empty;
		this.id = id;
	}
}
