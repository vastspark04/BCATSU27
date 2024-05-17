using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class LandingGearArmAnimator : MonoBehaviour
{
	public Transform armTransform;

	public Transform bottomTransform;

	public Transform footParentTransform;

	public AnimationCurve heightToAngleCurve;

	public float angleMultiplier = 1f;

	private float armLength = -1f;

	public GearAnimator gearAnimator;

	private Coroutine updateRoutine;

	private void Start()
	{
		if ((bool)armTransform && (bool)bottomTransform && (bool)footParentTransform)
		{
			if (armLength < 0f)
			{
				armLength = (armTransform.position - bottomTransform.position).magnitude;
			}
			if (!gearAnimator)
			{
				gearAnimator = base.transform.root.GetComponentInChildren<GearAnimator>(includeInactive: true);
			}
			gearAnimator.OnSetTargetState += GearAnimator_OnSetTargetState;
			EnsureUpdateRoutine();
		}
	}

	private void OnEnable()
	{
		if (!gearAnimator)
		{
			gearAnimator = base.transform.root.GetComponentInChildren<GearAnimator>(includeInactive: true);
		}
		EnsureUpdateRoutine();
	}

	private void GearAnimator_OnSetTargetState(GearAnimator.GearStates obj)
	{
		EnsureUpdateRoutine();
	}

	private void EnsureUpdateRoutine()
	{
		if (armLength < 0f)
		{
			armLength = (armTransform.position - bottomTransform.position).magnitude;
		}
		if (updateRoutine != null)
		{
			StopCoroutine(updateRoutine);
		}
		updateRoutine = StartCoroutine(UpdateRoutine());
	}

	private IEnumerator UpdateRoutine()
	{
		yield return null;
		WaitForEndOfFrame late = new WaitForEndOfFrame();
		yield return late;
		UpdateAnim();
		while (gearAnimator.state != GearAnimator.GearStates.Retracted || gearAnimator.targetState != GearAnimator.GearStates.Retracted)
		{
			yield return late;
			UpdateAnim();
		}
	}

	private void UpdateAnim()
	{
		float z = base.transform.InverseTransformVector(footParentTransform.position - bottomTransform.position).z;
		float num = heightToAngleCurve.Evaluate(z / armLength);
		Vector3 localEulerAngles = new Vector3(0f, num * angleMultiplier, 0f);
		armTransform.localEulerAngles = localEulerAngles;
	}
}
