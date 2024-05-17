using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VTAudioFilePlayer
{
	private struct CachedAudio
	{
		public string path;

		public AudioClip clip;

		public DateTime timeModified;

		public VTScenario.ScenarioSystemActions.VTSRadioMessagePlayer.MP3ClipStreamer mp3Streamer;
	}

	private AudioSource audioSource;

	private bool isPlaying;

	private bool isMp3Stream;

	private Dictionary<string, CachedAudio> audioCache = new Dictionary<string, CachedAudio>();

	public VTAudioFilePlayer(AudioSource source)
	{
		audioSource = source;
	}

	public void Play(string path, bool loop = false)
	{
		if (!audioSource)
		{
			Debug.LogError("VTAudioFilePlayer: audioSource is null.");
			return;
		}
		if (!File.Exists(path))
		{
			Debug.LogErrorFormat("VTAudioFilePlayer: File not found: {0}", path);
			return;
		}
		if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		if (path.ToLower().EndsWith("mp3"))
		{
			DateTime dateTime = VTResources.SafelyGetLastWriteTime(path);
			bool flag = false;
			if (flag = audioCache.TryGetValue(path, out var value) && dateTime == value.timeModified)
			{
				value.mp3Streamer.Rewind();
				audioSource.clip = value.clip;
				Debug.Log("VTAudioFilePlayer: Rewinding cached mp3 audio.");
			}
			else
			{
				VTScenario.ScenarioSystemActions.VTSRadioMessagePlayer.MP3ClipStreamer mP3ClipStreamer = new VTScenario.ScenarioSystemActions.VTSRadioMessagePlayer.MP3ClipStreamer(path, loop);
				CachedAudio cachedAudio = default(CachedAudio);
				cachedAudio.clip = mP3ClipStreamer.audioClip;
				cachedAudio.mp3Streamer = mP3ClipStreamer;
				cachedAudio.timeModified = dateTime;
				value = cachedAudio;
				if (flag)
				{
					Debug.Log("VTAudioFilePlayer: Replacing cached mp3 audio.");
					audioCache[path] = value;
				}
				else
				{
					Debug.Log("VTAudioFilePlayer: Caching new mp3 audio.");
					audioCache.Add(path, value);
				}
				audioSource.clip = mP3ClipStreamer.audioClip;
			}
			audioSource.Play();
			isPlaying = true;
		}
		else
		{
			WWW wWW = new WWW("file://" + path);
			while (!wWW.isDone)
			{
			}
			AudioClip audioClip = wWW.GetAudioClip();
			while (audioClip.loadState != AudioDataLoadState.Loaded)
			{
			}
		}
	}
}
