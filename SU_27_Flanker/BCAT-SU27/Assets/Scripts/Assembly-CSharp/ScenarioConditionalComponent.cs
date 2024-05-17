using UnityEngine;

public class ScenarioConditionalComponent
{
	public int id;

	public Vector3 uiPos;

	public ScenarioConditional conditionalSys;

	public void GatherReferences()
	{
		OnGatherReferences();
	}

	protected virtual void OnGatherReferences()
	{
	}

	public virtual int GetInputID(int inputIdx)
	{
		return -1;
	}

	public virtual bool GetCondition()
	{
		return false;
	}

	public virtual string GetDebugString()
	{
		return string.Empty;
	}
}
