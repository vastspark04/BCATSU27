using System.Collections.Generic;

public class VTSAudioReference : IScenarioResourceUser, IConfigValue
{
	public string audioPath;

	public bool audioResourceDirty;

	public VTSAudioReference()
	{
		if (VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Editor)
		{
			VTScenario.current.AddResourceUser(this);
		}
	}

	public string[] GetDirtyResources()
	{
		if (audioResourceDirty)
		{
			return new string[1] { audioPath };
		}
		return null;
	}

	public void SetCleanedResources(string[] resources)
	{
		audioPath = resources[0];
		audioResourceDirty = false;
	}

	public List<string> GetAllUsedResources()
	{
		if (!string.IsNullOrEmpty(audioPath))
		{
			return new List<string> { audioPath };
		}
		return null;
	}

	public string WriteValue()
	{
		return audioPath;
	}

	public void ConstructFromValue(string s)
	{
		audioPath = s;
		audioResourceDirty = false;
	}
}
