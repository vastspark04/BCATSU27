using UnityEngine;

public class MachShockCloud : MonoBehaviour
{
	public FlightInfo flightInfo;

	public ParticleSystem ps;

	public AnimationCurve speedCurve;

	public AnimationCurve altitudeCurve;

	private ParticleSystemRenderer psR;

	private ParticleSystem.MainModule psMain;

	private ParticleSystem.EmissionModule psE;

	private bool pEnabled;

	private void Start()
	{
		psR = ps.GetComponent<ParticleSystemRenderer>();
		psMain = ps.main;
		psE = ps.emission;
		psE.enabled = true;
		pEnabled = true;
	}

	private void Update()
	{
		float num = speedCurve.Evaluate(flightInfo.airspeed);
		if (num > 0.02f)
		{
			if (!ps.isPlaying)
			{
				ps.Play();
			}
			num *= altitudeCurve.Evaluate(flightInfo.altitudeASL);
			if (num > 0.02f)
			{
				if (!pEnabled)
				{
					psR.enabled = true;
					psE.enabled = true;
					pEnabled = true;
				}
				Color color = new Color(1f, 1f, 1f, num);
				psMain.startColor = new ParticleSystem.MinMaxGradient(color);
			}
			else if (pEnabled)
			{
				psR.enabled = false;
				psE.enabled = false;
				ps.Stop();
				pEnabled = false;
			}
		}
		else if (pEnabled)
		{
			psR.enabled = false;
			psE.enabled = false;
			ps.Stop();
			pEnabled = false;
		}
	}
}
