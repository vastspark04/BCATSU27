using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NAudio.Wave;
using UnityEngine;

public class CockpitRadio : ElectronicComponent
{
	private class ReaderCreationStatus
	{
		public string audioFilePath;

		public Mp3FileReaderVT.AudioFileReader reader;

		private bool _ready;

		private object statusLock;

		public bool ready
		{
			get
			{
				lock (statusLock)
				{
					return _ready;
				}
			}
			set
			{
				lock (statusLock)
				{
					_ready = value;
				}
			}
		}

		public ReaderCreationStatus(string path)
		{
			audioFilePath = path;
			statusLock = new object();
		}
	}

	public AudioSource audioSource;

	private List<string> shuffledSongs = new List<string>();

	private List<string> origSongs = new List<string>();

	private int songIdx;

	private bool paused;

	private Mp3FileReaderVT.AudioFileReader reader;

	private bool shuffle;

	private bool streamFinished;

	private Coroutine streamRoutine;

	private int sampleSize;

	private byte[] readBuffer = new byte[1];

	private WaveBuffer waveBuffer;

	public static CockpitRadio instance { get; private set; }

	private List<string> songs
	{
		get
		{
			if (!shuffle)
			{
				return origSongs;
			}
			return shuffledSongs;
		}
	}

	public bool IsShuffle => shuffle;

	public bool isPlaying
	{
		get
		{
			if (!paused && (bool)audioSource)
			{
				return audioSource.isPlaying;
			}
			return false;
		}
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		string text = GameSettings.RADIO_MUSIC_PATH;
		if (!Directory.Exists(text))
		{
			Debug.LogError("Cockpit radio song folder path not found: " + text + ". Using default path.");
			text = GameSettings.defaultRadioMusicPath;
			if (!Directory.Exists(text))
			{
				Debug.LogError("Cockpit radio default song folder path not found: " + text + ". Disabling cockpit radio.");
				try
				{
					Directory.CreateDirectory(text);
					Debug.Log("Cockpit radio created the default song folder for future use.");
					return;
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception when trying to create the default cockpit radio folder: \n" + ex);
					return;
				}
			}
		}
		string[] files = Directory.GetFiles(Path.GetFullPath(text));
		foreach (string text2 in files)
		{
			if (text2.EndsWith(".mp3"))
			{
				shuffledSongs.Add(text2);
				origSongs.Add(text2);
			}
		}
	}

	public void ToggleShuffle()
	{
		shuffle = !shuffle;
		if (shuffle)
		{
			shuffledSongs.Clear();
			for (int i = 0; i < origSongs.Count; i++)
			{
				int index = UnityEngine.Random.Range(0, shuffledSongs.Count);
				shuffledSongs.Insert(index, origSongs[i]);
			}
		}
	}

	private void OnDestroy()
	{
		if (reader != null)
		{
			reader.Dispose();
			reader = null;
		}
	}

	public void PlayButton()
	{
		BGMManager.FadeOut();
		if (songs.Count < 1)
		{
			return;
		}
		if ((bool)audioSource.clip)
		{
			if (paused)
			{
				audioSource.UnPause();
				paused = false;
			}
			else
			{
				audioSource.Pause();
				paused = true;
			}
		}
		else
		{
			if (streamRoutine != null)
			{
				StopCoroutine(streamRoutine);
			}
			streamRoutine = StartCoroutine(StreamSongRoutine());
		}
	}

	public void NextSong()
	{
		StopPlayingSong();
		if (songs.Count >= 1)
		{
			songIdx = (songIdx + 1) % songs.Count;
			PlayButton();
		}
	}

	public void PrevSong()
	{
		StopPlayingSong();
		if (songs.Count >= 1)
		{
			songIdx--;
			if (songIdx < 0)
			{
				songIdx = songs.Count - 1;
			}
			PlayButton();
		}
	}

	private void StopPlayingSong()
	{
		if (reader != null)
		{
			reader.Dispose();
			reader = null;
		}
		audioSource.clip = null;
		audioSource.Stop();
	}

	private IEnumerator StreamSongRoutine()
	{
		string fullPath = songs[songIdx];
		Debug.Log("Opening mp3 stream: " + fullPath);
		ReaderCreationStatus rStatus = new ReaderCreationStatus(fullPath);
		ThreadPool.QueueUserWorkItem(AsyncCreateReader, rStatus);
		while (!rStatus.ready)
		{
			yield return null;
		}
		reader = rStatus.reader;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
		try
		{
			sampleSize = reader.WaveFormat.BitsPerSample / 8;
			AudioClip audioClip = AudioClip.Create(fileNameWithoutExtension, reader.LengthSamples, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, stream: true, OnAudioRead);
			audioSource.loop = false;
			audioSource.clip = audioClip;
			audioSource.loop = true;
			audioSource.Play();
			streamFinished = false;
			Debug.Log("Playing song: " + fileNameWithoutExtension + " clip length: " + audioClip.length);
		}
		catch (Exception ex)
		{
			Debug.Log("Error in clip creation" + ex);
		}
		while (!streamFinished)
		{
			yield return null;
		}
		StopPlayingSong();
		NextSong();
	}

	private void AsyncCreateReader(object status)
	{
		ReaderCreationStatus readerCreationStatus = (ReaderCreationStatus)status;
		try
		{
			readerCreationStatus.reader = new Mp3FileReaderVT.AudioFileReader(readerCreationStatus.audioFilePath);
			readerCreationStatus.ready = true;
		}
		catch (Exception ex)
		{
			Debug.LogError("Error attempting to open stream for " + readerCreationStatus.audioFilePath + "\n" + ex);
		}
	}

	private void OnAudioRead(float[] data)
	{
		try
		{
			if (reader != null)
			{
				if (((ISampleProvider)reader).Read(data, 0, data.Length) == 0)
				{
					streamFinished = true;
				}
				return;
			}
			int num = data.Length * sampleSize;
			if (num > readBuffer.Length)
			{
				readBuffer = new byte[num];
				waveBuffer = new WaveBuffer(readBuffer);
			}
			else if (waveBuffer == null)
			{
				waveBuffer = new WaveBuffer(readBuffer);
			}
			if (reader.Read(readBuffer, 0, num) == 0)
			{
				streamFinished = true;
				return;
			}
			for (int i = 0; i < data.Length; i++)
			{
				data[i] = waveBuffer.FloatBuffer[i];
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Error in audio read callback: " + ex);
		}
	}
}
