using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WingmanVoiceProfileTest : MonoBehaviour
{
	public WingmanVoiceProfile profile;

	private List<WingmanVoiceProfile> profiles;

	private void Start()
	{
		VTResources.LoadVoiceProfiles();
		profiles = VTResources.GetWingmanVoiceProfiles();
	}

	private void PlayAll()
	{
		for (int i = 0; i < Enum.GetValues(typeof(WingmanVoiceProfile.Messages)).Length; i++)
		{
			WingmanVoiceProfile.Messages messages = (WingmanVoiceProfile.Messages)i;
			WingmanVoiceProfile.MessageAudio[] messageProfiles = profile.messageProfiles;
			foreach (WingmanVoiceProfile.MessageAudio messageAudio in messageProfiles)
			{
				if (messageAudio.messageType == messages && messageAudio.clips != null)
				{
					AudioClip[] clips = messageAudio.clips;
					foreach (AudioClip clip in clips)
					{
						CommRadioManager.instance.PlayMessage(clip);
					}
				}
			}
		}
	}

	private void OnGUI()
	{
		float num = 10f;
		float num2 = 155f;
		float num3 = 20f;
		float num4 = 40f;
		float num5 = 10f;
		float num6 = num;
		float num7 = 80f;
		foreach (WingmanVoiceProfile profile in profiles)
		{
			if (GUI.Button(new Rect(num6, num5, num7, num3), profile.name))
			{
				this.profile = profile;
			}
			num6 += num7;
		}
		num5 += num3;
		if (this.profile != null)
		{
			GUI.Label(new Rect(num, num5, 100f, num3), this.profile.name);
			num5 += num3;
			if (GUI.Button(new Rect(num, num5, 100f, num3), "Play All"))
			{
				PlayAll();
			}
			num5 += num3;
			for (int i = 0; i < this.profile.messageProfiles.Length; i++)
			{
				WingmanVoiceProfile.MessageAudio messageAudio = this.profile.messageProfiles[i];
				float num8 = num;
				GUI.Label(new Rect(num8, num5, num2, num3), messageAudio.messageType.ToString());
				num8 += num2;
				for (int j = 0; j < messageAudio.clips.Length; j++)
				{
					AudioClip clip = messageAudio.clips[j];
					if (GUI.Button(new Rect(num8, num5, num4, num3), j.ToString()))
					{
						CommRadioManager.instance.PlayMessage(clip);
					}
					num8 += num4;
				}
				num5 += num3;
			}
			if (GUI.Button(new Rect(num, num5, 100f, num3), "Stop"))
			{
				CommRadioManager.instance.StopAllRadioMessages();
			}
		}
		num5 += num3 * 2f;
		if (GUI.Button(new Rect(num, num5, 100f, num3), "Quit"))
		{
			SceneManager.LoadScene("SamplerScene");
		}
	}
}
