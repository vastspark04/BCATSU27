using UnityEngine;

public class SCCChanceUI : SCCNodeUI
{
	public VTFloatRangeProperty chanceProp;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		chanceProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
		chanceProp.min = 1f;
		chanceProp.max = 100f;
		chanceProp.SetInitialValue((float)((SCCChance)base.component).chance);
	}

	public override void UpdateComponent()
	{
		base.UpdateComponent();
		((SCCChance)base.component).chance = Mathf.RoundToInt((float)chanceProp.GetValue());
	}
}
