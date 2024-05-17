using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MP3Sharp;
using UnityEngine;

public class MP3LoadTest : MonoBehaviour
{
	public AudioSource audioSource;

	public string songFolderPath;

	public List<string> songs = new List<string>();

	private int songIdx;

	private bool paused;

	private MP3Stream stream;

	private const int startBufferSize = 4;

	private int bufferSize = 4;

	private byte[] buffer = new byte[4];

	private int channels = 1;

	private int sampleSize = 2;

	private bool streamFinished;

	private Coroutine streamRoutine;

	private void Start()
	{
		string[] files = Directory.GetFiles(VTResources.gameRootDirectory + songFolderPath);
		foreach (string text in files)
		{
			if (text.EndsWith(".mp3"))
			{
				songs.Add(text);
			}
		}
	}

	public void PlayButton()
	{
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
		songIdx = (songIdx + 1) % songs.Count;
		PlayButton();
	}

	public void PrevSong()
	{
		StopPlayingSong();
		songIdx--;
		if (songIdx < 0)
		{
			songIdx = songs.Count - 1;
		}
		PlayButton();
	}

	private void StopPlayingSong()
	{
		if (stream != null)
		{
			stream.Close();
		}
		audioSource.clip = null;
		audioSource.Stop();
	}

	private IEnumerator StreamSongRoutine()
	{
		string fileName = songs[songIdx];
		stream = new MP3Stream(fileName);
		stream.Read(buffer, 0, 4);
		audioSource.loop = false;
		channels = ((stream.Format == SoundFormat.Pcm16BitMono) ? 1 : 2);
		Debug.Log("freq: " + stream.Frequency);
		AudioClip audioClip = AudioClip.Create("Song", (int)stream.Length, channels, stream.Frequency, stream: true, OnAudioRead);
		audioSource.clip = audioClip;
		audioSource.loop = true;
		audioSource.Play();
		streamFinished = false;
		Debug.Log("clip length: " + audioClip.length);
		while (!streamFinished)
		{
			yield return null;
		}
		StopPlayingSong();
		NextSong();
	}

	private void OnAudioRead(float[] data)
	{
		if (!stream.CanRead)
		{
			return;
		}
		int num = data.Length;
		if (num * sampleSize > bufferSize)
		{
			bufferSize = num * sampleSize;
			buffer = new byte[bufferSize];
			Debug.Log("setting new buffer size: " + bufferSize);
		}
		if (stream.Read(buffer, 0, num * sampleSize) > 0)
		{
			float num2 = 32767f;
			int num3 = 0;
			int num4 = 0;
			while (num3 < num)
			{
				short num5 = BitConverter.ToInt16(buffer, num4);
				data[num3] = (float)num5 / num2;
				num3++;
				num4 += sampleSize;
			}
		}
		else
		{
			streamFinished = true;
		}
	}
}
