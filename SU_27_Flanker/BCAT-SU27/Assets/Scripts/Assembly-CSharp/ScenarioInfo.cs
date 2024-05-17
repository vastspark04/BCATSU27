using System;

[Serializable]
public class ScenarioInfo
{
	public enum ScenarioTypes
	{
		Mission,
		Training,
		Quick
	}

	public ScenarioTypes scenarioType = ScenarioTypes.Quick;

	public string scenarioName;

	public string scenarioDescription;
}
