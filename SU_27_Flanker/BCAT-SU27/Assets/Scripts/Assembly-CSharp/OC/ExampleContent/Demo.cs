using UnityEngine;
using UnityEngine.UI;

namespace OC.ExampleContent{

public class Demo : MonoBehaviour
{
	[SerializeField]
	private OverCloudCamera m_OverCloudCamera;

	[SerializeField]
	private Text m_ControlsText;

	[SerializeField]
	private Text m_OutputText;

	[SerializeField]
	private Text m_FPSText;

	[SerializeField]
	private ReflectionProbe m_DynamicReflectionProbe;

	[SerializeField]
	private OverCloudProbe m_CloudProbe;

	[SerializeField]
	private AudioLowPassFilter m_PropellerFilter;

	private string m_CachedString;

	private int m_CloudQuality = 2;

	private void Start()
	{
		m_CachedString = m_ControlsText.text;
		UpdateText();
		Application.targetFrameRate = 60;
	}

	private void UpdateText()
	{
		m_ControlsText.text = m_CachedString + "\n";
		m_ControlsText.text += "Cloud Quality: ";
		switch (m_CloudQuality)
		{
		case 0:
			m_ControlsText.text += "Low";
			break;
		case 1:
			m_ControlsText.text += "Medium";
			break;
		case 2:
			m_ControlsText.text += "High";
			break;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown("q"))
		{
			m_CloudQuality++;
			if (m_CloudQuality > 2)
			{
				m_CloudQuality = 0;
			}
			switch (m_CloudQuality)
			{
			case 0:
				m_OverCloudCamera.downsampleFactor = DownSampleFactor.Quarter;
				m_OverCloudCamera.renderScatteringMask = false;
				m_OverCloudCamera.highQualityClouds = false;
				m_OverCloudCamera.lightSampleCount = SampleCount.Low;
				break;
			case 1:
				m_OverCloudCamera.downsampleFactor = DownSampleFactor.Quarter;
				m_OverCloudCamera.renderScatteringMask = true;
				m_OverCloudCamera.highQualityClouds = true;
				m_OverCloudCamera.lightSampleCount = SampleCount.Normal;
				break;
			case 2:
				m_OverCloudCamera.downsampleFactor = DownSampleFactor.Half;
				m_OverCloudCamera.renderScatteringMask = true;
				m_OverCloudCamera.highQualityClouds = true;
				m_OverCloudCamera.lightSampleCount = SampleCount.High;
				break;
			}
			UpdateText();
		}
		m_FPSText.text = "FPS: " + Mathf.CeilToInt(1f / Time.smoothDeltaTime);
		if (Input.GetKeyDown("1"))
		{
			OverCloud.SetWeatherPreset("Clear");
		}
		if (Input.GetKeyDown("2"))
		{
			OverCloud.SetWeatherPreset("Broken");
		}
		if (Input.GetKeyDown("3"))
		{
			OverCloud.SetWeatherPreset("Overcast");
		}
		if (Input.GetKeyDown("4"))
		{
			OverCloud.SetWeatherPreset("Foggy");
		}
		if (Input.GetKeyDown("5"))
		{
			OverCloud.SetWeatherPreset("Rain");
		}
		if (Input.GetKeyDown("6"))
		{
			OverCloud.SetWeatherPreset("Storm");
		}
		if (Input.GetKeyDown("h"))
		{
			m_OutputText.enabled = !m_ControlsText.enabled;
			m_FPSText.enabled = !m_ControlsText.enabled;
			m_ControlsText.enabled = !m_ControlsText.enabled;
		}
		if (Input.GetKeyDown("r"))
		{
			m_DynamicReflectionProbe.enabled = !m_DynamicReflectionProbe.enabled;
		}
		m_PropellerFilter.cutoffFrequency = Mathf.Lerp(22000f, 800f, m_CloudProbe.density);
		float axis = Input.GetAxis("Mouse ScrollWheel");
		if (axis > Mathf.Epsilon)
		{
			OverCloud.timeOfDay.time += 0.20000000298023224;
		}
		else if (axis < 0f - Mathf.Epsilon)
		{
			OverCloud.timeOfDay.time -= 0.20000000298023224;
		}
		if (Input.GetKeyDown("space"))
		{
			OverCloud.timeOfDay.play = !OverCloud.timeOfDay.play;
		}
		m_OutputText.text = "Time of Day - " + (OverCloud.timeOfDay.play ? "Playing" : "Paused") + "\n";
		Text outputText = m_OutputText;
		outputText.text = outputText.text + "Timescale - " + OverCloud.timeOfDay.playSpeed + "\n";
		Text outputText2 = m_OutputText;
		outputText2.text = outputText2.text + "Year - " + OverCloud.timeOfDay.year + "\n";
		Text outputText3 = m_OutputText;
		outputText3.text = outputText3.text + "Month - " + OverCloud.timeOfDay.month + "\n";
		Text outputText4 = m_OutputText;
		outputText4.text = outputText4.text + "Day - " + OverCloud.timeOfDay.day + "\n";
		int hour = OverCloud.timeOfDay.hour;
		int minute = OverCloud.timeOfDay.minute;
		int second = OverCloud.timeOfDay.second;
		Text outputText5 = m_OutputText;
		outputText5.text = outputText5.text + "Time - " + ((hour < 10) ? "0" : "") + hour + ":" + ((minute < 10) ? "0" : "") + minute + ":" + ((second < 10) ? "0" : "") + second + "\n";
	}
}
}