using UnityEngine;
using UnityEngine.Events;

public class EventTester : MonoBehaviour
{
	public UnityEvent events;

	[ContextMenu("Fire Events")]
	public void FireEvents()
	{
		if (Application.isPlaying)
		{
			events.Invoke();
		}
	}
}
