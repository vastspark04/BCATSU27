using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using UnityEngine.Audio;

namespace VTNetworking{

public class VTNetworkVoice : MonoBehaviour
{
	private class VoiceThreadStatus
	{
		public bool doUpdate;

		public bool hadInputData;
	}

	private class SteamUserVoice
	{
		private class V_AS_Disabler : MonoBehaviour
		{
			public SteamUserVoice voice;

			public AudioSource audioSource;

			private void OnDisable()
			{
				if (voice != null && voice.audioSource == audioSource)
				{
					voice.audioSource = null;
					voice.hasAudioSource = false;
				}
			}

			private void OnDestroy()
			{
				if (voice != null && voice.audioSource == audioSource)
				{
					voice.audioSource = null;
					voice.hasAudioSource = false;
				}
			}
		}

		public SteamId id;

		public object inStreamLock = new object();

		public AudioSource audioSource;

		public bool hasAudioSource;

		public Queue<float> sampleQueue = new Queue<float>();

		public bool isTransmitting;

		private AudioClip incomingStreamClip;

		private int sampleRate;

		private int minQueueCount = 1000;

		private bool reading;

		public SteamUserVoice(SteamId id, int sampleRate)
		{
			this.id = id;
			this.sampleRate = sampleRate;
		}

		public void SetupAudioSource(AudioSource a)
		{
			if ((bool)audioSource && audioSource != a)
			{
				audioSource.Stop();
			}
			if (!incomingStreamClip)
			{
				incomingStreamClip = AudioClip.Create("Steam Voice", sampleRate * 10, 1, sampleRate, stream: true, OnAudioRead);
			}
			audioSource = a;
			audioSource.gameObject.name = new Friend(id).Name + " Voice";
			audioSource.loop = true;
			audioSource.clip = incomingStreamClip;
			audioSource.outputAudioMixerGroup = instance.voiceMixerGroup;
			audioSource.Play();
			V_AS_Disabler v_AS_Disabler = audioSource.gameObject.AddComponent<V_AS_Disabler>();
			v_AS_Disabler.voice = this;
			v_AS_Disabler.audioSource = audioSource;
			hasAudioSource = true;
		}

		~SteamUserVoice()
		{
			DestroyObjects();
		}

		public void DestroyObjects()
		{
			if ((bool)incomingStreamClip)
			{
				UnityEngine.Object.Destroy(incomingStreamClip);
			}
			if ((bool)audioSource)
			{
				UnityEngine.Object.Destroy(audioSource.gameObject);
				hasAudioSource = false;
			}
		}

		private void OnAudioRead(float[] data)
		{
			lock (inStreamLock)
			{
				isTransmitting = false;
				for (int i = 0; i < data.Length; i++)
				{
					if ((reading && sampleQueue.Count > 0) || (!reading && sampleQueue.Count > minQueueCount))
					{
						data[i] = sampleQueue.Dequeue() * 2f;
						isTransmitting = true;
						reading = true;
					}
					else
					{
						data[i] = 0f;
						reading = false;
					}
				}
			}
		}
	}

	private const int VOICE_BUFFER_SIZE = 12288;

	private const int VOICE_P2P_CHANNEL = 2;

	private MemoryStream voiceUpStream;

	private List<SteamId> p2pConnections = new List<SteamId>();

	public static List<ulong> mutes = new List<ulong>();

	private const int INTIAL_DECOMPRESS_BUFFER_SIZE = 20480;

	private byte[] voiceDownBuffer = new byte[12288];

	private MemoryStream voiceDownStream;

	private MemoryStream voiceDecompressedStream;

	private AudioClip incomingStreamClip;

	private RawSourceWaveStream nWaveStream;

	private SampleChannel sampleProvider;

	private float[] inFloatBuffer = new float[20480];

	public AudioMixerGroup voiceMixerGroup;

	public AudioMixerGroup voiceMixerGroupRadio;

	private bool useVTNetworking = true;

	private Lobby currentLobby;

	private bool multithreaded = true;

	private bool vcEnabled;

	private Thread voiceThread;

	private VoiceThreadStatus threadStatus;

