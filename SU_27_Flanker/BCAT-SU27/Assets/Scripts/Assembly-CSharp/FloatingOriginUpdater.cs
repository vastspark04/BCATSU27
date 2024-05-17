using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class FloatingOriginUpdater : MonoBehaviour
{
	public bool awaitingShift;

	public Vector3 awaitingOffset;

	public FloatingOrigin fo;

	private Queue<UnityAction> updateQueue = new Queue<UnityAction>();

	private bool queueWaiting;

	private void Start()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += Instance_OnExitScene;
		}
	}

	private void Instance_OnExitScene()
	{
		queueWaiting = false;
		updateQueue.Clear();
	}

	private void FixedUpdate()
	{
		if (queueWaiting)
		{
			while (updateQueue.Count > 0)
			{
				updateQueue.Dequeue()();
			}
			queueWaiting = false;
		}
		if (awaitingShift)
		{
			awaitingShift = false;
			fo.ShiftOrigin(awaitingOffset, immediate: true);
		}
	}

	public void AddLateFixedUpdateAction(UnityAction a)
	{
		updateQueue.Enqueue(a);
		queueWaiting = true;
	}
}
