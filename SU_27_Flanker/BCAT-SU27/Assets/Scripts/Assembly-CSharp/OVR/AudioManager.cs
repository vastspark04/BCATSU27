using UnityEngine;
using UnityEngine.Audio;

namespace OVR
{
	public class AudioManager : MonoBehaviour
	{
		public bool makePersistent;
		public bool enableSpatializedAudio;
		public bool enableSpatializedFastOverride;
		public AudioMixer audioMixer;
		public AudioMixerGroup defaultMixerGroup;
		public AudioMixerGroup reservedMixerGroup;
		public AudioMixerGroup voiceChatMixerGroup;
		public bool verboseLogging;
		public int maxSoundEmitters;
		public float volumeSoundFX;
		public float soundFxFadeSecs;
		public float audioMinFallOffDistance;
		public float audioMaxFallOffDistance;
		public SoundGroup[] soundGroupings;
	}
}
