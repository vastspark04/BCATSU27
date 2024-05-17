using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[Serializable]
public class SerializedScenario
{
	public string scenarioID;

	[HideInInspector]
	public string scenarioConfig;

	public Texture2D image;

	public Texture2D[] images;

	public string[] imagePaths;

	public AudioClip[] audioClips;

	public string[] audioClipPaths;

	public VideoClip[] videoClips;

	public string[] videoClipPaths;

	private Dictionary<string, AudioClip> audioDictionary;

	private Dictionary<string, Texture2D> imageDictionary;

	private Dictionary<string, VideoClip> videoDictionary;

	private void CreateDictionaries()
	{
		imageDictionary = new Dictionary<string, Texture2D>();
		for (int i = 0; i < imagePaths.Length; i++)
		{
			imageDictionary.Add(imagePaths[i], images[i]);
		}
		audioDictionary = new Dictionary<string, AudioClip>();
		for (int j = 0; j < audioClipPaths.Length; j++)
		{
			audioDictionary.Add(audioClipPaths[j], audioClips[j]);
		}
		videoDictionary = new Dictionary<string, VideoClip>();
		if (videoClipPaths != null)
		{
			for (int k = 0; k < videoClipPaths.Length; k++)
			{
				videoDictionary.Add(videoClipPaths[k], videoClips[k]);
			}
		}
	}

	public Texture2D GetTexture(string path)
	{
		if (imageDictionary == null || imageDictionary.Count == 0)
		{
			CreateDictionaries();
		}
		try
		{
			return imageDictionary[path];
		}
		catch (KeyNotFoundException)
		{
			Debug.LogError("Key not found for texture: " + path);
			return null;
		}
	}

	public AudioClip GetAudioClip(string path)
	{
		if (audioDictionary == null || audioDictionary.Count == 0)
		{
			CreateDictionaries();
		}
		return audioDictionary[path];
	}

	public VideoClip GetVideoClip(string path)
	{
		if (videoDictionary == null || videoDictionary.Count == 0)
		{
			CreateDictionaries();
		}
		if (videoDictionary.TryGetValue(path, out var value))
		{
			return value;
		}
		string text = string.Empty;
		foreach (string key in videoDictionary.Keys)
		{
			text = text + key + "\n";
		}
		Debug.Log("Video clip at path \"" + path + "\" was not found. Available paths:\n" + text);
		return null;
	}
}
