using System.Collections.Generic;
using UnityEngine;

public class DebugCamToggler : MonoBehaviour
{
	private bool debugEnabled;

	public CameraFollowMe debugCam;

	private FloatingOriginShifter playerfos;

	private void Update()
	{
		if (!Input.GetKeyDown(KeyCode.Insert))
		{
			return;
		}
		if (debugEnabled)
		{
			debugEnabled = false;
			debugCam.gameObject.SetActive(value: false);
			if ((bool)playerfos)
			{
				playerfos.enabled = true;
			}
			return;
		}
		debugEnabled = true;
		if ((bool)FlightSceneManager.instance.playerActor)
		{
			Transform transform = FlightSceneManager.instance.playerActor.transform;
			playerfos = transform.GetComponent<FloatingOriginShifter>();
			if ((bool)playerfos)
			{
				playerfos.enabled = false;
			}
		}
		debugCam.targets = new List<Actor>();
		foreach (Actor allActor in TargetManager.instance.allActors)
		{
			if ((bool)allActor && allActor.alive && !allActor.parentActor)
			{
				debugCam.targets.Add(allActor);
			}
		}
		debugCam.gameObject.SetActive(value: true);
	}
}
