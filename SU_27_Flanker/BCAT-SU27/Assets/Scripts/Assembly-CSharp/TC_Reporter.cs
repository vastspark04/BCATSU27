using System.Diagnostics;
using UnityEngine;

[ExecuteInEditMode]
public class TC_Reporter : MonoBehaviour
{
	public static TC_Reporter instance;

	private static bool hasReported;

	private int frame;

	public bool report;

	public bool[] channels;

	public float[] timeStart;

	public TC_Reporter()
	{
		instance = this;
	}

	private void OnEnable()
	{
		instance = this;
	}

	private void OnDisable()
	{
		instance = null;
	}

	private void LateUpdate()
	{
		if (hasReported)
		{
			UnityEngine.Debug.Log("----------------------------------------------------------> " + frame + "\n");
			frame++;
			hasReported = false;
		}
	}

	public static string GetInclination()
	{
		StackTrace stackTrace = new StackTrace();
		string text = string.Empty;
		for (int i = 0; i < stackTrace.FrameCount - 2; i++)
		{
			text += "   ";
		}
		return text;
	}

	public static void Log(string text, int channelIndex = 0)
	{
		if (!(instance == null) && instance.report)
		{
			if (instance.channels == null)
			{
				instance.channels = new bool[5];
			}
			if (instance.channels.Length != 5)
			{
				instance.channels = new bool[5];
			}
			if (instance.channels[channelIndex] || channelIndex == -1)
			{
				UnityEngine.Debug.Log(GetInclination() + text + "\n");
				hasReported = true;
			}
		}
	}

	public static void BenchmarkStart(int channel = 0)
	{
		if (instance.timeStart == null)
		{
			instance.timeStart = new float[5];
		}
		if (instance.timeStart.Length != 5)
		{
			instance.timeStart = new float[5];
		}
		instance.timeStart[channel] = Time.realtimeSinceStartup;
	}

	public static string BenchmarkStop(string text = "", bool logToConsole = true, int channel = 0)
	{
		float num = Time.realtimeSinceStartup - instance.timeStart[channel];
		if (logToConsole)
		{
			text = text + " time " + num + " frame " + 1f / num;
			UnityEngine.Debug.Log(text);
		}
		else
		{
			text += (1f / num).ToString("F0");
		}
		return text;
	}
}
