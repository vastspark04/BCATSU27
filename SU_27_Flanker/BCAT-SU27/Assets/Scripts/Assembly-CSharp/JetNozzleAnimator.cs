using UnityEngine;

public class JetNozzleAnimator : MonoBehaviour
{
	public ModuleEngine engine;

	public AnimationCurve throttleCurve;

	public float abMultiplier;

	public string stateName;

	public int layer = -1;

	private Animator animator;

	private int stateNameHash;

	private LODBase lodBase;

	private float lastT = -1f;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		stateNameHash = Animator.StringToHash(stateName);
		lodBase = GetComponentInParent<LODBase>();
	}

	private void Update()
	{
		SetThrottle(engine.finalThrottle);
	}

	private void SetThrottle(float throttle)
	{
		if (!lodBase || !(lodBase.sqrDist > 1000000f))
		{
			float num = throttle * (1f + engine.abMult * abMultiplier);
			if (Mathf.Abs(num - lastT) > 0.001f)
			{
				lastT = num;
				animator.Play(stateNameHash, layer, throttleCurve.Evaluate(num));
				animator.speed = 0f;
			}
		}
	}
}
