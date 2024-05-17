using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class VTSOTutorial : VTStaticObject
{
	[Serializable]
	public class CustomAction
	{
		public UnityEvent eventActions;
	}

	public VRTutorial tutorial;

	public CustomAction[] customActions;

	private void Awake()
	{
		tutorial.enabled = false;
	}

	private IEnumerator Startup()
	{
		yield return new WaitForSeconds(3f);
		tutorial.enabled = true;
		yield return null;
		tutorial.StartTutorial();
	}

	protected override void OnSpawned()
	{
		base.OnSpawned();
		StartCoroutine(Startup());
	}

	[VTEvent("Fire Custom Action", "Fires custom action set up in VTSO", new string[] { "Action Index" })]
	public void FireCustomAction([VTRangeParam(0f, 100f)][VTRangeTypeParam(UnitSpawnAttributeRange.RangeTypes.Int)] float idx)
	{
		int num = Mathf.RoundToInt(idx);
		customActions[num].eventActions.Invoke();
	}
}
