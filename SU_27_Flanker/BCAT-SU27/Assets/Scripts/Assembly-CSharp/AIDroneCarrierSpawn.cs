using UnityEngine;

public class AIDroneCarrierSpawn : AISeaUnitSpawn
{
	public MultiUCAVLauncher droneLauncher;

	[VTEvent("Launch Drones", "Launch all drones from the carrier.")]
	public void LaunchDrones()
	{
		droneLauncher.LaunchAll();
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		base.Quicksave(qsNode);
		for (int i = 0; i < droneLauncher.launchers.Length; i++)
		{
			UCAVLauncher uCAVLauncher = droneLauncher.launchers[i];
			ConfigNode configNode = qsNode.AddNode("DroneLauncher");
			configNode.SetValue("idx", i);
			configNode.SetValue("launched", uCAVLauncher.launched);
			if (!uCAVLauncher.launched)
			{
				continue;
			}
			configNode.SetValue("finishedLaunchSequence", uCAVLauncher.finishedLaunchSequence);
			bool flag = (bool)uCAVLauncher.aiPilot && uCAVLauncher.aiPilot.actor.alive;
			configNode.SetValue("aircraftAlive", flag);
			if (flag)
			{
				ConfigNode configNode2 = configNode.AddNode("Aircraft");
				IQSVehicleComponent[] componentsInChildrenImplementing = uCAVLauncher.aiPilot.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>(includeInactive: true);
				for (int j = 0; j < componentsInChildrenImplementing.Length; j++)
				{
					componentsInChildrenImplementing[j].OnQuicksave(configNode2);
				}
				configNode2.SetValue("globalPos", VTMapManager.WorldToGlobalPoint(uCAVLauncher.aiPilot.transform.position));
				configNode2.SetValue("rotation", uCAVLauncher.aiPilot.transform.rotation.eulerAngles);
			}
		}
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		foreach (ConfigNode node2 in qsNode.GetNodes("DroneLauncher"))
		{
			int value = node2.GetValue<int>("idx");
			if (!node2.GetValue<bool>("launched"))
			{
				continue;
			}
			UCAVLauncher uCAVLauncher = droneLauncher.launchers[value];
			uCAVLauncher.launched = true;
			if (uCAVLauncher.finishedLaunchSequence = node2.GetValue<bool>("finishedLaunchSequence"))
			{
				if (node2.GetValue<bool>("aircraftAlive"))
				{
					uCAVLauncher.QL_StopLaunchRoutines();
					uCAVLauncher.planeRb.transform.parent = null;
					FloatingOriginTransform floatingOriginTransform = uCAVLauncher.planeRb.GetComponent<FloatingOriginTransform>();
					if (!floatingOriginTransform)
					{
						floatingOriginTransform = uCAVLauncher.planeRb.gameObject.AddComponent<FloatingOriginTransform>();
					}
					floatingOriginTransform.SetRigidbody(uCAVLauncher.planeRb);
					uCAVLauncher.planeRb.interpolation = RigidbodyInterpolation.Interpolate;
					if (!uCAVLauncher.engine.engineEnabled)
					{
						uCAVLauncher.engine.ToggleEngine();
					}
					uCAVLauncher.aiPilot.enabled = true;
					uCAVLauncher.aiPilot.kPlane.enabled = true;
					uCAVLauncher.wingRotator.SetDefault();
					uCAVLauncher.aiPilot.actor.role = Actor.Roles.Air;
					uCAVLauncher.aiPilot.actor.parentActor = null;
					Object.Destroy(uCAVLauncher.booster.gameObject);
					ConfigNode node = node2.GetNode("Aircraft");
					uCAVLauncher.aiPilot.transform.position = VTMapManager.GlobalToWorldPoint(node.GetValue<Vector3D>("globalPos"));
					uCAVLauncher.aiPilot.transform.rotation = Quaternion.Euler(node.GetValue<Vector3>("rotation"));
					uCAVLauncher.planeRb.position = uCAVLauncher.aiPilot.transform.position;
					uCAVLauncher.planeRb.rotation = uCAVLauncher.aiPilot.transform.rotation;
					IQSVehicleComponent[] componentsInChildrenImplementing = uCAVLauncher.aiPilot.gameObject.GetComponentsInChildrenImplementing<IQSVehicleComponent>(includeInactive: true);
					for (int i = 0; i < componentsInChildrenImplementing.Length; i++)
					{
						componentsInChildrenImplementing[i].OnQuickload(node);
					}
				}
				else
				{
					Object.Destroy(droneLauncher.launchers[value].aiPilot.gameObject);
				}
			}
			else
			{
				droneLauncher.launchers[value].LaunchUCAV();
			}
		}
	}
}
