using System.Collections;
using UnityEngine;

public class MultiUCAVLauncher : MonoBehaviour, IQSVehicleComponent
{
	public UCAVLauncher[] launchers;

	public float interval;

	public Actor actor;

	private bool launched;

	private void Awake()
	{
		if (!VTScenarioEditor.isLoadingPreviewThumbnails)
		{
			if (!actor)
			{
				actor = GetComponentInParent<Actor>();
			}
			string text = actor.actorName;
			if ((bool)actor.unitSpawn)
			{
				text = actor.unitSpawn.unitSpawner.GetUIDisplayName();
			}
			text += " ";
			for (int i = 0; i < launchers.Length; i++)
			{
				UCAVLauncher uCAVLauncher = launchers[i];
				uCAVLauncher.aiPilot.gameObject.name = text + uCAVLauncher.aiPilot.gameObject.name + i;
			}
			actor.GetComponent<Health>().OnDeath.AddListener(OnDeath);
		}
	}

	private void OnDeath()
	{
		UCAVLauncher[] array = launchers;
		foreach (UCAVLauncher uCAVLauncher in array)
		{
			if ((bool)uCAVLauncher.aiPilot && uCAVLauncher.aiPilot.actor.alive && !uCAVLauncher.launched)
			{
				uCAVLauncher.aiPilot.actor.health.KillDelayed(Random.Range(0.25f, 4f));
			}
		}
	}

	public void LaunchAll()
	{
		if (!launched && base.enabled)
		{
			launched = true;
			StartCoroutine(LaunchRoutine());
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		qsNode.AddNode("MultiUCAVLauncher_" + base.gameObject.name).SetValue("launched", launched);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("MultiUCAVLauncher_" + base.gameObject.name);
		if (node != null && node.GetValue<bool>("launched"))
		{
			LaunchAll();
		}
	}

	private IEnumerator LaunchRoutine()
	{
		for (int i = 0; i < launchers.Length; i++)
		{
			launchers[i].LaunchUCAV();
			yield return new WaitForSeconds(interval);
		}
	}
}
