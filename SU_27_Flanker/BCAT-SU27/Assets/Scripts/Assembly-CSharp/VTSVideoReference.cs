using System.Collections.Generic;

public class VTSVideoReference : IConfigValue, IScenarioResourceUser
{
	public string relativeUrl;

	public bool resourceDirty = true;

	public string url
	{
		get
		{
			if (resourceDirty)
			{
				return relativeUrl;
			}
			return VTScenario.currentScenarioInfo.GetFullResourcePath(relativeUrl);
		}
	}

	public VTSVideoReference()
	{
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Editor)
		{
			VTScenario.current.AddResourceUser(this);
		}
	}

	public void ConstructFromValue(string s)
	{
		relativeUrl = s;
		resourceDirty = false;
	}

	public List<string> GetAllUsedResources()
	{
		if (!string.IsNullOrEmpty(relativeUrl))
		{
			return new List<string> { relativeUrl };
		}
		return null;
	}

	public string[] GetDirtyResources()
	{
		if (resourceDirty)
		{
			return new string[1] { relativeUrl };
		}
		return null;
	}

	public void SetCleanedResources(string[] resources)
	{
		relativeUrl = resources[0];
		resourceDirty = false;
	}

	public string WriteValue()
	{
		return relativeUrl;
	}
}
