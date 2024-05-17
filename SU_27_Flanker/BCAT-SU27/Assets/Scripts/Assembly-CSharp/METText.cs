using System;
using UnityEngine;
using UnityEngine.UI;

public class METText : MonoBehaviour
{
	public Text text;

	private void Update()
	{
		if ((bool)FlightSceneManager.instance)
		{
			float missionElapsedTime = FlightSceneManager.instance.missionElapsedTime;
			TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0, Mathf.RoundToInt(missionElapsedTime * 1000f));
			string text = string.Format("{0}:{1}:{2}", timeSpan.Hours.ToString("00"), timeSpan.Minutes.ToString("00"), timeSpan.Seconds.ToString("00"));
			this.text.text = text;
		}
	}
}
