using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioListener))]
public class AudioListenerPosition : MonoBehaviour, IParentRBDependent
{
	public AudioVelocityUpdateMode updateMode = AudioVelocityUpdateMode.Dynamic;

	private static List<AudioListenerPosition> availableListeners = new List<AudioListenerPosition>();

	public Rigidbody rb;

	public bool useManualVelocity;

	private Vector3 _manualVelocity;

	private Camera cam;

	public static AudioListenerPosition currentAudioListener { get; private set; }

	public Vector3 myVelocity
	{
		get
		{
			if (useManualVelocity)
			{
				return _manualVelocity;
			}
			if ((bool)rb)
			{
				return rb.velocity;
			}
			if ((bool)cam)
			{
				return cam.velocity;
			}
			return Vector3.zero;
		}
	}

	public static Vector3 velocity
	{
		get
		{
			if ((bool)currentAudioListener)
			{
				return currentAudioListener.myVelocity;
			}
			return Vector3.zero;
		}
	}

	public AudioListener audioListener { get; private set; }

	public void SetManualVelocity(Vector3 v)
	{
		useManualVelocity = true;
		_manualVelocity = v;
	}

	public static Vector3 GetAudioListenerPosition()
	{
		if ((bool)currentAudioListener)
		{
			return currentAudioListener.transform.position;
		}
		return Vector3.zero;
	}

	private void OnEnable()
	{
		audioListener = GetComponent<AudioListener>();
		audioListener.velocityUpdateMode = updateMode;
		availableListeners.Add(this);
		if (audioListener.enabled)
		{
			if ((bool)currentAudioListener && currentAudioListener.GetInstanceID() != GetInstanceID())
			{
				currentAudioListener.audioListener.enabled = false;
			}
			currentAudioListener = this;
		}
		Debug.Log($"New audio listener: {base.gameObject.name}");
	}

	private void OnDisable()
	{
		availableListeners.Remove(this);
		if (!(currentAudioListener == this))
		{
			return;
		}
		for (int num = availableListeners.Count - 1; num >= 0; num--)
		{
			AudioListenerPosition audioListenerPosition = availableListeners[num];
			if ((bool)audioListenerPosition && audioListenerPosition.gameObject.activeInHierarchy)
			{
				audioListenerPosition.audioListener.enabled = true;
				currentAudioListener = audioListenerPosition;
				break;
			}
		}
	}

	private void Update()
	{
		if (audioListener.enabled)
		{
			if ((bool)currentAudioListener && currentAudioListener.GetInstanceID() != GetInstanceID())
			{
				currentAudioListener.audioListener.enabled = false;
			}
			currentAudioListener = this;
		}
	}

	public void SetParentRigidbody(Rigidbody rb)
	{
		this.rb = rb;
	}
}
