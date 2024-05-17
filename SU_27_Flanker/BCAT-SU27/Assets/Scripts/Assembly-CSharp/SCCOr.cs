using System.Collections.Generic;

public class SCCOr : ScenarioConditionalComponent, ISCCMultiInput
{
	[SCCField]
	public List<int> factors = new List<int>();

	private List<ScenarioConditionalComponent> _factors = new List<ScenarioConditionalComponent>();

	public override int GetInputID(int inputIdx)
	{
		return factors[inputIdx];
	}

	protected override void OnGatherReferences()
	{
		foreach (int factor in factors)
		{
			_factors.Add(conditionalSys.GetComponent(factor));
		}
	}

	public override bool GetCondition()
	{
		if (_factors != null && _factors.Count > 0)
		{
			bool flag = false;
			for (int i = 0; i < _factors.Count; i++)
			{
				ScenarioConditionalComponent scenarioConditionalComponent = _factors[i];
				if (scenarioConditionalComponent != null)
				{
					flag = flag || scenarioConditionalComponent.GetCondition();
				}
				if (flag)
				{
					return true;
				}
			}
			return flag;
		}
		return false;
	}

	public void ClearFactorList()
	{
		factors = new List<int>();
	}

	public void AddFactorID(int id)
	{
		factors.Add(id);
	}

	public int GetInputCount()
	{
		return factors.Count;
	}
}
