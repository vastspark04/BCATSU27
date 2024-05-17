using UnityEngine;

namespace VTOLVR.DLC.Rotorcraft{

public class LowRotorRPMHorn : MonoBehaviour
{
	public EmissiveTextureLight tLight;

	public AudioSource audioSource;

	public HelicopterRotor rotor;

	public float minCollective;

	public MinMax hornRPMRange;

	public float blinkRate = 2f;

	private void Update()
	{
		if (tLight.battery.Drain(0.01f * Time.deltaTime) && rotor.inputShaft.outputRPM > hornRPMRange.min && rotor.inputShaft.outputRPM < hornRPMRange.max && rotor.CurrentCollective() > minCollective)
		{
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
			tLight.SetStatus((Mathf.RoundToInt(blinkRate * Time.time) % 2 == 0) ? 1 : 0);
		}
		else
		{
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
			}
			tLight.SetStatus(0);
		}
	}
}

}