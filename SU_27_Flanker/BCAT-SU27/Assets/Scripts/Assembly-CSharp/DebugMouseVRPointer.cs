using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SpatialTracking;
using UnityEngine.UI;

public class DebugMouseVRPointer : MonoBehaviour
{
	private static List<DebugMouseVRPointer> debugPointers = new List<DebugMouseVRPointer>();

	public Transform cameraPosTf;

	private bool mouseEnabled;

	private VRHandController rightController;

	private static List<RaycastResult> rList = new List<RaycastResult>();

	private void OnEnable()
	{
		if (!GameSettings.TryGetGameSettingValue<bool>("mptest", out var val) || !val)
		{
			base.enabled = false;
		}
		else
		{
			debugPointers.Add(this);
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.PageUp))
		{
			if (mouseEnabled)
			{
				DisablePointer();
			}
			else
			{
				EnablePointer();
			}
		}
	}

	private void EnablePointer()
	{
		DebugMouseVRPointer debugMouseVRPointer = null;
		float num = float.MaxValue;
		foreach (DebugMouseVRPointer debugPointer in debugPointers)
		{
			float sqrMagnitude = (debugPointer.transform.position - VRHead.instance.transform.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				debugMouseVRPointer = debugPointer;
			}
		}
		if (debugMouseVRPointer != this)
		{
			return;
		}
		foreach (VRHandController controller in VRHandController.controllers)
		{
			if (!controller.isLeft)
			{
				rightController = controller;
			}
		}
		mouseEnabled = true;
		Transform transform = VRHead.instance.transform;
		Camera[] componentsInChildren = VRHead.instance.GetComponentsInChildren<Camera>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].stereoTargetEye = StereoTargetEyeMask.None;
		}
		transform.GetComponent<TrackedPoseDriver>().enabled = false;
		transform.position = cameraPosTf.position;
		transform.rotation = cameraPosTf.rotation;
		GetComponent<Canvas>().worldCamera = VRHead.instance.cam;
		StartCoroutine(MouseRoutine());
	}

	private void DisablePointer()
	{
		mouseEnabled = false;
	}

	private void OnDisable()
	{
		DisablePointer();
		debugPointers.Remove(this);
	}

	private IEnumerator MouseRoutine()
	{
		GetComponent<GraphicRaycaster>();
		while (mouseEnabled)
		{
			Ray ray = VRHead.instance.cam.ScreenPointToRay(Input.mousePosition);
			Plane plane = new Plane(-base.transform.forward, base.transform.position);
			Debug.DrawRay(ray.origin, ray.direction, Color.red);
			if (plane.Raycast(ray, out var enter))
			{
				VRPointInteractableLine component = rightController.GetComponent<VRPointInteractableLine>();
				if ((bool)component)
				{
					component.interactionTransform.rotation = Quaternion.LookRotation(ray.GetPoint(enter) - component.interactionTransform.position);
				}
				else
				{
					rightController.interactionTransform.position = ray.GetPoint(enter);
				}
			}
			if (Input.GetMouseButtonDown(0) && (bool)rightController.hoverInteractable)
			{
				rightController.hoverInteractable.StartInteraction();
			}
			yield return null;
		}
	}
}
