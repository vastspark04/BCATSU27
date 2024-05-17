using System.Collections;
using System.IO;
using NAudio.Wave;
using UnityEngine;

public class TestNAudio : MonoBehaviour
{
	public AudioSource audioSource;

	public string filepath;

	private AudioFileReader reader;

	private float[] initialBuffer = new float[8];

	private bool readDone;

	private IEnumerator Start()
	{
		yield return new WaitForSeconds(2f);
		if (!audioSource)
		{
			audioSource = GetComponent<AudioSource>();
		}
		reader = new AudioFileReader(filepath);
		AudioClip clip = AudioClip.Create(filepath, (int)reader.Length, reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, stream: true, OnAudioRead);
		reader.Seek(0L, SeekOrigin.Begin);
		audioSource.clip = clip;
		audioSource.Play();
		while (!readDone)
		{
			yield return null;
		}
		audioSource.Stop();
		audioSource.clip = null;
	}

	private void OnAudioRead(float[] data)
	{
		if (reader.Read(data, 0, data.Length) == 0)
		{
			readDone = true;
		}
	}
}
