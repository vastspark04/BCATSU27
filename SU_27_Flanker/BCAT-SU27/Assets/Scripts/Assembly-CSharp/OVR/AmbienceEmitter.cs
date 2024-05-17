using UnityEngine;

namespace OVR
{
	public class AmbienceEmitter : MonoBehaviour
	{
		public SoundFXRef[] ambientSounds;
		public bool autoActivate;
		public bool autoRetrigger;
		public Vector2 randomRetriggerDelaySecs;
		public Transform[] playPositions;
	}
}
