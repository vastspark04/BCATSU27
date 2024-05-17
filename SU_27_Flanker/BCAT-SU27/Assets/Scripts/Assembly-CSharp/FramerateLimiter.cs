using System.Collections;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.XR;

public class FramerateLimiter : MonoBehaviour
{
	private bool limitFrameRate;

	public int fpsLimit = 144;

	public bool matchVRDevice = true;

	public bool logMaxFrameRate = true;

	private int maxFrameRate;

	private Stopwatch sw = new Stopwatch();

	private bool started;

	private static FramerateLimiter instance;

	private void Awake()
	{
		if ((bool)instance)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		instance = this;
		Object.DontDestroyOnLoad(base.gameObject);
		if (matchVRDevice)
		{
			StartCoroutine(MatchVRDeviceRoutine());
		}
		GameSettings.OnAppliedSettings += GameSettings_OnAppliedSettings;
		GameSettings.EnsureSettings();
		GameSettings_OnAppliedSettings(GameSettings.CurrentSettings);
	}

	private void GameSettings_OnAppliedSettings(GameSettings s)
	{
		bool flag = false;
		if (flag != limitFrameRate)
		{
			limitFrameRate = flag;
			UnityEngine.Debug.Log("Setting FrameRateLimiter: " + limitFrameRate);
			maxFrameRate = 0;
			if (limitFrameRate)
			{
				StartCoroutine(MatchVRDeviceRoutine());
			}
		}
	}

	private IEnumerator MatchVRDeviceRoutine()
	{
		while (!InputDevices.GetDeviceAtXRNode(XRNode.Head).isValid)
		{
			yield return null;
		}
		UnityEngine.Debug.Log("FrameRateLimiter matching limit to VR device refresh rate: " + XRDevice.refreshRate);
		fpsLimit = Mathf.FloorToInt(XRDevice.refreshRate);
	}

	private void OnEnable()
	{
		StartCoroutine(LimiterRoutine());
	}

	private IEnumerator LimiterRoutine()
	{
		yield return null;
		WaitForEndOfFrame wait = new WaitForEndOfFrame();
		while (base.enabled)
		{
			if (started)
			{
				bool flag = VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.Editor || VTMapManager.nextLaunchMode == VTMapManager.MapLaunchModes.MapEditor;
				if (limitFrameRate || flag)
				{
					float num = (flag ? Screen.currentResolution.refreshRate : fpsLimit);
					double num2 = 1.0 / (double)num;
					if (sw.Elapsed.TotalSeconds < num2)
					{
						Thread.Sleep(Mathf.RoundToInt((float)((num2 - sw.Elapsed.TotalSeconds) * 1000.0)));
					}
				}
				sw.Stop();
				int num3 = (int)(1.0 / sw.Elapsed.TotalSeconds);
				if (logMaxFrameRate && num3 > maxFrameRate)
				{
					maxFrameRate = num3;
					UnityEngine.Debug.Log("New max framerate: " + maxFrameRate);
				}
				sw.Reset();
			}
			else
			{
				started = true;
			}
			sw.Start();
			yield return wait;
		}
	}
}
