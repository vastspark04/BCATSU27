using UnityEngine;

public class ONSPAudioSource : MonoBehaviour
{
	[SerializeField]
	private bool enableSpatialization;
	[SerializeField]
	private float gain;
	[SerializeField]
	private bool useInvSqr;
	[SerializeField]
	private float near;
	[SerializeField]
	private float far;
	[SerializeField]
	private float volumetricRadius;
	[SerializeField]
	private float reverbSend;
	[SerializeField]
	private bool enableRfl;
}
