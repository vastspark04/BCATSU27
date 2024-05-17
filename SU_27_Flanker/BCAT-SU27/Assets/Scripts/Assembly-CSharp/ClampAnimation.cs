using UnityEngine;

public class ClampAnimation : StateMachineBehaviour
{
	public string speedMultiplierName;

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		if ((stateInfo.normalizedTime >= 1f && stateInfo.speedMultiplier > 0f) || (stateInfo.normalizedTime <= 0f && stateInfo.speedMultiplier < 0f))
		{
			animator.SetFloat(speedMultiplierName, 0f);
		}
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
	}
}