	private object statusLock = new object();

	private int sleepMs = 20;

	private bool setVoiceRecord;

	private static P2PSend voiceSendProtocol = P2PSend.UnreliableNoDelay;

	public List<SteamId> sendWhitelist;

	private bool t_sendVoice;

	private static Dictionary<SteamId, SteamUserVoice> voices = new Dictionary<SteamId, SteamUserVoice>();

	private ulong mySteamID;

	public static VTNetworkVoice instance { get; private set; }

	public static float VoiceInputLevel { get; private set; }

	public bool isVoiceChatEnabled => vcEnabled;

	public bool isVoiceRecording { get; private set; }

	public event Action<Friend> OnNewVoiceReady;

	public event Action<Friend> OnVoiceRemoved;

	private void Awake()
	{
		if ((bool)instance)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		instance = this;
		GameSettings.OnAppliedSettings += GameSettings_OnAppliedSettings;
		GameSettings_OnAppliedSettings(GameSettings.CurrentSettings);
		if (GameSettings.TryGetGameSettingValue<int>("VOICE_SEND_PROTOCOL", out var val))
		{
			voiceSendProtocol = (P2PSend)val;
		}
	}

	private void Start()
	{
		mySteamID = SteamClient.SteamId.Value;
	}

	private void GameSettings_OnAppliedSettings(GameSettings s)
	{
		SetVolume(s.GetFloatSetting("VOICE_VOLUME") / 100f);
	}

	public static void SetVolume(float t)
	{
		VTResources.GetEnvironmentAudioMixer().SetFloat("VoiceVolumeSetting", Mathf.Lerp(-50f, 5f, Mathf.Pow(t, 0.5f)));
	}

	public void BeginVoiceChat(Lobby lobby)
	{
		if (vcEnabled)
		{
			Debug.LogError("BeginVoiceChat was called but VC is already enabled!!");
			return;
		}
		Debug.Log("Beginning voice chat.");
		SteamUser.SampleRate = SteamUser.OptimalSampleRate;
		int sampleRate = (int)SteamUser.SampleRate;
		currentLobby = lobby;
		if (!useVTNetworking)
		{
			SteamNetworking.OnP2PSessionRequest = (Action<SteamId>)Delegate.Combine(SteamNetworking.OnP2PSessionRequest, new Action<SteamId>(Voice_OnP2PRequest));
		}
		SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
		foreach (Friend member in lobby.Members)
		{
			if (!member.IsMe)
			{
				SteamMatchmaking_OnLobbyMemberJoined(currentLobby, member);
			}
		}
		if (voiceUpStream == null)
		{
			voiceUpStream = new MemoryStream(12288);
		}
		if (voiceDownStream == null)
		{
			voiceDownStream = new MemoryStream(voiceDownBuffer);
		}
		if (voiceDecompressedStream == null)
		{
			voiceDecompressedStream = new MemoryStream(20480);
		}
		WaveFormat waveFormat = new WaveFormat(sampleRate, 1);
		waveFormat = WaveFormat.CreateCustomFormat(WaveFormatEncoding.Pcm, sampleRate, 1, 16 * sampleRate, 0, 16);
		nWaveStream = new RawSourceWaveStream(voiceDecompressedStream, waveFormat);
		sampleProvider = new SampleChannel(nWaveStream);
		GameSettings.TryGetGameSettingValue<int>("VOICE_SLEEP", out sleepMs);
		if (SteamUser.HasVoiceData)
		{
			SteamUser.ReadVoiceDataBytes();
		}
		if (threadStatus == null)
		{
			threadStatus = new VoiceThreadStatus();
		}
		if (multithreaded)
		{
			voiceThread = new Thread(VoiceThreadWorker);
			voiceThread.Start();
		}
		vcEnabled = true;
		SteamUser.VoiceRecord = true;
		StartCoroutine(VoiceChatRoutine());
	}

