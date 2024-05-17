using System;
using UnityEngine;

public static class CurrentCameraEvents
{
	public static event Action<Camera> OnRenderCamera;

	public static event Action<Camera> OnCameraPreCull;

	public static void ReportRenderingCamera(Camera c)
	{
		CurrentCameraEvents.OnRenderCamera?.Invoke(c);
	}

	public static void ReportCameraPreCull(Camera c)
	{
		CurrentCameraEvents.OnCameraPreCull?.Invoke(c);
	}
}
