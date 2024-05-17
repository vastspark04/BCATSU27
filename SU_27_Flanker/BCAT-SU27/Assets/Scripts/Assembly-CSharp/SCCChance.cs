using UnityEngine;

public class SCCChance : ScenarioConditionalComponent, IQSVehicleComponent
{
	[SCCField]
	public int chance = 50;

	private bool val;

	protected override void OnGatherReferences()
	{
		val = Random.Range(0, 100) < chance;
	}

	public override bool GetCondition()
	{
		return val;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.SetValue("val", val);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		val = qsNode.GetValue<bool>("val");
	}
}