	private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
	{
		if ((ulong)lobby.Id == (ulong)currentLobby.Id)
		{
			if (!voices.ContainsKey(friend.Id))
			{
				Debug.Log("VTNetworkVoice setting up new voice for user " + friend.Name);
				SteamUserVoice steamUserVoice = new SteamUserVoice(friend.Id, (int)SteamUser.SampleRate);
				voices.Add(steamUserVoice.id, steamUserVoice);
			}
			this.OnNewVoiceReady?.Invoke(friend);
		}
	}

	private void SteamMatchmaking_OnLobbyMemberLeave(Lobby arg1, Friend friend)
	{
		if (voices.TryGetValue(friend.Id, out var value))
		{
			Debug.Log("VTNetworkVoice removing voice for user " + friend.Name);
			value.DestroyObjects();
			voices.Remove(friend.Id);
			this.OnVoiceRemoved?.Invoke(friend);
		}
	}

	public void EndVoiceChat()
	{
		if (!vcEnabled)
		{
			return;
		}
		vcEnabled = false;
		Debug.Log("Ending voice chat.");
		if (SteamClient.IsValid)
		{
			SteamUser.VoiceRecord = false;
		}
		setVoiceRecord = false;
		if (multithreaded && voiceThread != null)
		{
			voiceThread.Abort();
			voiceThread = null;
		}
		threadStatus.doUpdate = false;
		threadStatus.hadInputData = false;
		nWaveStream.Dispose();
		voiceUpStream.Dispose();
		voiceUpStream = null;
		voiceDecompressedStream.Dispose();
		voiceDecompressedStream = null;
		voiceDownStream.Dispose();
		voiceDownStream = null;
		foreach (SteamUserVoice value in voices.Values)
		{
			value.DestroyObjects();
		}
		voices.Clear();
		UnityEngine.Object.DestroyImmediate(incomingStreamClip);
		if (SteamClient.IsValid)
		{
			foreach (SteamId p2pConnection in p2pConnections)
			{
				SteamNetworking.CloseP2PSessionWithUser(p2pConnection);
			}
		}
		p2pConnections.Clear();
		SteamNetworking.OnP2PSessionRequest = (Action<SteamId>)Delegate.Remove(SteamNetworking.OnP2PSessionRequest, new Action<SteamId>(Voice_OnP2PRequest));
		SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
		SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
		VoiceInputLevel = 0f;
	}

	private void OnDestroy()
	{
		EndVoiceChat();
	}

	private IEnumerator VoiceChatRoutine()
	{
		while (vcEnabled)
		{
			float num = 0f;
			lock (statusLock)
			{
				if (threadStatus != null)
				{
					threadStatus.doUpdate = true;
					num = (threadStatus.hadInputData ? 1 : 0);
				}
			}
			VoiceInputLevel = Mathf.Max(num, Mathf.Lerp(VoiceInputLevel, num, 5f * Time.deltaTime));
			if (!multithreaded)
			{
				VoiceUpdate();
			}
			yield return null;
		}
	}

	public void SetVoiceRecord(bool r)
	{
		if (setVoiceRecord != r)
		{
			setVoiceRecord = r;
			isVoiceRecording = r;
		}
	}

	private void VoiceThreadWorker()
	{
		while (SteamClient.IsValid)
		{
			VoiceUpdate();
			Thread.Sleep(sleepMs);
		}
	}

