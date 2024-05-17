using System;
using UnityEngine;
using UnityEngine.Events;

public class DisableRenderOnFlyby : MonoBehaviour
{
	private Renderer[] renderers;

	private bool[] initialValue;

	private void Start()
	{
		renderers = GetComponentsInChildren<Renderer>();
		initialValue = new bool[renderers.Length];
		ShipController componentInParent = GetComponentInParent<ShipController>();
		componentInParent.OnFlybyCameraEnter = (UnityAction)Delegate.Combine(componentInParent.OnFlybyCameraEnter, new UnityAction(OnEnterFlyby));
		componentInParent.OnFlybyCameraExit = (UnityAction)Delegate.Combine(componentInParent.OnFlybyCameraExit, new UnityAction(OnExitFlyby));
	}

	private void OnEnterFlyby()
	{
		base.gameObject.SetActive(value: false);
		for (int i = 0; i < renderers.Length; i++)
		{
			initialValue[i] = renderers[i].enabled;
			renderers[i].enabled = false;
		}
	}

	private void OnExitFlyby()
	{
		base.gameObject.SetActive(value: true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = initialValue[i];
		}
	}
}
