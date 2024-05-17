using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GroundCrewVoiceTest : MonoBehaviour
{
	private class GCButton
	{
		private UnityAction action;

		public Rect rect;

		private string label;

		public GCButton(Rect rect, string label, UnityAction action)
		{
			this.rect = rect;
			this.label = label;
			this.action = action;
		}

		public void Draw()
		{
			if (GUI.Button(rect, label))
			{
				action();
			}
		}
	}

	private class GCProfile
	{
		public GCButton profileButton;

		public List<GCButton> lineButtons;

		public List<GCButton> variantButtons;
	}

	public float buttonHeight = 20f;

	public float profileButtonWidth;

	public float lineIDWidth;

	public float lineVariantWidth;

	private Rect highlightProfileRect;

	private Rect highlightVariantRect;

	private List<GroundCrewVoiceProfile> profiles;

	private List<GCProfile> testProfiles;

	private GCProfile currProfile;

	private GUIStyle highlightStyle;

	private void Awake()
	{
		highlightStyle = new GUIStyle();
		float num = 10f;
		float num2 = 10f;
		profiles = VTResources.GetAllGroundCrewVoices();
		testProfiles = new List<GCProfile>();
		for (int i = 0; i < profiles.Count; i++)
		{
			GCProfile gCProfile = new GCProfile();
			testProfiles.Add(gCProfile);
			int pIdx = i;
			gCProfile.profileButton = new GCButton(new Rect(num2 + (float)i * profileButtonWidth, num, profileButtonWidth, buttonHeight), profiles[i].name, delegate
			{
				SelectProfile(pIdx);
			});
			gCProfile.variantButtons = new List<GCButton>();
			gCProfile.lineButtons = new List<GCButton>();
			float num3 = num + buttonHeight;
			foreach (GroundCrewVoiceProfile.GroundCrewMessages id in Enum.GetValues(typeof(GroundCrewVoiceProfile.GroundCrewMessages)))
			{
				GCButton item = new GCButton(new Rect(num2, num3, lineIDWidth, buttonHeight), id.ToString(), delegate
				{
					profiles[pIdx].PlayMessage(id);
				});
				gCProfile.lineButtons.Add(item);
				float num4 = num2 + lineIDWidth;
				AudioClip[] clips = profiles[i].GetAllClips(id);
				for (int j = 0; j < clips.Length; j++)
				{
					Rect cbRect = new Rect(num4, num3, lineVariantWidth, buttonHeight);
					int cIdx = j;
					GCButton item2 = new GCButton(cbRect, j.ToString(), delegate
					{
						CommRadioManager.instance.PlayMessage(clips[cIdx]);
						highlightVariantRect = cbRect;
					});
					gCProfile.variantButtons.Add(item2);
					num4 += lineVariantWidth;
				}
				num3 += buttonHeight;
			}
		}
	}

	private void SelectProfile(int idx)
	{
		currProfile = testProfiles[idx];
		highlightProfileRect = currProfile.profileButton.rect;
	}

	private void OnGUI()
	{
		GUI.color = Color.green;
		GUI.Box(highlightProfileRect, string.Empty, highlightStyle);
		GUI.Box(highlightVariantRect, string.Empty, highlightStyle);
		GUI.color = Color.white;
		foreach (GCProfile testProfile in testProfiles)
		{
			testProfile.profileButton.Draw();
		}
		if (currProfile == null)
		{
			return;
		}
		foreach (GCButton lineButton in currProfile.lineButtons)
		{
			lineButton.Draw();
		}
		foreach (GCButton variantButton in currProfile.variantButtons)
		{
			variantButton.Draw();
		}
	}
}
