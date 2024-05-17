using UnityEngine;

public class ParticleAnimator : MonoBehaviour
{
	public bool animateEmission;

	public AnimationCurve emissionCurve;

	public bool animateSize;

	public AnimationCurve minSizeCurve;

	public AnimationCurve maxSizeCurve;

	private ParticleSystem.MainModule psMain;

	private ParticleSystem.EmissionModule em;

	private float lastT = -1f;

	public ParticleSystem ps { get; private set; }

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
		psMain = ps.main;
		em = ps.emission;
		Evaluate(0f);
	}

	public void Evaluate(float t)
	{
		if (!(Mathf.Abs(t - lastT) > 0.001f))
		{
			return;
		}
		lastT = t;
		if (animateEmission)
		{
			float num = emissionCurve.Evaluate(t);
			if (num > 0f)
			{
				em.enabled = true;
				em.rateOverTime = new ParticleSystem.MinMaxCurve(num);
			}
			else
			{
				em.enabled = false;
			}
		}
		if (animateSize && em.enabled)
		{
			float min = minSizeCurve.Evaluate(t);
			float max = maxSizeCurve.Evaluate(t);
			psMain.startSize = new ParticleSystem.MinMaxCurve(min, max);
		}
	}
}
