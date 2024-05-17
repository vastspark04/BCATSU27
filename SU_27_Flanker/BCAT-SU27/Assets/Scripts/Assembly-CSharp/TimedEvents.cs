using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedEvents : MonoBehaviour, IQSMissileComponent
{
	public List<TimedEvent> events;

	public bool startImmediately = true;

	public float initialDelay;

	private bool started;

	public int scenarioTimedEventId = -1;

	private Coroutine eventsRoutine;

	private float lastWaitStartTime;

	private int waitIdx;

	private bool finished;

	private float timeSinceLastWait;

	private void Start()
	{
		if (!GetComponentInParent<Missile>())
		{
			QuicksaveManager.instance.OnQuicksave += OnQuicksave;
			QuicksaveManager.instance.OnQuickload += OnQuickload;
		}
		if (events != null && events.Count != 0 && startImmediately)
		{
			StartTimedEvents();
		}
	}

	private void OnDestroy()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuicksave -= OnQuicksave;
			QuicksaveManager.instance.OnQuickload -= OnQuickload;
		}
	}

	public void StartTimedEvents()
	{
		if (!started)
		{
			started = true;
			startImmediately = false;
			eventsRoutine = StartCoroutine(EventRoutine());
		}
	}

	public void StopTimedEvents()
	{
		if (started && eventsRoutine != null)
		{
			StopCoroutine(eventsRoutine);
			finished = true;
		}
	}

	private IEnumerator EventRoutine()
	{
		events.Sort(CompareTime);
		List<WaitForSeconds> waits = new List<WaitForSeconds>();
		float num = 0f;
		foreach (TimedEvent @event in events)
		{
			float num2 = @event.time - num;
			waits.Add(new WaitForSeconds(num2));
			num += num2;
		}
		if ((bool)VTMapManager.fetch)
		{
			while (!FlightSceneManager.isFlightReady)
			{
				yield return null;
			}
			if (VTScenario.current != null && !VTScenario.current.multiplayer)
			{
				while (!PlayerSpawn.playerVehicleReady)
				{
					yield return null;
				}
			}
		}
		waitIdx = -1;
		lastWaitStartTime = Time.time;
		yield return new WaitForSeconds(initialDelay);
		for (int idx = 0; idx < events.Count; idx++)
		{
			lastWaitStartTime = Time.time;
			waitIdx = idx;
			yield return waits[idx];
			Debug.Log($"Firing timed event: {events[idx].name} - Time: {Time.time}");
			events[idx].OnFireEvent.Invoke();
			if (scenarioTimedEventId >= 0 && VTScenario.isScenarioHost)
			{
				VTScenario.current.timedEventGroups.ReportEventFired(scenarioTimedEventId, idx);
			}
		}
		finished = true;
	}

	private IEnumerator EventRoutineQuickload()
	{
		if (waitIdx == -1)
		{
			initialDelay -= timeSinceLastWait;
			eventsRoutine = StartCoroutine(EventRoutine());
			yield break;
		}
		events.Sort(CompareTime);
		List<WaitForSeconds> waits = new List<WaitForSeconds>();
		events[waitIdx].time -= timeSinceLastWait;
		float num = 0f;
		foreach (TimedEvent @event in events)
		{
			float num2 = @event.time - num;
			waits.Add(new WaitForSeconds(num2));
			num += num2;
		}
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		for (int idx = waitIdx; idx < events.Count; idx++)
		{
			if (idx == waitIdx)
			{
				lastWaitStartTime = Time.time - timeSinceLastWait;
				Debug.LogFormat("Resuming timed event group {0} at idx {1} before event {2}.", base.gameObject.name, idx, events[idx].name);
			}
			else
			{
				lastWaitStartTime = Time.time;
			}
			waitIdx = idx;
			yield return waits[idx];
			Debug.Log($"Firing timed event (quickload): {events[idx].name} - Time: {Time.time}");
			events[idx].OnFireEvent.Invoke();
		}
		finished = true;
	}

	private static int CompareTime(TimedEvent a, TimedEvent b)
	{
		return a.time.CompareTo(b.time);
	}

	private void OnQuicksave(ConfigNode qsNode)
	{
		try
		{
			ConfigNode configNode = new ConfigNode(base.gameObject.name + "_TimedEvents");
			qsNode.AddNode(configNode);
			configNode.SetValue("started", started);
			configNode.SetValue("timeSinceLastWait", Time.time - lastWaitStartTime);
			configNode.SetValue("waitIdx", waitIdx);
			configNode.SetValue("finished", finished);
		}
		catch (Exception ex)
		{
			Debug.LogError("TimedEvent had an error quicksaving!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private void OnQuickload(ConfigNode qsNode)
	{
		try
		{
			string text = base.gameObject.name + "_TimedEvents";
			if (qsNode.HasNode(text))
			{
				ConfigNode node = qsNode.GetNode(text);
				started = ConfigNodeUtils.ParseBool(node.GetValue("started"));
				finished = ConfigNodeUtils.ParseBool(node.GetValue("finished"));
				if (eventsRoutine != null)
				{
					StopCoroutine(eventsRoutine);
				}
				if (started && !finished)
				{
					timeSinceLastWait = ConfigNodeUtils.ParseFloat(node.GetValue("timeSinceLastWait"));
					waitIdx = ConfigNodeUtils.ParseInt(node.GetValue("waitIdx"));
					eventsRoutine = StartCoroutine(EventRoutineQuickload());
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("TimedEvent had an error quickloading!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	public void OnQuicksavedMissile(ConfigNode qsNode, float elapsedTime)
	{
		OnQuicksave(qsNode);
	}

	public void OnQuickloadedMissile(ConfigNode qsNode, float elapsedTime)
	{
		OnQuickload(qsNode);
	}
}
