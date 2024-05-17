using UnityEngine;

[RequireComponent(typeof(AudioAnimator))]
public class TireSkidAudio : MonoBehaviour
{
	public RaySpringDamper susp;

	public float maxSkid = 50f;

	private AudioAnimator anim;

	private void Awake()
	{
		anim = GetComponent<AudioAnimator>();
		anim.Evaluate(0f);
		susp.OnWheelSkid += Susp_OnWheelSkid;
	}

	private void Susp_OnWheelSkid(float skidAmount)
	{
		anim.Evaluate(skidAmount / maxSkid);
	}
}
