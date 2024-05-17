using System;
using UnityEngine;

namespace OVR
{
	[Serializable]
	public class SoundFX
	{
		public string name;
		public SoundFXNext playback;
		public float volume;
		public Vector2 pitchVariance;
		public Vector2 falloffDistance;
		public AudioRolloffMode falloffCurve;
		public AnimationCurve volumeFalloffCurve;
		public AnimationCurve reverbZoneMix;
		public float spread;
		public float pctChanceToPlay;
		public SoundPriority priority;
		public Vector2 delay;
		public bool looping;
		public OSPProps ospProps;
		public AudioClip[] soundClips;
		public bool visibilityToggle;
	}
}