	private void VoiceUpdate()
	{
		t_sendVoice = setVoiceRecord;
		bool hasVoiceData = SteamUser.HasVoiceData;
		if (hasVoiceData)
		{
			voiceUpStream.Position = 0L;
			int num = SteamUser.ReadVoiceData(voiceUpStream);
			byte[] buffer = voiceUpStream.GetBuffer();
			if (t_sendVoice)
			{
				if (useVTNetworking)
				{
					foreach (Friend member in currentLobby.Members)
					{
						SteamId id = member.Id;
						if ((ulong)id != BDSteamClient.mySteamID && (sendWhitelist == null || sendWhitelist.Contains(id)))
						{
							VTNetworkManager.instance.SendVoiceData(id, buffer, num);
						}
					}
				}
				else
				{
					foreach (Friend member2 in currentLobby.Members)
					{
						SteamId id2 = member2.Id;
						if ((ulong)id2 != BDSteamClient.mySteamID && !p2pConnections.Contains(id2))
						{
							p2pConnections.Add(id2);
						}
					}
					for (int num2 = p2pConnections.Count - 1; num2 >= 0; num2--)
					{
						if (!IsInLobby(p2pConnections[num2]))
						{
							SteamNetworking.CloseP2PSessionWithUser(p2pConnections[num2]);
							p2pConnections.RemoveAt(num2);
						}
						else if (sendWhitelist == null || sendWhitelist.Contains(p2pConnections[num2]))
						{
							SteamNetworking.SendP2PPacket(p2pConnections[num2], buffer, num, 2, voiceSendProtocol);
						}
					}
				}
			}
		}
		lock (statusLock)
		{
			if (threadStatus.doUpdate)
			{
				threadStatus.doUpdate = false;
				threadStatus.hadInputData = hasVoiceData && t_sendVoice;
			}
		}
		if (useVTNetworking)
		{
			return;
		}
		uint size = 0u;
		SteamId steamid = default(SteamId);
		if (!SteamNetworking.ReadP2PPacket(voiceDownBuffer, ref size, ref steamid, 2) || !voices.TryGetValue(steamid, out var value) || !value.hasAudioSource || (mutes != null && mutes.Contains(steamid.Value)))
		{
			return;
		}
		lock (value.inStreamLock)
		{
			voiceDownStream.Position = 0L;
			voiceDecompressedStream.Position = 0L;
			int num3 = SteamUser.DecompressVoice(voiceDownStream, (int)size, voiceDecompressedStream) / 2;
			voiceDecompressedStream.Position = 0L;
			sampleProvider.Read(inFloatBuffer, 0, num3);
			for (int i = 0; i < num3; i++)
			{
				value.sampleQueue.Enqueue(inFloatBuffer[i]);
			}
		}
	}

	public void ReceiveVTNetVoiceData(ulong incomingID, byte[] buffer, int offset, int count)
	{
		if (!voices.TryGetValue(incomingID, out var value) || !value.hasAudioSource || (mutes != null && mutes.Contains(incomingID)))
		{
			return;
		}
		Buffer.BlockCopy(buffer, offset, voiceDownBuffer, 0, count);
		lock (value.inStreamLock)
		{
			voiceDownStream.Position = 0L;
			voiceDecompressedStream.Position = 0L;
			int num = SteamUser.DecompressVoice(voiceDownStream, count, voiceDecompressedStream) / 2;
			voiceDecompressedStream.Position = 0L;
			sampleProvider.Read(inFloatBuffer, 0, num);
			for (int i = 0; i < num; i++)
			{
				value.sampleQueue.Enqueue(inFloatBuffer[i]);
			}
		}
	}

	private bool IsInLobby(SteamId userId)
	{
		foreach (Friend member in currentLobby.Members)
		{
			if ((ulong)member.Id == (ulong)userId)
			{
				return true;
			}
		}
		return false;
	}

	public AudioSource GetUserVoiceSource(SteamId id)
	{
		if ((ulong)id == (ulong)SteamClient.SteamId)
		{
			return null;
		}
		if (voices.TryGetValue(id, out var value))
		{
			if (!value.audioSource)
			{
				AudioSource a = new GameObject().AddComponent<AudioSource>();
				value.SetupAudioSource(a);
			}
			value.audioSource.Play();
			lock (value.inStreamLock)
			{
				value.sampleQueue.Clear();
			}
			return value.audioSource;
		}
		return null;
	}

	public bool IsUserTransmittingVoice(SteamId id)
	{
		if ((ulong)id == mySteamID)
		{
			return VoiceInputLevel > 0.01f;
		}
		if (voices.TryGetValue(id, out var value))
		{
			return value.isTransmitting;
		}
		return false;
	}

	private void Voice_OnP2PRequest(SteamId id)
	{
		bool flag = false;
		foreach (Friend member in currentLobby.Members)
		{
			if ((ulong)member.Id == (ulong)id)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			SteamNetworking.AcceptP2PSessionWithUser(id);
			if (!p2pConnections.Contains(id))
			{
				p2pConnections.Add(id);
			}
		}
	}
}

}