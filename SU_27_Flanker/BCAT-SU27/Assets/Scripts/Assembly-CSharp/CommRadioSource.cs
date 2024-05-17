using System.Collections;
using UnityEngine;

public class CommRadioSource : MonoBehaviour
{
	public float volume = 1f;

	public float spatialBlend = 1f;

	public float minDistance = 1f;

	public float maxDistance = 500f;

	public static AudioSource commSource { get; set; }

	public static AudioSource copilotCommSource { get; set; }

	private void EnsureCommSource()
	{
		if (!commSource)
		{
			commSource = new GameObject("CommRadioSource").AddComponent<AudioSource>();
			commSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
		}
		if (!copilotCommSource)
		{
			copilotCommSource = commSource.gameObject.AddComponent<AudioSource>();
			copilotCommSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
		}
	}

	private void ApplySettings(AudioSource s)
	{
		s.spatialBlend = spatialBlend;
		s.minDistance = minDistance;
		s.maxDistance = maxDistance;
		s.volume = volume;
		s.dopplerLevel = 0f;
		s.priority = 32;
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	public void SetAsRadioSource()
	{
		EnsureCommSource();
		CommRadioManager.instance.SetAudioSource(this);
		ApplySettings(commSource);
		ApplySettings(copilotCommSource);
	}

	private IEnumerator EnableRoutine()
	{
		while (!CommRadioManager.instance)
		{
			yield return null;
		}
		SetAsRadioSource();
	}
}
