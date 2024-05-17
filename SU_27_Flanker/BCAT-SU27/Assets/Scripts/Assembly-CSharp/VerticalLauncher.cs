using System;
using UnityEngine;
using UnityEngine.Events;

public class VerticalLauncher : MonoBehaviour
{
	[Serializable]
	public class VLSTube
	{
		public RotationToggle tubeCap;

		public Transform fireTransform;
	}

	public VLSTube[] tubeCaps;

	public RotationToggle exhaustCap;

	public UnityEvent OnFire;

	public void FireMissile()
	{
		if (OnFire != null)
		{
			OnFire.Invoke();
		}
	}

	public void DisableLauncher()
	{
		VLSTube[] array = tubeCaps;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].tubeCap.SetDefault();
		}
		if ((bool)exhaustCap)
		{
			exhaustCap.SetDefault();
		}
	}

	public bool EnableLauncher(Transform fireTransform)
	{
		VLSTube[] array = tubeCaps;
		foreach (VLSTube vLSTube in array)
		{
			if (vLSTube.fireTransform == fireTransform)
			{
				vLSTube.tubeCap.SetDeployed();
				if ((bool)exhaustCap)
				{
					exhaustCap.SetDeployed();
				}
				return true;
			}
		}
		return false;
	}
}
