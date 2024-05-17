using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu]
public class AudioMixerReference : ScriptableObject
{
	public AudioMixer mixer;

	public AudioMixerGroup exteriorGroup;

	public AudioMixerGroup interiorGroup;
}
