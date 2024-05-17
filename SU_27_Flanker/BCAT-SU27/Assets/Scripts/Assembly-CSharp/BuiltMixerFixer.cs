using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class BuiltMixerFixer : MonoBehaviour
{
	private IEnumerator Start()
	{
		while (!AudioController.instance)
		{
			yield return null;
		}
		AudioSource[] componentsInChildren = GetComponentsInChildren<AudioSource>(includeInactive: true);
		foreach (AudioSource audioSource in componentsInChildren)
		{
			if ((bool)audioSource.outputAudioMixerGroup)
			{
				AudioMixerGroup[] array = AudioController.instance.environmentMixer.FindMatchingGroups(audioSource.outputAudioMixerGroup.name);
				if (array != null && array.Length != 0)
				{
					audioSource.outputAudioMixerGroup = array[0];
				}
			}
		}
	}
}
