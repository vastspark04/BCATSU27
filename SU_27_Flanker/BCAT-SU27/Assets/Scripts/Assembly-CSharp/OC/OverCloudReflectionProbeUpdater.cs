using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace OC{

[ExecuteInEditMode]
[RequireComponent(typeof(ReflectionProbe))]
public class OverCloudReflectionProbeUpdater : MonoBehaviour
{
	[Serializable]
	public enum UpdateMode
	{
		OnSkyChanged,
		OnEnable,
		Realtime
	}

	private ReflectionProbe _reflectionProbe;

	[Tooltip("If and when the reflection probe should be updated. If set to ScriptOnly, the reflection probe will not render unless RenderProbe is manually called.")]
	public UpdateMode updateMode;

	public bool useTimeThreshold = true;

	public float timeThresholdMinutes = 2f;

	[Tooltip("OverCloudReflectionProbeUpdater will set the reflection probe timeSlicingMode to this value.")]
	public ReflectionProbeTimeSlicingMode timeSlicing = ReflectionProbeTimeSlicingMode.IndividualFaces;

	private double lastRenderTime;

	public ReflectionProbe reflectionProbe
	{
		get
		{
			if (!_reflectionProbe)
			{
				_reflectionProbe = GetComponent<ReflectionProbe>();
			}
			return _reflectionProbe;
		}
	}

	private void OnEnable()
	{
		reflectionProbe.mode = ReflectionProbeMode.Realtime;
		reflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
		reflectionProbe.timeSlicingMode = timeSlicing;
		reflectionProbe.RenderProbe();
	}

	private void Start()
	{
		lastRenderTime = OverCloud.timeOfDay.time;
	}

	private void OnValidate()
	{
		reflectionProbe.timeSlicingMode = timeSlicing;
	}

	private void Update()
	{
		if ((bool)reflectionProbe && (updateMode == UpdateMode.Realtime || (updateMode == UpdateMode.OnSkyChanged && OverCloud.skyChanged)) && (!useTimeThreshold || Mathf.Abs((float)(OverCloud.timeOfDay.time - lastRenderTime)) > timeThresholdMinutes / 60f))
		{
			reflectionProbe.RenderProbe();
			lastRenderTime = OverCloud.timeOfDay.time;
		}
	}
}}
