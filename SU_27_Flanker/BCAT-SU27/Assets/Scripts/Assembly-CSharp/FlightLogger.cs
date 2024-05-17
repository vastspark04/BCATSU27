using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class FlightLogger : MonoBehaviour
{
	public delegate void LogEvent(LogEntry message);

	public struct LogEntry
	{
		public float timestamp;

		public string message;

		public string timestampedMessage => $"[{UIUtils.FormattedTime(timestamp)}] {message}";
	}

	private List<LogEntry> log = new List<LogEntry>();

	private float startTime;

	public static FlightLogger fetch { get; private set; }

	public event LogEvent OnLogEntry;

	public event Action OnLogCleared;

	public static void Log(string message)
	{
		EnsureInstance();
		fetch._Log(message);
	}

	public static void Relog(LogEntry e)
	{
		EnsureInstance();
		fetch.log.Add(e);
		if (fetch.OnLogEntry != null)
		{
			fetch.OnLogEntry(e);
		}
	}

	public static List<LogEntry> GetLog()
	{
		EnsureInstance();
		return fetch.log;
	}

	public static void AddLogEntryListener(LogEvent e)
	{
		EnsureInstance();
		fetch.OnLogEntry += e;
	}

	public static void RemoveLogEntryListener(LogEvent e)
	{
		if ((bool)fetch)
		{
			fetch.OnLogEntry -= e;
		}
	}

	public static void ClearLog()
	{
		if ((bool)fetch)
		{
			fetch._ClearLog();
		}
	}

	private void _ClearLog()
	{
		log.Clear();
		if (this.OnLogCleared != null)
		{
			this.OnLogCleared();
		}
	}

	public static void DumpLog()
	{
		if (!fetch)
		{
			Debug.LogError("FlightLogger: Attempted to dump flight log but none exists!");
		}
		try
		{
			Log("Flight log dumped to file.");
			StringBuilder stringBuilder = new StringBuilder();
			foreach (LogEntry item in fetch.log)
			{
				stringBuilder.AppendLine(item.timestampedMessage);
			}
			string text = Application.dataPath + "/FlightLogs/";
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text2 = text + DateTime.Now.ToString().Replace('/', '-').Replace(':', '-') + ".txt";
			File.Create(text2).Dispose();
			File.WriteAllText(text2, stringBuilder.ToString());
			Debug.LogFormat("Dumped flight log to: {0}", text2);
		}
		catch (Exception ex)
		{
			Debug.LogErrorFormat("FlightLogger: failed to dump log: {0}", ex.ToString());
		}
	}

	private void Awake()
	{
		startTime = Time.time;
	}

	private static void EnsureInstance()
	{
		if (!fetch)
		{
			fetch = new GameObject("FlightLogger").AddComponent<FlightLogger>();
		}
	}

	private void _Log(string message)
	{
		Debug.LogFormat("FlightLogger: {0}", message);
		LogEntry logEntry = default(LogEntry);
		logEntry.timestamp = FlightSceneManager.instance.missionElapsedTime;
		logEntry.message = message;
		LogEntry logEntry2 = logEntry;
		log.Add(logEntry2);
		if (this.OnLogEntry != null)
		{
			this.OnLogEntry(logEntry2);
		}
	}
}
