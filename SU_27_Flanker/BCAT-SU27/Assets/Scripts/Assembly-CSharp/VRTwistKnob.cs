using System.Collections;
using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRTwistKnob : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData
{
	public bool vDataPersistent;

	public bool quicksavePersistent = true;

	private VRInteractable vrint;

	private bool grabbed;

	private VRHandController ctrlr;

	public Transform knobTransform;

	public bool smallKnob;

	[Range(0f, 1f)]
	public float startValue;

	private float value;

	private float clampedValue;

	public float twistRange = 270f;

	public FloatEvent OnSetState;

	public FloatEvent OnTwistDelta;

	private Vector3 startUp;

	[Range(0f, 1f)]
	public float vibeInterval = 1f;

	private float lastValue;

	private Transform lockTransform;

	public bool useCtrlrZ = true;

	public float currentValue => clampedValue;

	public void SetKnobValue(float t)
	{
		t = (clampedValue = Mathf.Clamp01(t));
		lastValue = t;
		knobTransform.localRotation = Quaternion.Euler(0f, clampedValue * twistRange, 0f);
		value = t;
		FireEvent();
	}

	private IEnumerator Start()
	{
		vrint = GetComponent<VRInteractable>();
		vrint.OnStartInteraction += Vrint_OnStartInteraction;
		vrint.OnStopInteraction += Vrint_OnStopInteraction;
		clampedValue = Mathf.Clamp01(startValue);
		value = clampedValue;
		knobTransform.localRotation = Quaternion.Euler(0f, clampedValue * twistRange, 0f);
		lockTransform = new GameObject("knobLockTransform").transform;
		lockTransform.parent = knobTransform;
		yield return null;
		SetKnobValue(value);
	}

	private void OnDrawGizmos()
	{
		if ((bool)knobTransform)
		{
			Vector3 vector = knobTransform.parent.forward * 0.1f;
			Gizmos.DrawLine(knobTransform.position, knobTransform.position + vector);
			vector = Quaternion.AngleAxis(twistRange, knobTransform.parent.up) * vector;
			Gizmos.DrawLine(knobTransform.position, knobTransform.position + vector);
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		grabbed = false;
		value = clampedValue;
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.ClearInteractPose();
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		grabbed = true;
		ctrlr = controller;
		startUp = ctrlr.transform.InverseTransformDirection(knobTransform.parent.forward);
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.SetKnobTransform(knobTransform, lockTransform, smallKnob);
			controller.gloveAnimation.SetPoseInteractable(GloveAnimation.Poses.Knob);
		}
		StartCoroutine(GrabbedRoutine());
	}

	private IEnumerator GrabbedRoutine()
	{
		while (grabbed)
		{
			Vector3 vector = knobTransform.parent.InverseTransformDirection(ctrlr.transform.TransformDirection(startUp));
			vector.y = 0f;
			float num = Vector3.Angle(vector, Vector3.forward);
			num *= Mathf.Sign(Vector3.Dot(vector, Vector3.right));
			startUp = ctrlr.transform.InverseTransformDirection(knobTransform.parent.forward);
			float num2 = Mathf.Clamp(twistRange, 1f, 360f);
			float arg = num / num2;
			value += num / num2;
			if (twistRange >= 360f)
			{
				clampedValue = Mathf.Repeat(value, 360f);
			}
			else
			{
				clampedValue = Mathf.Clamp01(value);
			}
			if (OnTwistDelta != null)
			{
				OnTwistDelta.Invoke(arg);
			}
			knobTransform.localRotation = Quaternion.Euler(0f, clampedValue * num2, 0f);
			if (Mathf.Abs(lastValue - clampedValue) > vibeInterval)
			{
				lastValue = clampedValue;
				ctrlr.Vibrate(0.3f, 0.06f);
			}
			FireEvent();
			yield return null;
		}
	}

	private void FireEvent()
	{
		if (OnSetState != null)
		{
			OnSetState.Invoke(clampedValue);
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			qsNode.AddNode(base.gameObject.name + "_VRTwistKnob").SetValue("currentValue", currentValue);
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		if (quicksavePersistent)
		{
			ConfigNode node = qsNode.GetNode(base.gameObject.name + "_VRTwistKnob");
			if (node != null)
			{
				SetKnobValue(startValue = node.GetValue<float>("currentValue"));
			}
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		if (vDataPersistent)
		{
			string nodeName = base.gameObject.name + "_VRTwistKnob";
			vDataNode.AddOrGetNode(nodeName).SetValue("value", currentValue);
		}
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		if (vDataPersistent)
		{
			string text = base.gameObject.name + "_VRTwistKnob";
			ConfigNode node = vDataNode.GetNode(text);
			if (node != null)
			{
				SetKnobValue(startValue = node.GetValue<float>("value"));
			}
		}
	}
}
