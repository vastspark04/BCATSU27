using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrobeLightController : ElectronicComponent
{
	[Serializable]
	public class StrobeLight
	{
		public GameObject lightObject;

		public float startOffset;

		public float strobeInterval;

		public int pulses;

		public float pulseInterval;
	}

	public List<StrobeLight> lights;

	public bool onByDefault;

	public float strobeDrain;

	private bool strobing;

	private List<Coroutine> strobeRoutines = new List<Coroutine>();

	public ObjectPowerUnit[] indicatorObjs;

	public int state { get; private set; }

	public event Action<int> OnSetPower;

	private void OnEnable()
	{
		foreach (StrobeLight light in lights)
		{
			light.lightObject.SetActive(value: false);
		}
		if (onByDefault)
		{
			StartStrobe();
		}
	}

	private void OnDisable()
	{
		EndStrobe();
	}

	public void SetStrobePower(int p)
	{
		if (p > 0)
		{
			StartStrobe();
			onByDefault = true;
		}
		else
		{
			EndStrobe();
			onByDefault = false;
		}
		state = p;
		this.OnSetPower?.Invoke(p);
		ObjectPowerUnit[] array = indicatorObjs;
		foreach (ObjectPowerUnit objectPowerUnit in array)
		{
			if ((bool)objectPowerUnit)
			{
				objectPowerUnit.SetConnection(p);
			}
		}
	}

	private void StartStrobe()
	{
		if (strobing)
		{
			return;
		}
		foreach (StrobeLight light in lights)
		{
			strobeRoutines.Add(StartCoroutine(StrobeRoutine(light)));
		}
		strobing = true;
	}

	private void EndStrobe()
	{
		if (!strobing)
		{
			return;
		}
		if (strobeRoutines != null)
		{
			foreach (Coroutine strobeRoutine in strobeRoutines)
			{
				if (strobeRoutine != null)
				{
					StopCoroutine(strobeRoutine);
				}
			}
			strobeRoutines.Clear();
		}
		foreach (StrobeLight light in lights)
		{
			if (light != null && (bool)light.lightObject)
			{
				light.lightObject.SetActive(value: false);
			}
		}
		strobing = false;
	}

	private IEnumerator StrobeRoutine(StrobeLight light)
	{
		yield return new WaitForSeconds(light.startOffset);
		while (true)
		{
			if (!battery || battery.Drain(strobeDrain * 2f * (float)light.pulses * Time.deltaTime))
			{
				for (int i = 0; i < light.pulses; i++)
				{
					light.lightObject.SetActive(value: true);
					yield return null;
					yield return null;
					light.lightObject.SetActive(value: false);
					yield return new WaitForSeconds(light.pulseInterval);
				}
			}
			else
			{
				foreach (StrobeLight light2 in lights)
				{
					if (light2 != null && (bool)light2.lightObject)
					{
						light2.lightObject.SetActive(value: false);
					}
				}
			}
			yield return new WaitForSeconds(light.strobeInterval);
			yield return null;
		}
	}
}
