using UnityEngine;

public class ElbowHintCrossInterpolator : MonoBehaviour
{
	public Transform positionATransform;

	public Transform positionBTransform;

	public Transform handTransform;

	public Transform handReferenceSpace;

	public AnimationCurve interpolationCurve;

	private void Update()
	{
		if ((bool)positionATransform)
		{
			Vector3 localPosition = Vector3.Lerp(positionATransform.localPosition, positionBTransform.localPosition, interpolationCurve.Evaluate(handReferenceSpace.InverseTransformPoint(handTransform.position).x));
			base.transform.localPosition = localPosition;
		}
	}
}
