using System.Collections;
using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRButton : MonoBehaviour
{
	public float depression = 0.005f;

	public Transform buttonTransform;

	public Vector3 depressionAxis = -Vector3.forward;

	public bool pressAndHold;

	private Vector3 localDepressionPosition;

	private Vector3 origPosition;

	private bool holding;

	private bool buttonDepressed;

	private void Start()
	{
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStartInteraction += Vrint_OnStartInteraction;
		component.OnStopInteraction += Vrint_OnStopInteraction;
		if ((bool)buttonTransform)
		{
			origPosition = buttonTransform.localPosition;
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.PressButton(base.transform, pressAndHold);
		}
		if ((bool)buttonTransform)
		{
			Vector3 normalized = buttonTransform.TransformDirection(depressionAxis).normalized;
			localDepressionPosition = origPosition + buttonTransform.parent.InverseTransformVector(depression * normalized);
			buttonTransform.localPosition = localDepressionPosition;
			buttonDepressed = true;
		}
		if (pressAndHold)
		{
			holding = true;
		}
		else if (base.gameObject.activeInHierarchy && base.enabled)
		{
			StartCoroutine(ButtonReturnRoutine());
		}
		else
		{
			buttonTransform.localPosition = origPosition;
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		if (pressAndHold)
		{
			controller.gloveAnimation.UnPressButton();
			holding = false;
			if (base.gameObject.activeInHierarchy && base.enabled)
			{
				StartCoroutine(ButtonReturnRoutine());
			}
			else
			{
				buttonTransform.localPosition = origPosition;
			}
		}
	}

	private void OnDisable()
	{
		if (buttonDepressed && (bool)buttonTransform)
		{
			buttonTransform.localPosition = origPosition;
		}
	}

	private IEnumerator ButtonReturnRoutine()
	{
		if ((bool)buttonTransform)
		{
			yield return null;
			while (!holding && (buttonTransform.localPosition - origPosition).sqrMagnitude > 1E-06f)
			{
				buttonTransform.localPosition = Vector3.Lerp(buttonTransform.localPosition, origPosition, 10f * Time.deltaTime);
				yield return null;
			}
			if (!holding)
			{
				buttonTransform.localPosition = origPosition;
				buttonDepressed = false;
			}
		}
	}
}
