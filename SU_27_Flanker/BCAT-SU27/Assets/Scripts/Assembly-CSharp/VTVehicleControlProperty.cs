using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VTVehicleControlProperty : VTPropertyField
{
	public VTScenarioEditor editor;

	public Text buttonLabel;

	public bool allowTutorialTargets = true;

	private VehicleControlReference currVal;

	public override void SetInitialValue(object value)
	{
		currVal = (VehicleControlReference)value;
		buttonLabel.text = currVal.GetUIDisplayName();
	}

	public override object GetValue()
	{
		return currVal;
	}

	public void EditButton()
	{
		VehicleControlManifest component = VTScenario.current.vehicle.vehiclePrefab.GetComponent<VehicleControlManifest>();
		List<string> list = new List<string>();
		List<object> list2 = new List<object>();
		VRLever[] levers = component.levers;
		foreach (VRLever vRLever in levers)
		{
			if ((bool)vRLever)
			{
				list.Add(vRLever.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRLever);
			}
		}
		VRTwistKnob[] twistKnobs = component.twistKnobs;
		foreach (VRTwistKnob vRTwistKnob in twistKnobs)
		{
			if ((bool)vRTwistKnob)
			{
				list.Add(vRTwistKnob.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRTwistKnob);
			}
		}
		VRTwistKnobInt[] twistKnobInts = component.twistKnobInts;
		foreach (VRTwistKnobInt vRTwistKnobInt in twistKnobInts)
		{
			if ((bool)vRTwistKnobInt)
			{
				list.Add(vRTwistKnobInt.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRTwistKnobInt);
			}
		}
		VRButton[] buttons = component.buttons;
		foreach (VRButton vRButton in buttons)
		{
			if ((bool)vRButton)
			{
				list.Add(vRButton.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRButton);
			}
		}
		VRDoor[] doors = component.doors;
		foreach (VRDoor vRDoor in doors)
		{
			if ((bool)vRDoor)
			{
				list.Add(vRDoor.GetComponent<VRInteractable>().GetControlReferenceName() + " (Door)");
				list2.Add(vRDoor);
			}
		}
		list.Add("Joystick");
		list2.Add(component.joysticks[0]);
		list.Add("Throttle");
		list2.Add(component.throttle);
		VRThrottle[] powerLevers = component.powerLevers;
		foreach (VRThrottle vRThrottle in powerLevers)
		{
			if ((bool)vRThrottle)
			{
				list.Add(vRThrottle.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRThrottle);
			}
		}
		powerLevers = component.collectives;
		foreach (VRThrottle vRThrottle2 in powerLevers)
		{
			if ((bool)vRThrottle2)
			{
				list.Add(vRThrottle2.GetComponent<VRInteractable>().GetControlReferenceName());
				list2.Add(vRThrottle2);
			}
		}
		VRInteractable[] vrInteractables = component.vrInteractables;
		foreach (VRInteractable vRInteractable in vrInteractables)
		{
			if ((bool)vRInteractable)
			{
				list.Add(vRInteractable.GetControlReferenceName() ?? "");
				list2.Add(vRInteractable);
			}
		}
		if (component.tutorialTargets != null && allowTutorialTargets)
		{
			TutLineTarget[] tutorialTargets = component.tutorialTargets;
			foreach (TutLineTarget tutLineTarget in tutorialTargets)
			{
				if ((bool)tutLineTarget)
				{
					list.Add(tutLineTarget.targetName);
					list2.Add(tutLineTarget);
				}
			}
		}
		editor.optionSelector.Display("Select Control", list.ToArray(), list2.ToArray(), -1, OnSelected);
	}

	private void OnSelected(object o)
	{
		currVal = default(VehicleControlReference);
		if (o != null)
		{
			currVal.vehicleName = VTScenario.current.vehicle.vehicleName;
			currVal.controlName = ((Component)o).gameObject.name;
			if (o is VRLever)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Lever;
			}
			else if (o is VRTwistKnob)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Float_Knob;
			}
			else if (o is VRTwistKnobInt)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Int_Knob;
			}
			else if (o is VRButton)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Button;
			}
			else if (o is VRJoystick)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Joystick;
			}
			else if (o is VRThrottle)
			{
				switch (((VRThrottle)o).throttleType)
				{
				case VRThrottle.ThrottleTypes.Throttle:
					currVal.controlType = VehicleControlReference.ControlTypes.Throttle;
					break;
				case VRThrottle.ThrottleTypes.HeliPower:
					currVal.controlType = VehicleControlReference.ControlTypes.PowerLever;
					break;
				case VRThrottle.ThrottleTypes.Collective:
					currVal.controlType = VehicleControlReference.ControlTypes.Collective;
					break;
				}
			}
			else if (o is TutLineTarget)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.TutorialTarget;
			}
			else if (o is VRDoor)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.Door;
			}
			else if (o is VRInteractable)
			{
				currVal.controlType = VehicleControlReference.ControlTypes.VRInteractable;
			}
		}
		buttonLabel.text = currVal.GetUIDisplayName();
		ValueChanged();
	}
}
