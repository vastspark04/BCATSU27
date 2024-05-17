using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class VTNetworkVoicePosition : MonoBehaviour
{
	public bool useParentEntity = true;

	public ulong overrideUserId;

	[Range(0f, 1f)]
	public float _3DBlend = 1f;

	public float minDistance = 1f;

	public float maxDistance = 500f;

	public AudioMixerGroup mixerGroup;

	public bool radioStartStop;

	public bool copilot;

	public AudioSource startStopSource;

	private AudioSource source;

	private static Dictionary<ulong, VTNetworkVoicePosition> voicePositions = new Dictionary<ulong, VTNetworkVoicePosition>();

	public void SetMixerGroup(AudioMixerGroup group)
	{
		mixerGroup = group;
		if ((bool)source)
		{
			source.outputAudioMixerGroup = group;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(EnableRoutine());
	}

	public void SetCommsVolume(float t)
	{
		if ((bool)CommRadioManager.instance)
		{
			CommRadioManager.instance.SetCommsVolume(t);
		}
	}

	private IEnumerator EnableRoutine()
	{
		ulong voiceId;
		if (useParentEntity)
		{
			VTNetEntity ent = GetComponentInParent<VTNetEntity>();
			while (ent.ownerID == 0L)
			{
				yield return null;
			}
			if (ent.owner.IsMe)
			{
				yield break;
			}
			voiceId = ent.ownerID;
		}
		else
		{
			voiceId = overrideUserId;
		}
		if (voicePositions.TryGetValue(voiceId, out var value))
		{
			if ((bool)value)
			{
				Object.Destroy(value.gameObject);
			}
			voicePositions[voiceId] = this;
		}
		else
		{
			voicePositions.Add(voiceId, this);
		}
		while (!VTNetworkVoice.instance.isVoiceChatEnabled)
		{
			yield return null;
		}
		AudioSource userVoiceSource = VTNetworkVoice.instance.GetUserVoiceSource(voiceId);
		if (!userVoiceSource)
		{
			Debug.LogError($"Could not get a voice source for {voiceId} ({new Friend(voiceId).Name})");
			yield break;
		}
		userVoiceSource.transform.parent = base.transform;
		userVoiceSource.transform.localPosition = Vector3.zero;
		userVoiceSource.spatialBlend = _3DBlend;
		userVoiceSource.minDistance = minDistance;
		userVoiceSource.maxDistance = maxDistance;
		userVoiceSource.dopplerLevel = 0f;
		userVoiceSource.outputAudioMixerGroup = mixerGroup;
		source = userVoiceSource;
		if (radioStartStop)
		{
			StartCoroutine(RadioRoutine(userVoiceSource, voiceId, copilot));
		}
	}

	private IEnumerator RadioRoutine(AudioSource a, SteamId id, bool copilot)
	{
		float lastTransmitTime = 0f;
		float stopAfterTransmitTime = 0.5f;
		bool rOn = false;
		AudioClip ssSound = (copilot ? CommRadioManager.instance.copilotStartStopSound : CommRadioManager.instance.startStopRadioSound);
		if ((bool)startStopSource)
		{
			_ = startStopSource;
		}
		else if (copilot)
		{
			_ = CommRadioManager.instance.copilotAudioSource;
		}
		else
		{
			_ = CommRadioManager.instance.commAudioSource;
		}
		while (base.enabled && (bool)a)
		{
			bool num = VTNetworkVoice.instance.IsUserTransmittingVoice(id);
			if (num)
			{
				lastTransmitTime = Time.time;
			}
			if (num != rOn)
			{
				if (!rOn)
				{
					if ((bool)CommRadioManager.instance.commAudioSource)
					{
						CommRadioManager.instance.commAudioSource.PlayOneShot(ssSound);
					}
					_ = Time.time;
					rOn = true;
				}
				else if (rOn && Time.time - lastTransmitTime > stopAfterTransmitTime)
				{
					if ((bool)CommRadioManager.instance.commAudioSource)
					{
						CommRadioManager.instance.commAudioSource.PlayOneShot(ssSound);
					}
					rOn = false;
				}
			}
			yield return null;
		}
	}
}

}