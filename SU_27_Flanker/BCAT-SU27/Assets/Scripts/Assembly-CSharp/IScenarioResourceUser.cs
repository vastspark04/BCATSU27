using System.Collections.Generic;

public interface IScenarioResourceUser
{
	string[] GetDirtyResources();

	void SetCleanedResources(string[] resources);

	List<string> GetAllUsedResources();
}
