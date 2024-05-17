using UnityEngine;

public class OVRLipSyncContext : OVRLipSyncContextBase
{
	public bool enableKeyboardInput;
	public bool enableTouchInput;
	public bool audioLoopback;
	public KeyCode loopbackKey;
	public bool showVisemes;
	public KeyCode debugVisemesKey;
	public bool skipAudioSource;
	public float gain;
	public KeyCode debugLaughterKey;
	public bool showLaughter;
	public float laughterScore;
}
