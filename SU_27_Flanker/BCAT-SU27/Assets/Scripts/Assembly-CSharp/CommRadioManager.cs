using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class CommRadioManager : MonoBehaviour
{
	[Serializable]
	public class CachedRadioMessage
	{
		public string messageName;

		public AudioClip sound;

		public List<AudioClip> extraSounds = new List<AudioClip>();

		public AudioClip GetClip()
		{
			int index = UnityEngine.Random.Range(0, extraSounds.Count);
			return extraSounds[index];
		}
	}

	private class QueuedMessage : IEquatable<QueuedMessage>
	{
		public string messageID;

		public AudioClip[] clips;

		public bool duckBGM;

		public bool Equals(QueuedMessage other)
		{
			return messageID.Equals(other.messageID);
		}
	}

	private List<CommRadioSource> audioSources = new List<CommRadioSource>();

	private CommRadioSource currentSource;

	public AudioMixerGroup mixerGroup;

	public AudioClip startStopRadioSound;

	public AudioMixerGroup copilotMixerGroup;

	public AudioMixerGroup opforMixerGroup;

	public AudioClip copilotStartStopSound;

	public List<CachedRadioMessage> cachedMessages;

	public WingmanVoiceProfile defaultWingmanVoice;

	private Dictionary<string, CachedRadioMessage> cachedMessageDictionary = new Dictionary<string, CachedRadioMessage>();

	private Queue<QueuedMessage> messageQueue = new Queue<QueuedMessage>();

	private Queue<QueuedMessage> copilotMessageQueue = new Queue<QueuedMessage>();

	private Coroutine radioRoutine;

	private Coroutine copilotRadioRoutine;

	private List<string> cooldownMessages = new List<string>();

	private bool _stopMessage;

	private bool _stopCopilotMessage;

	private static int nextRandomWingmenVoiceIdx = 0;

	private static List<WingmanVoiceProfile> shuffledWingmenVoices = new List<WingmanVoiceProfile>();

	private bool liveRadioEnabled;

	private bool stopLiveRadio;

	private QueuedMessage _lm;

	private bool liveDuckBgm;

	private uint liveMessageId;

	public static CommRadioManager instance { get; private set; }

	public AudioSource commAudioSource => CommRadioSource.commSource;

	public AudioSource copilotAudioSource => CommRadioSource.copilotCommSource;

	private QueuedMessage liveMessage
	{
		get
		{
			return _lm;
		}
		set
		{
			_lm = value;
		}
	}

	private void Awake()
	{
		instance = this;
		foreach (CachedRadioMessage cachedMessage in cachedMessages)
		{
			if ((bool)cachedMessage.sound)
			{
				cachedMessage.extraSounds.Add(cachedMessage.sound);
			}
			cachedMessageDictionary.Add(cachedMessage.messageName, cachedMessage);
		}
	}

	private void Start()
	{
		EnsureRadioRoutines();
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
	}

	private void EnsureRadioRoutines()
	{
		if (radioRoutine == null)
		{
			radioRoutine = StartCoroutine(RadioRoutine(() => commAudioSource, () => _stopMessage, delegate
			{
				_stopMessage = false;
			}, startStopRadioSound, messageQueue, doLiveMessages: true));
		}
		if (copilotRadioRoutine == null)
		{
			copilotRadioRoutine = StartCoroutine(RadioRoutine(() => copilotAudioSource, () => _stopCopilotMessage, delegate
			{
				_stopCopilotMessage = false;
			}, copilotStartStopSound, copilotMessageQueue, doLiveMessages: false));
		}
	}

	private void Update()
	{
		EnsureRadioRoutines();
	}

	private void LateUpdate()
	{
		if ((bool)currentSource && (bool)CommRadioSource.commSource)
		{
			CommRadioSource.commSource.transform.position = currentSource.transform.position;
		}
	}

	private void OnExitScene()
	{
		StopAllRadioMessages();
	}

	public void SetAudioSource(CommRadioSource aSource)
	{
		if (!(currentSource == aSource))
		{
			currentSource = aSource;
			CommRadioSource.commSource.outputAudioMixerGroup = mixerGroup;
			CommRadioSource.copilotCommSource.outputAudioMixerGroup = copilotMixerGroup;
		}
	}

	private IEnumerator RadioRoutine(Func<AudioSource> GetAudioSource, Func<bool> GetStopCommand, Action ClearStopCommand, AudioClip startStopSound, Queue<QueuedMessage> mQueue, bool doLiveMessages)
	{
		WaitForSeconds startStopWait = new WaitForSeconds(startStopSound.length);
		WaitForSeconds halfSecWait = new WaitForSeconds(0.5f);
		while (base.enabled)
		{
			yield return null;
			AudioSource cAudioSource = GetAudioSource();
			if (GetStopCommand())
			{
				ClearStopCommand();
			}
			if (doLiveMessages && liveRadioEnabled)
			{
				if (liveDuckBgm)
				{
					BGMManager.SetBGMCommDucker();
				}
				if ((bool)cAudioSource)
				{
					cAudioSource.PlayOneShot(startStopSound);
					yield return startStopWait;
				}
				while (liveRadioEnabled || (!stopLiveRadio && liveMessage != null))
				{
					if (liveMessage != null)
					{
						QueuedMessage currLiveMessage2 = liveMessage;
						liveMessage = null;
						int j = 0;
						while (!GetStopCommand() && j < currLiveMessage2.clips.Length)
						{
							AudioClip audioClip = null;
							try
							{
								audioClip = currLiveMessage2.clips[j];
							}
							catch (IndexOutOfRangeException message)
							{
								Debug.LogError(message);
							}
							if (!(audioClip == null))
							{
								if ((bool)cAudioSource)
								{
									cAudioSource.Stop();
									cAudioSource.clip = audioClip;
									cAudioSource.Play();
								}
								float l = audioClip.length;
								float t2 = Time.time;
								while (Time.time - t2 < l)
								{
									if (GetStopCommand() || liveMessage != null)
									{
										_stopMessage = true;
										if ((bool)cAudioSource)
										{
											cAudioSource.Stop();
										}
										t2 = -10000f;
										j = int.MaxValue;
									}
									yield return null;
								}
								if ((bool)cAudioSource)
								{
									cAudioSource.Stop();
								}
								if (GetStopCommand())
								{
									j = int.MaxValue;
								}
							}
							j++;
						}
					}
					ClearStopCommand();
					yield return null;
				}
				if ((bool)cAudioSource)
				{
					cAudioSource.PlayOneShot(startStopSound);
					yield return startStopWait;
				}
				BGMManager.ReleaseBGMCommDucker();
			}
			if (mQueue.Count <= 0 || !cAudioSource)
			{
				continue;
			}
			if (GetStopCommand())
			{
				ClearStopCommand();
				continue;
			}
			if ((bool)cAudioSource)
			{
				cAudioSource.PlayOneShot(startStopSound);
				yield return startStopWait;
			}
			if (GetStopCommand())
			{
				ClearStopCommand();
				continue;
			}
			if ((bool)cAudioSource)
			{
				QueuedMessage currLiveMessage2 = mQueue.Dequeue();
				if (currLiveMessage2.duckBGM)
				{
					BGMManager.SetBGMCommDucker();
				}
				int j = 0;
				while (!GetStopCommand() && j < currLiveMessage2.clips.Length)
				{
					AudioClip audioClip2 = null;
					try
					{
						audioClip2 = currLiveMessage2.clips[j];
					}
					catch (IndexOutOfRangeException message2)
					{
						Debug.LogError(message2);
					}
					if (!(audioClip2 == null))
					{
						if ((bool)cAudioSource)
						{
							cAudioSource.Stop();
							cAudioSource.clip = audioClip2;
							cAudioSource.Play();
						}
						float t2 = audioClip2.length;
						float l = Time.time;
						while (Time.time - l < t2)
						{
							if (GetStopCommand())
							{
								if ((bool)cAudioSource)
								{
									cAudioSource.Stop();
								}
								l = -10000f;
								j = int.MaxValue;
							}
							yield return null;
						}
						if ((bool)cAudioSource)
						{
							cAudioSource.Stop();
						}
						if (GetStopCommand())
						{
							j = int.MaxValue;
						}
					}
					j++;
				}
			}
			if (GetStopCommand())
			{
				ClearStopCommand();
				continue;
			}
			if ((bool)cAudioSource)
			{
				cAudioSource.PlayOneShot(startStopSound);
				yield return startStopWait;
			}
			BGMManager.ReleaseBGMCommDucker();
			yield return halfSecWait;
		}
	}

	public void PlayMessage(string messageName, float cooldownTime = 5f)
	{
		if (cachedMessageDictionary.ContainsKey(messageName))
		{
			if (!CheckIsCooldown(messageName))
			{
				PlayMessage(cachedMessageDictionary[messageName].GetClip(), duckBGM: false, queueBehindLiveRadio: false);
				StartCooldown(messageName, cooldownTime);
			}
		}
		else
		{
			Debug.LogWarning("No radio message for '" + messageName + "'.");
		}
	}

	private void StartCooldown(string message, float time)
	{
		StartCoroutine(CooldownMessageRoutine(message, time));
	}

	private IEnumerator CooldownMessageRoutine(string message, float time)
	{
		cooldownMessages.Add(message);
		yield return new WaitForSeconds(time);
		cooldownMessages.Remove(message);
	}

	private bool CheckIsCooldown(string message)
	{
		return cooldownMessages.Contains(message);
	}

	public void PlayMessage(AudioClip clip, bool duckBGM = false, bool queueBehindLiveRadio = true)
	{
		if ((bool)clip && (!FlightSceneManager.instance || !FlightSceneManager.instance.switchingScene) && (queueBehindLiveRadio || !liveRadioEnabled))
		{
			QueuedMessage queuedMessage = new QueuedMessage();
			queuedMessage.messageID = clip.name;
			queuedMessage.clips = new AudioClip[1] { clip };
			queuedMessage.duckBGM = duckBGM;
			QueuedMessage item = queuedMessage;
			if (!messageQueue.Contains(item))
			{
				messageQueue.Enqueue(item);
			}
		}
	}

	public void PlayCopilotMessage(AudioClip clip, bool duckBGM = false)
	{
		if ((bool)clip && (!FlightSceneManager.instance || !FlightSceneManager.instance.switchingScene))
		{
			QueuedMessage queuedMessage = new QueuedMessage();
			queuedMessage.messageID = clip.name;
			queuedMessage.clips = new AudioClip[1] { clip };
			queuedMessage.duckBGM = duckBGM;
			QueuedMessage item = queuedMessage;
			if (!copilotMessageQueue.Contains(item))
			{
				copilotMessageQueue.Enqueue(item);
			}
		}
	}

	public void PlayMessageString(AudioClip[] clips, bool duckBGM = false, bool queueBehindLiveRadio = true)
	{
		if ((!FlightSceneManager.instance || !FlightSceneManager.instance.switchingScene) && (queueBehindLiveRadio || !liveRadioEnabled))
		{
			QueuedMessage queuedMessage = new QueuedMessage();
			string text = string.Empty;
			queuedMessage.clips = new AudioClip[clips.Length];
			queuedMessage.duckBGM = duckBGM;
			for (int i = 0; i < clips.Length; i++)
			{
				text += clips[i].name;
				queuedMessage.clips[i] = clips[i];
			}
			queuedMessage.messageID = text;
			if (!messageQueue.Contains(queuedMessage))
			{
				messageQueue.Enqueue(queuedMessage);
			}
		}
	}

	public void PlayMessageString(List<AudioClip> clips)
	{
		if (!FlightSceneManager.instance || !FlightSceneManager.instance.switchingScene)
		{
			QueuedMessage queuedMessage = new QueuedMessage();
			string text = string.Empty;
			queuedMessage.clips = new AudioClip[clips.Count];
			for (int i = 0; i < clips.Count; i++)
			{
				text += clips[i].name;
				queuedMessage.clips[i] = clips[i];
			}
			queuedMessage.messageID = text;
			if (!messageQueue.Contains(queuedMessage))
			{
				messageQueue.Enqueue(queuedMessage);
			}
		}
	}

	public void StopCurrentRadioMessage()
	{
		_stopMessage = true;
	}

	public void StopCurrentCopilotMessage()
	{
		_stopCopilotMessage = true;
	}

	public void StopAllRadioMessages()
	{
		StopCurrentRadioMessage();
		messageQueue.Clear();
	}

	public void StopAllCopilotMessages()
	{
		StopCurrentCopilotMessage();
		copilotMessageQueue.Clear();
	}

	public void PlayWingmanMessage(WingmanVoiceProfile.Messages m, WingmanVoiceProfile profile, float cooldown)
	{
		if (!cooldownMessages.Contains(m.ToString()))
		{
			if (profile == null)
			{
				profile = defaultWingmanVoice;
			}
			profile.PlayMessage(m);
			if (cooldown > 0f)
			{
				StartCooldown(m.ToString(), cooldown);
			}
		}
	}

	public void SetCommsVolume(float t)
	{
		float value = Mathf.Lerp(-30f, 8f, Mathf.Sqrt(t));
		if ((bool)commAudioSource)
		{
			commAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("CommAttenuation", value);
		}
	}

	public void SetCommsVolumeMP(float t)
	{
		float value = Mathf.Lerp(-30f, 8f, Mathf.Sqrt(t));
		if ((bool)commAudioSource)
		{
			commAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("CommAttenuationAllied", value);
			commAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("CommAttenuationOpfor", value);
		}
	}

	public void SetCommsVolumeCopilot(float t)
	{
		float value = Mathf.Lerp(-30f, 8f, Mathf.Sqrt(t));
		if ((bool)commAudioSource)
		{
			commAudioSource.outputAudioMixerGroup.audioMixer.SetFloat("CommAttenuationCopilot", value);
		}
	}

	public static void ShuffleVoiceProfiles()
	{
		VTResources.LoadVoiceProfiles();
		List<WingmanVoiceProfile> wingmanVoiceProfiles = VTResources.GetWingmanVoiceProfiles();
		shuffledWingmenVoices.Clear();
		if (wingmanVoiceProfiles.Count > 0)
		{
			while (wingmanVoiceProfiles.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, wingmanVoiceProfiles.Count);
				if (wingmanVoiceProfiles[index].enabled)
				{
					shuffledWingmenVoices.Add(wingmanVoiceProfiles[index]);
				}
				wingmanVoiceProfiles.RemoveAt(index);
			}
		}
		else
		{
			wingmanVoiceProfiles.Add(VTResources.GetDefaultWingmanVoice());
		}
		if (shuffledWingmenVoices.Count == 0)
		{
			shuffledWingmenVoices.Add(VTResources.GetDefaultWingmanVoice());
		}
		nextRandomWingmenVoiceIdx = 0;
	}

	public static WingmanVoiceProfile GetNextRandomWingmanVoice()
	{
		nextRandomWingmenVoiceIdx = (nextRandomWingmenVoiceIdx + 1) % shuffledWingmenVoices.Count;
		return shuffledWingmenVoices[nextRandomWingmenVoiceIdx];
	}

	public void BeginLiveRadio(bool duckBgm)
	{
		Debug.Log("CommRadioManager: Beginning live radio.");
		liveRadioEnabled = true;
		stopLiveRadio = false;
		liveDuckBgm = duckBgm;
		StopAllRadioMessages();
	}

	public void EndLiveRadio()
	{
		if (liveRadioEnabled)
		{
			Debug.Log("CommRadioManager: Ending live radio.");
		}
		liveRadioEnabled = false;
	}

	public void EndLiveRadioImmediate()
	{
		if (liveRadioEnabled)
		{
			Debug.Log("CommRadioManager: Ending live radio immediately.");
		}
		liveRadioEnabled = false;
		stopLiveRadio = true;
	}

	public void PlayLiveRadioMessage(params AudioClip[] clips)
	{
		if (liveRadioEnabled)
		{
			QueuedMessage queuedMessage = new QueuedMessage();
			queuedMessage.clips = clips;
			queuedMessage.messageID = liveMessageId.ToString();
			liveMessageId++;
			liveMessage = queuedMessage;
		}
	}
}
