using UnityEngine;

[RequireComponent(typeof(VRInteractable))]
public class VRIntGlovePoser : MonoBehaviour
{
	public GloveAnimation.Poses hoverPose;

	public GloveAnimation.Poses interactionPose;

	public Transform lockTransform;

	public bool referencedLeft;

	public Transform leftLockTransform { get; private set; }

	private void Start()
	{
		if ((bool)lockTransform)
		{
			leftLockTransform = new GameObject(lockTransform.name + "_LEFT").transform;
			leftLockTransform.parent = lockTransform.parent;
			leftLockTransform.localScale = lockTransform.localScale;
			Vector3 localPosition = lockTransform.localPosition;
			localPosition.x = 0f - localPosition.x;
			leftLockTransform.localPosition = localPosition;
			Vector3 localEulerAngles = lockTransform.localEulerAngles;
			localEulerAngles.y = 0f - localEulerAngles.y;
			localEulerAngles.z = 0f - localEulerAngles.z;
			leftLockTransform.localEulerAngles = localEulerAngles;
			if (referencedLeft)
			{
				Transform transform = lockTransform;
				lockTransform = leftLockTransform;
				leftLockTransform = transform;
			}
		}
		VRInteractable component = GetComponent<VRInteractable>();
		component.OnStartInteraction += Vrint_OnStartInteraction;
		component.OnStopInteraction += Vrint_OnStopInteraction;
		if (!component.poseBounds)
		{
			component.OnHover += Vrint_OnHover;
			component.OnUnHover += Vrint_OnUnHover;
		}
	}

	private void Vrint_OnUnHover(VRHandController controller)
	{
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.SetPoseInteractable(GloveAnimation.Poses.Idle);
		}
	}

	private void Vrint_OnHover(VRHandController controller)
	{
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.SetPoseHover(hoverPose);
		}
	}

	private void Vrint_OnStopInteraction(VRHandController controller)
	{
		if ((bool)controller.gloveAnimation)
		{
			controller.gloveAnimation.ClearInteractPose();
		}
	}

	private void Vrint_OnStartInteraction(VRHandController controller)
	{
		if (!controller.gloveAnimation)
		{
			return;
		}
		controller.gloveAnimation.SetPoseInteractable(interactionPose);
		if ((bool)lockTransform)
		{
			if (controller.isLeft)
			{
				controller.gloveAnimation.SetLockTransform(leftLockTransform);
			}
			else
			{
				controller.gloveAnimation.SetLockTransform(lockTransform);
			}
		}
	}
}
