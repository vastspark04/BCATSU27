using System;
using UnityEngine.Audio;

namespace OVR
{
	[Serializable]
	public class SoundGroup
	{
		public string name;
		public SoundFX[] soundList;
		public AudioMixerGroup mixerGroup;
		public int maxPlayingSounds;
		public PreloadSounds preloadAudio;
		public float volumeOverride;
		public int playingSoundCount;
	}
}
