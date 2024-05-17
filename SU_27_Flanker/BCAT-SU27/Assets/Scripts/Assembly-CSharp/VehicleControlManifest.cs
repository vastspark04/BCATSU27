using System.Collections.Generic;
using UnityEngine;

public class VehicleControlManifest : MonoBehaviour
{
	public VRLever[] levers;

	public VRTwistKnob[] twistKnobs;

	public VRTwistKnobInt[] twistKnobInts;

	public VRButton[] buttons;

	public VRJoystick[] joysticks;

	public VRThrottle throttle;

	public TutLineTarget[] tutorialTargets;

	public VRDoor[] doors;

	public VRInteractable[] vrInteractables;

	public MFDManager mfdManager;

	public MFDManager miniMfdManager;

	public MFDPortalManager mfdPortalManager;

	[Header("Helicopter")]
	public VRThrottle[] powerLevers;

	public VRThrottle[] collectives;

	[ContextMenu("Gather Controls")]
	public void GatherControls()
	{
		levers = GetComponentsInChildren<VRLever>(includeInactive: true);
		twistKnobs = GetComponentsInChildren<VRTwistKnob>(includeInactive: true);
		twistKnobInts = GetComponentsInChildren<VRTwistKnobInt>(includeInactive: true);
		joysticks = GetComponentsInChildren<VRJoystick>(includeInactive: true);
		throttle = GetComponentInChildren<VRThrottle>(includeInactive: true);
		buttons = GetComponentsInChildren<VRButton>(includeInactive: true);
		tutorialTargets = GetComponentsInChildren<TutLineTarget>(includeInactive: true);
		List<VRButton> list = new List<VRButton>();
		VRButton[] array = buttons;
		foreach (VRButton vRButton in array)
		{
			if (vRButton.GetComponentsInParent<EndMission>(includeInactive: true).Length == 0)
			{
				list.Add(vRButton);
			}
		}
		buttons = list.ToArray();
	}

	[ContextMenu("Find Duplicate Names")]
	private void FindDupeNames()
	{
		List<string> list = new List<string>();
		VRInteractable[] componentsInChildren = GetComponentsInChildren<VRInteractable>(includeInactive: true);
		VRInteractable[] array = componentsInChildren;
		foreach (VRInteractable vRInteractable in array)
		{
			if (list.Contains(vRInteractable.gameObject.name))
			{
				continue;
			}
			Debug.Log("Checking object: " + vRInteractable.gameObject.name, vRInteractable.gameObject);
			VRInteractable[] array2 = componentsInChildren;
			foreach (VRInteractable vRInteractable2 in array2)
			{
				if (vRInteractable != vRInteractable2 && (vRInteractable.GetControlReferenceName() == vRInteractable2.GetControlReferenceName() || vRInteractable.gameObject.name == vRInteractable2.gameObject.name))
				{
					Debug.LogError("Duplicate Found: " + vRInteractable.gameObject.name, vRInteractable2.gameObject);
				}
			}
			list.Add(vRInteractable.gameObject.name);
		}
	}
}
