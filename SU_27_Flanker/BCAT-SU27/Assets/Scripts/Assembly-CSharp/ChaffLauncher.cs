using UnityEngine;

public class ChaffLauncher : MonoBehaviour
{
	private ParticleSystem[] ps;

	public AudioSource audioSource;

	public AudioClip fireSound;

	private void Start()
	{
		ps = GetComponentsInChildren<ParticleSystem>();
	}

	public void FireChaff()
	{
		FloatingOrigin.instance.AddQueuedFixedUpdateAction(FinalChaff);
	}

	private void FinalChaff()
	{
		if ((bool)this)
		{
			ps.FireBurst();
			audioSource.PlayOneShot(fireSound);
		}
	}
}
