using System;
using UnityEngine;

[CreateAssetMenu]
public class WingmanVoiceProfile : ScriptableObject
{
	public enum Messages
	{
		None = -1,
		Fox2,
		Fox3,
		Rifle,
		Bruiser,
		Magnum,
		Pickle,
		Guns,
		Shack,
		GroundMiss,
		Splash,
		AirMiss,
		EngagingTargets,
		CopyAttackOrder,
		AttackOrderComplete,
		DefendingMissile,
		DefeatedMissile,
		MissileOnPlayer,
		BanditOnYourSix,
		LowFuel,
		ShotDown,
		Copy,
		Deny,
		ReturningToBase
	}

	[Serializable]
	public class MessageAudio
	{
		public Messages messageType;

		public AudioClip[] clips;

		public AudioClip GetRandomClip()
		{
			if (clips == null || clips.Length == 0)
			{
				return null;
			}
			int num = UnityEngine.Random.Range(0, clips.Length);
			return clips[num];
		}
	}

	public string entryVersion;

	public MessageAudio[] messageProfiles;

	public bool enabled { get; set; }

	public void PlayMessage(Messages m)
	{
		if (m != Messages.None)
		{
			AudioClip randomClip = messageProfiles[(int)m].GetRandomClip();
			if (randomClip != null)
			{
				CommRadioManager.instance.PlayMessage(randomClip, duckBGM: false, queueBehindLiveRadio: false);
			}
		}
	}

	public void PlayRandomMessage()
	{
		int num = UnityEngine.Random.Range(0, messageProfiles.Length);
		for (int i = 0; i < messageProfiles.Length; i++)
		{
			if (messageProfiles[num].clips != null && messageProfiles[num].clips.Length != 0)
			{
				PlayMessage(messageProfiles[num].messageType);
				break;
			}
			num = (num + 1) % messageProfiles.Length;
		}
	}

	[ContextMenu("Remove Nulls")]
	public void ContextRemoveNulls()
	{
	}

	[ContextMenu("Check For Nulls")]
	public void ContextCheckForNulls()
	{
		Debug.Log("Checking for problems in " + base.name);
		MessageAudio[] array = messageProfiles;
		foreach (MessageAudio messageAudio in array)
		{
			if (messageAudio.clips == null || messageAudio.clips.Length == 0)
			{
				Debug.Log(messageAudio.messageType.ToString() + " clips array is null or empty!");
				continue;
			}
			bool flag = false;
			AudioClip[] clips = messageAudio.clips;
			foreach (AudioClip audioClip in clips)
			{
				if (!flag && audioClip == null)
				{
					flag = true;
					Debug.Log(messageAudio.messageType.ToString() + " has one or more null clips!");
				}
			}
		}
	}

	[ContextMenu("Setup Profiles")]
	public void SetupProfiles()
	{
	}

	[ContextMenu("Create Folder")]
	public void CreatFolder()
	{
	}
}
