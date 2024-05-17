using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SCCVehicleControlUI : SCCNodeUI
{
	public enum IsNotOptions
	{
		Is,
		Is_Not
	}

	public VTVehicleControlProperty vcProp;

	public GameObject condPropButtonObj;

	public Text conditionPropText;

	private SCCVehicleControl.ControlConditions cCondition;

	public VTFloatRangeProperty valueProp;

	public VTEnumProperty isOrIsNotProp;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		SCCVehicleControl sCCVehicleControl = (SCCVehicleControl)base.component;
		vcProp.SetInitialValue(sCCVehicleControl.vehicleControl);
		vcProp.OnPropertyValueChanged += VcProp_OnPropertyValueChanged;
		cCondition = sCCVehicleControl.controlCondition;
		SetValueLimits(sCCVehicleControl.vehicleControl);
		valueProp.SetInitialValue(sCCVehicleControl.controlValue);
		isOrIsNotProp.SetInitialValue(sCCVehicleControl.isNot ? IsNotOptions.Is_Not : IsNotOptions.Is);
		VcProp_OnPropertyValueChanged(sCCVehicleControl.vehicleControl);
		UpdateUI();
	}

	private void SetValueLimits(VehicleControlReference vRef)
	{
		valueProp.min = 0f;
		valueProp.max = 1f;
		Component control = vRef.GetControl();
		if (control != null)
		{
			switch (vRef.controlType)
			{
			case VehicleControlReference.ControlTypes.Lever:
			{
				VRLever vRLever = (VRLever)control;
				valueProp.max = vRLever.states - 1;
				break;
			}
			case VehicleControlReference.ControlTypes.Float_Knob:
				valueProp.max = 1f;
				break;
			case VehicleControlReference.ControlTypes.Int_Knob:
			{
				VRTwistKnobInt vRTwistKnobInt = (VRTwistKnobInt)control;
				valueProp.max = vRTwistKnobInt.states - 1;
				break;
			}
			}
		}
	}

	private void VcProp_OnPropertyValueChanged(object arg0)
	{
		VehicleControlReference valueLimits = (VehicleControlReference)arg0;
		if (valueLimits.controlType == VehicleControlReference.ControlTypes.Button || valueLimits.controlType == VehicleControlReference.ControlTypes.Joystick || valueLimits.controlType == VehicleControlReference.ControlTypes.Throttle)
		{
			cCondition = SCCVehicleControl.ControlConditions.Interacted;
		}
		else
		{
			SetValueLimits(valueLimits);
		}
		UpdateUI();
	}

	public override void UpdateComponent()
	{
		SCCVehicleControl obj = (SCCVehicleControl)base.component;
		obj.vehicleControl = (VehicleControlReference)vcProp.GetValue();
		obj.controlCondition = cCondition;
		obj.controlValue = (float)valueProp.GetValue();
		obj.isNot = (IsNotOptions)isOrIsNotProp.GetValue() == IsNotOptions.Is_Not;
	}

	private void UpdateUI()
	{
		VehicleControlReference vehicleControlReference = (VehicleControlReference)vcProp.GetValue();
		if (vehicleControlReference.GetControl() != null)
		{
			isOrIsNotProp.gameObject.SetActive(value: true);
			conditionPropText.gameObject.SetActive(value: true);
			conditionPropText.text = cCondition.ToString();
			VehicleControlReference.ControlTypes controlType = vehicleControlReference.controlType;
			if (controlType == VehicleControlReference.ControlTypes.Lever || controlType == VehicleControlReference.ControlTypes.Float_Knob || controlType == VehicleControlReference.ControlTypes.Int_Knob || controlType == VehicleControlReference.ControlTypes.Door || controlType == VehicleControlReference.ControlTypes.PowerLever)
			{
				if (cCondition == SCCVehicleControl.ControlConditions.Interacted)
				{
					valueProp.gameObject.SetActive(value: false);
				}
				else
				{
					valueProp.gameObject.SetActive(value: true);
				}
				condPropButtonObj.SetActive(value: true);
			}
			else
			{
				condPropButtonObj.SetActive(value: false);
				valueProp.gameObject.SetActive(value: false);
			}
			if (controlType == VehicleControlReference.ControlTypes.Float_Knob || controlType == VehicleControlReference.ControlTypes.PowerLever)
			{
				valueProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Float;
			}
			else
			{
				valueProp.rangeType = UnitSpawnAttributeRange.RangeTypes.Int;
			}
		}
		else
		{
			valueProp.gameObject.SetActive(value: false);
			conditionPropText.gameObject.SetActive(value: false);
			condPropButtonObj.SetActive(value: false);
			isOrIsNotProp.gameObject.SetActive(value: false);
		}
	}

	public void ControlConditionPropButton()
	{
		VehicleControlReference vehicleControlReference = (VehicleControlReference)vcProp.GetValue();
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		list2.Add(SCCVehicleControl.ControlConditions.Interacted);
		list2.Add(SCCVehicleControl.ControlConditions.GreaterThan);
		list2.Add(SCCVehicleControl.ControlConditions.LessThan);
		if (vehicleControlReference.controlType != VehicleControlReference.ControlTypes.Float_Knob && vehicleControlReference.controlType != VehicleControlReference.ControlTypes.PowerLever)
		{
			list2.Add(SCCVehicleControl.ControlConditions.EqualTo);
		}
		foreach (object item in list2)
		{
			list.Add(((SCCVehicleControl.ControlConditions)item).ToString());
		}
		int selected = list2.IndexOf(cCondition);
		conditionalEditor.editor.optionSelector.Display("Control Condition", list.ToArray(), list2.ToArray(), selected, OnSelectedControlCondition);
	}

	private void OnSelectedControlCondition(object obj)
	{
		cCondition = (SCCVehicleControl.ControlConditions)obj;
		UpdateUI();
	}
}
