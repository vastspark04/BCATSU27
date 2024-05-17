using UnityEngine;

public class SCCVehicleControl : ScenarioConditionalComponent
{
	public enum ControlConditions
	{
		Interacted,
		EqualTo,
		GreaterThan,
		LessThan
	}

	[SCCField]
	public VehicleControlReference vehicleControl;

	[SCCField]
	public ControlConditions controlCondition;

	[SCCField]
	public float controlValue;

	[SCCField]
	public bool isNot;

	private VRInteractable interactable;

	public override bool GetCondition()
	{
		if (controlCondition == ControlConditions.Interacted)
		{
			if (!interactable)
			{
				interactable = vehicleControl.GetControl().GetComponent<VRInteractable>();
			}
			bool flag = interactable.wasInteractedThisFrame;
			if (isNot)
			{
				flag = !flag;
			}
			return flag;
		}
		Component control = vehicleControl.GetControl();
		float num = -1f;
		switch (vehicleControl.controlType)
		{
		case VehicleControlReference.ControlTypes.Lever:
			num = ((VRLever)control).currentState;
			break;
		case VehicleControlReference.ControlTypes.Float_Knob:
			num = ((VRTwistKnob)control).currentValue;
			break;
		case VehicleControlReference.ControlTypes.Int_Knob:
			num = ((VRTwistKnobInt)control).currentState;
			break;
		case VehicleControlReference.ControlTypes.Door:
		{
			VRDoor vRDoor = (VRDoor)control;
			num = vRDoor.currentAngle / vRDoor.maxDoorAngle;
			break;
		}
		case VehicleControlReference.ControlTypes.PowerLever:
		{
			VRThrottle vRThrottle = (VRThrottle)control;
			num = (vRThrottle.invertOutput ? (1f - vRThrottle.currentThrottle) : vRThrottle.currentThrottle);
			break;
		}
		}
		bool flag2 = false;
		switch (controlCondition)
		{
		case ControlConditions.EqualTo:
			flag2 = num == controlValue;
			break;
		case ControlConditions.GreaterThan:
			flag2 = num > controlValue;
			break;
		case ControlConditions.LessThan:
			flag2 = num < controlValue;
			break;
		}
		if (isNot)
		{
			flag2 = !flag2;
		}
		return flag2;
	}
}
