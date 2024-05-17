public struct StaticObjectReference : IConfigValue
{
	private int idPlusOne;

	public int objectID
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

	public StaticObjectReference(int id)
	{
		idPlusOne = id + 1;
	}

	public VTStaticObject GetStaticObject()
	{
		if (idPlusOne == 0)
		{
			return null;
		}
		return VTScenario.current.staticObjects.GetObject(objectID);
	}

	public string GetDisplayName()
	{
		if (objectID >= 0)
		{
			VTStaticObject staticObject = GetStaticObject();
			if ((bool)staticObject)
			{
				return staticObject.GetUIDisplayName();
			}
			return "Missing!";
		}
		return "None";
	}

	public string WriteValue()
	{
		return objectID.ToString();
	}

	public void ConstructFromValue(string s)
	{
		objectID = ConfigNodeUtils.ParseInt(s);
	}
}
