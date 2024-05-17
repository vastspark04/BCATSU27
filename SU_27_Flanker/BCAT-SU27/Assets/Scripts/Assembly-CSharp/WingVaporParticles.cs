using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WingVaporParticles : MonoBehaviour
{
	public FlightInfo flightInfo;

	private ParticleSystem ps;

	private ParticleSystem.MainModule psMain;

	private ParticleSystem.EmissionModule psEmission;

	public AnimationCurve alphaGCurve;

	private Color mainColor;

	private bool emissionEnabled;

	private void Awake()
	{
		ps = GetComponent<ParticleSystem>();
		mainColor = ps.main.startColor.color;
		psMain = ps.main;
		psEmission = ps.emission;
		emissionEnabled = ps.emission.enabled;
	}

	private void Update()
	{
		float num = 0f;
		if (flightInfo.airspeed > 50f)
		{
			num = alphaGCurve.Evaluate(flightInfo.playerGs) * Mathf.Clamp01((flightInfo.airspeed - 50f) / 100f);
		}
		if (num > 0f)
		{
			Color color = mainColor;
			color.a = num;
			psMain.startColor = color;
			if (!emissionEnabled)
			{
				emissionEnabled = true;
				psEmission.enabled = true;
				ps.Play();
			}
		}
		else if (emissionEnabled)
		{
			psEmission.enabled = false;
			emissionEnabled = false;
			ps.Stop();
		}
	}
}
