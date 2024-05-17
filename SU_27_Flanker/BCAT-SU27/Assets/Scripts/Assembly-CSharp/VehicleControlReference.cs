using System;
using System.Collections.Generic;
using UnityEngine;

public struct VehicleControlReference : IConfigValue
{
	public enum ControlTypes
	{
		Lever,
		Float_Knob,
		Int_Knob,
		Button,
		Joystick,
		Throttle,
		TutorialTarget,
		PowerLever,
		Collective,
		Door,
		VRInteractable
	}

	public string vehicleName;

	public ControlTypes controlType;

	public string controlName;

	public Component GetControl()
	{
		VehicleControlManifest vehicleControlManifest = null;
		string empty = string.Empty;
		bool flag = false;
		if ((bool)FlightSceneManager.instance && (bool)FlightSceneManager.instance.playerActor)
		{
			vehicleControlManifest = FlightSceneManager.instance.playerActor.gameObject.GetComponent<VehicleControlManifest>();
			empty = PilotSaveManager.currentVehicle.vehicleName;
		}
		else if (VTScenario.current != null && VTScenario.current.vehicle != null)
		{
			flag = true;
			vehicleControlManifest = VTScenario.current.vehicle.vehiclePrefab.GetComponent<VehicleControlManifest>();
			empty = VTScenario.current.vehicle.vehicleName;
		}
		if ((bool)vehicleControlManifest && vehicleName == empty)
		{
			Array array = null;
			switch (controlType)
			{
			case ControlTypes.Lever:
				array = vehicleControlManifest.levers;
				break;
			case ControlTypes.Float_Knob:
				array = vehicleControlManifest.twistKnobs;
				break;
			case ControlTypes.Int_Knob:
				array = vehicleControlManifest.twistKnobInts;
				break;
			case ControlTypes.Button:
				array = vehicleControlManifest.buttons;
				break;
			case ControlTypes.Joystick:
				array = vehicleControlManifest.joysticks;
				break;
			case ControlTypes.Throttle:
				return vehicleControlManifest.throttle;
			case ControlTypes.TutorialTarget:
				array = vehicleControlManifest.tutorialTargets;
				break;
			case ControlTypes.PowerLever:
				array = vehicleControlManifest.powerLevers;
				break;
			case ControlTypes.Collective:
				array = vehicleControlManifest.collectives;
				break;
			case ControlTypes.Door:
				array = vehicleControlManifest.doors;
				break;
			case ControlTypes.VRInteractable:
				array = vehicleControlManifest.vrInteractables;
				break;
			}
			if (array != null)
			{
				if (controlType == ControlTypes.Joystick && !flag)
				{
					Component result = null;
					float num = float.MaxValue;
					for (int i = 0; i < array.Length; i++)
					{
						Component component = (Component)array.GetValue(i);
						if (component.gameObject.activeInHierarchy)
						{
							float sqrMagnitude = (component.transform.position - VRHead.position).sqrMagnitude;
							if (sqrMagnitude < num)
							{
								result = component;
								num = sqrMagnitude;
							}
						}
					}
					return result;
				}
				for (int j = 0; j < array.Length; j++)
				{
					Component component2 = (Component)array.GetValue(j);
					if (!component2)
					{
						continue;
					}
					if (controlType == ControlTypes.Joystick)
					{
						if (component2.gameObject.activeInHierarchy || flag)
						{
							return component2;
						}
					}
					else if (component2.gameObject.name == controlName)
					{
						return component2;
					}
				}
			}
		}
		return null;
	}

	public string WriteValue()
	{
		return ConfigNodeUtils.WriteList(new List<string>(3)
		{
			vehicleName,
			controlType.ToString(),
			controlName
		});
	}

	public void ConstructFromValue(string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			List<string> list = ConfigNodeUtils.ParseList(s);
			vehicleName = list[0];
			controlType = ConfigNodeUtils.ParseEnum<ControlTypes>(list[1]);
			controlName = list[2];
		}
	}

	public string GetUIDisplayName()
	{
		if (string.IsNullOrEmpty(vehicleName) || string.IsNullOrEmpty(controlName))
		{
			return "None";
		}
		Component control = GetControl();
		if ((bool)control)
		{
			if (control is TutLineTarget)
			{
				return ((TutLineTarget)control).targetName;
			}
			VRInteractable component = control.gameObject.GetComponent<VRInteractable>();
			if ((bool)component)
			{
				return component.interactableName;
			}
			return control.gameObject.name;
		}
		if (!string.IsNullOrEmpty(controlName))
		{
			return "Missing!";
		}
		return "None";
	}
}
