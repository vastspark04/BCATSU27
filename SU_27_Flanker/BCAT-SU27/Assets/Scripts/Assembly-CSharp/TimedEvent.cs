using System;
using UnityEngine.Events;

[Serializable]
public class TimedEvent
{
	public string name;

	public float time;

	public UnityEvent OnFireEvent;
}
