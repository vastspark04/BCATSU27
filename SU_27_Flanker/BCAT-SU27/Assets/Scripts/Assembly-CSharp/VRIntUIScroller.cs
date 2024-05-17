using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VRInteractable))]
public class VRIntUIScroller : MonoBehaviour
{
	public ScrollRect scrollRect;

	private VRInteractable vrInt;

	public float scrollRate = 1f;

	public bool positional;

	private float dist;

	private float startAngle;

	private float angleToNrmDelta;

	private float startNrmPos;

	private float startPos;

	private float offsetToNrmDelta;

	private void Start()
	{
		vrInt = GetComponent<VRInteractable>();
		vrInt.OnInteract.AddListener(OnInteract);
		vrInt.OnInteracting.AddListener(OnInteracting);
	}

	private void OnInteract()
	{
		Vector3 position = vrInt.activeController.transform.position;
		dist = Vector3.Distance(base.transform.position, position);
		startAngle = VectorUtils.SignedAngle(base.transform.forward, Vector3.ProjectOnPlane(vrInt.activeController.transform.forward, base.transform.right), base.transform.up);
		float height = ((RectTransform)scrollRect.verticalScrollbar.transform).rect.height;
		float height2 = scrollRect.verticalScrollbar.handleRect.rect.height;
		float num = height - height2;
		float num2 = Vector3.Angle(base.transform.TransformPoint(0.5f * num * Vector3.up) - position, base.transform.TransformPoint(-0.5f * num * Vector3.up) - position);
		angleToNrmDelta = 1f / num2;
		startNrmPos = scrollRect.verticalNormalizedPosition;
		startPos = scrollRect.transform.InverseTransformPoint(vrInt.activeController.transform.position).y;
		offsetToNrmDelta = 1f / num;
	}

	private void OnInteracting()
	{
		if (positional)
		{
			float num = (scrollRect.transform.InverseTransformPoint(vrInt.activeController.transform.position).y - startPos) * offsetToNrmDelta * scrollRate;
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(startNrmPos + num);
		}
		else
		{
			float num2 = (VectorUtils.SignedAngle(base.transform.forward, Vector3.ProjectOnPlane(vrInt.activeController.transform.forward, base.transform.right), base.transform.up) - startAngle) * angleToNrmDelta * scrollRate;
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(startNrmPos + num2);
		}
	}
}
