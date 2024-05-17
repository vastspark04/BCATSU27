using OC;
using UnityEngine;

public class VTOverCloudTester : MonoBehaviour
{
	private OverCloudReflectionProbeUpdater probeUpdater;

	public float lineHeight = 18f;

	private float[] csCoverageOptions = new float[3] { 0.125f, 0.25f, 0.5f };

	private int csCoverageOpt = 2;

	private OverCloudCamera oCam;

	private bool showDebug;

	private DownSampleFactor[] dsOpts = new DownSampleFactor[4]
	{
		DownSampleFactor.Full,
		DownSampleFactor.Half,
		DownSampleFactor.Quarter,
		DownSampleFactor.Eight
	};

	private int currDsFac = -1;

	private void Awake()
	{
		probeUpdater = GetComponentInChildren<OverCloudReflectionProbeUpdater>();
	}

	private void Update()
	{
		if (!VTResources.useOverCloud)
		{
			base.enabled = false;
			return;
		}
		if (Input.GetKeyDown(KeyCode.O))
		{
			showDebug = !showDebug;
		}
		if (Input.GetKey(KeyCode.P))
		{
			OverCloud.timeOfDay.time += 2f * Time.deltaTime;
		}
	}

	private void OnGUI()
	{
		if (!showDebug)
		{
			return;
		}
		int num = Mathf.RoundToInt(120f / lineHeight);
		OverCloud.VolumetricClouds volumetricClouds = OverCloud.volumetricClouds;
		GUI.Label(SettingRect(num++), "VCloud Radius: " + volumetricClouds.cloudPlaneRadius);
		volumetricClouds.cloudPlaneRadius = GUI.HorizontalSlider(SliderRect(num++), volumetricClouds.cloudPlaneRadius, 0f, 32000f);
		GUI.Label(SettingRect(num++), "Particle Count: " + volumetricClouds.particleCount);
		float f = GUI.HorizontalSlider(SliderRect(num++), volumetricClouds.particleCount, 0f, 16000f);
		volumetricClouds.particleCount = Mathf.RoundToInt(f);
		OverCloud.Lighting.CloudShadows cloudShadows = OverCloud.lighting.cloudShadows;
		cloudShadows.enabled = GUI.Toggle(SettingRect(num++), cloudShadows.enabled, "Cloud Shadows");
		GUI.Label(SettingRect(num++), "Cloud Coverage: " + cloudShadows.coverage);
		csCoverageOpt = Mathf.RoundToInt(GUI.HorizontalSlider(SliderRect(num++), csCoverageOpt, 0f, 2f));
		cloudShadows.coverage = csCoverageOptions[csCoverageOpt];
		probeUpdater.useTimeThreshold = GUI.Toggle(SettingRect(num++), probeUpdater.useTimeThreshold, $"Use Probe Updater Timer ({probeUpdater.timeThresholdMinutes})");
		probeUpdater.timeThresholdMinutes = GUI.HorizontalSlider(SliderRect(num++), probeUpdater.timeThresholdMinutes, 0.01f, 10f);
		OverCloud.TimeOfDay timeOfDay = OverCloud.timeOfDay;
		GUI.Label(SettingRect(num++), "Time of Day: " + timeOfDay.time);
		float value = (float)timeOfDay.time;
		value = GUI.HorizontalSlider(SliderRect(num++), value, 0f, 24f);
		timeOfDay.time = value;
		num++;
		GUI.Label(SettingRect(num++), "Presets");
		PresetButton("Clear", num++);
		PresetButton("Broken", num++);
		PresetButton("Overcast", num++);
		PresetButton("Foggy", num++);
		PresetButton("Rain", num++);
		PresetButton("Storm", num++);
		num++;
		if (GUI.Button(SliderRect(num++), "Get Camera"))
		{
			oCam = Camera.main.GetComponent<OverCloudCamera>();
		}
		if (oCam != null)
		{
			oCam.renderScatteringMask = GUI.Toggle(SettingRect(num++), oCam.renderScatteringMask, "Render Scatter Mask");
			oCam.includeCascadedShadows = GUI.Toggle(SettingRect(num++), oCam.includeCascadedShadows, "Include Cascaded Shadow");
			oCam.downsample2DClouds = GUI.Toggle(SettingRect(num++), oCam.downsample2DClouds, "Downsample 2D Clouds");
			if (currDsFac == -1)
			{
				currDsFac = dsOpts.IndexOf(oCam.downsampleFactor);
			}
			currDsFac = Mathf.RoundToInt(GUI.HorizontalSlider(SliderRect(num++), currDsFac, 0f, dsOpts.Length - 1));
			if (oCam.downsampleFactor != dsOpts[currDsFac])
			{
				oCam.downsampleFactor = dsOpts[currDsFac];
			}
		}
	}

	private Rect SettingRect(int line)
	{
		return new Rect(lineHeight, (float)line * lineHeight, 1000f, lineHeight);
	}

	private Rect SliderRect(int line)
	{
		return new Rect(lineHeight, (float)line * lineHeight, 600f, lineHeight);
	}

	private void PresetButton(string preset, int line)
	{
		if (GUI.Button(SliderRect(line), preset))
		{
			OverCloud.SetWeatherPreset(preset);
		}
	}
}
