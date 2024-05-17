using UnityEngine;

public class SCCGlobalValueUI : SCCNodeUI
{
	public VTGlobalValueProperty gvProp;

	public VTEnumProperty comparisonProp;

	public VTFloatRangeProperty c_valueProp;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		SCCGlobalValue sCCGlobalValue = (SCCGlobalValue)base.component;
		gvProp.SetInitialValue(sCCGlobalValue.gv);
		comparisonProp.SetInitialValue(sCCGlobalValue.comparison);
		c_valueProp.min = -99999f;
		c_valueProp.max = 99999f;
		c_valueProp.SetInitialValue((float)sCCGlobalValue.c_value);
	}

	public override void UpdateComponent()
	{
		SCCGlobalValue obj = (SCCGlobalValue)base.component;
		obj.gv = (GlobalValue)gvProp.GetValue();
		obj.comparison = (IntComparisons)comparisonProp.GetValue();
		obj.c_value = Mathf.RoundToInt((float)c_valueProp.GetValue());
	}
}
