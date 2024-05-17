using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerEnvironmentObject : MonoBehaviour
{
	public List<string> excludeFromEnvs;

	public List<string> enableOnEnvs;

	private IEnumerator Start()
	{
		while (!EnvironmentManager.instance)
		{
			yield return null;
		}
		EnvironmentManager.instance.OnEnvironmentChanged += OnEnvironmentChanged;
		OnEnvironmentChanged(EnvironmentManager.instance.GetCurrentEnvironment());
	}

	private void OnEnvironmentChanged(EnvironmentManager.EnvironmentSetting obj)
	{
		bool active = true;
		foreach (string excludeFromEnv in excludeFromEnvs)
		{
			if (obj.name == excludeFromEnv)
			{
				active = false;
				break;
			}
		}
		foreach (string enableOnEnv in enableOnEnvs)
		{
			if (obj.name == enableOnEnv)
			{
				active = true;
				break;
			}
		}
		base.gameObject.SetActive(active);
	}

	private void OnDestroy()
	{
		if ((bool)EnvironmentManager.instance)
		{
			EnvironmentManager.instance.OnEnvironmentChanged -= OnEnvironmentChanged;
		}
	}
}
