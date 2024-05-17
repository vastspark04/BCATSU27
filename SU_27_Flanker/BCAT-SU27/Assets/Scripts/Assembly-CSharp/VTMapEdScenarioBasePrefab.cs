using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VTMapEdScenarioBasePrefab : VTMapEdStructurePrefab, IHasAirport
{
	public string baseName;

	public Teams team;

	public AirportManager airportManager;

	public List<ReArmingPoint> rearmPoints = new List<ReArmingPoint>();

	public event UnityAction<Teams> OnSetTeam;

	[ContextMenu("Get Rearm Points")]
	public void Editor_GetRearmPoints()
	{
		rearmPoints = new List<ReArmingPoint>();
		ReArmingPoint[] componentsInChildren = GetComponentsInChildren<ReArmingPoint>();
		foreach (ReArmingPoint item in componentsInChildren)
		{
			rearmPoints.Add(item);
		}
	}

	public override void OnPlacedInMap(VTMapEdPlacementInfo info)
	{
		base.OnPlacedInMap(info);
		if ((bool)airportManager && airportManager.runways != null)
		{
			Runway[] runways = airportManager.runways;
			foreach (Runway runwayQueueAltitude in runways)
			{
				SetRunwayQueueAltitude(runwayQueueAltitude);
			}
		}
		SetTeam(team);
	}

	[VTEvent("Set Team", "Set the team that this base belongs to.", new string[] { "Team" })]
	public void SetTeam(Teams team)
	{
		this.team = team;
		foreach (ReArmingPoint rearmPoint in rearmPoints)
		{
			if ((bool)rearmPoint)
			{
				rearmPoint.team = team;
			}
		}
		if ((bool)airportManager)
		{
			airportManager.team = team;
		}
		if (this.OnSetTeam != null)
		{
			this.OnSetTeam(team);
		}
	}

	public override string GetDisplayName()
	{
		string text = baseName;
		if (string.IsNullOrEmpty(text))
		{
			text = "unnamed";
		}
		return $"[{id}] {text} ({prefabName})";
	}

	protected override void OnSavedToNode(ConfigNode node)
	{
		base.OnSavedToNode(node);
		node.SetValue("baseName", baseName);
	}

	protected override void OnLoadedFromNode(ConfigNode node)
	{
		base.OnLoadedFromNode(node);
		ConfigNodeUtils.TryParseValue(node, "baseName", ref baseName);
		string text = baseName;
		if (string.IsNullOrEmpty(text))
		{
			text = "Airbase";
		}
		airportManager.airportName = text;
		if ((bool)airportManager.location)
		{
			airportManager.location.locationName = text;
		}
	}

	public void BeginScenario()
	{
		OnBeginScenario();
	}

	protected virtual void OnBeginScenario()
	{
		if ((bool)QuicksaveManager.instance)
		{
			QuicksaveManager.instance.OnQuicksave -= QSManager_OnQuicksave;
			QuicksaveManager.instance.OnQuickload -= QSManager_OnQuickload;
			QuicksaveManager.instance.OnQuicksave += QSManager_OnQuicksave;
			QuicksaveManager.instance.OnQuickload += QSManager_OnQuickload;
		}
	}

	private void QSManager_OnQuickload(ConfigNode configNode)
	{
		try
		{
			string text = "mapBase_" + id;
			if (configNode.HasNode(text))
			{
				ConfigNode node = configNode.GetNode(text);
				OnQuickload(node);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("Scenario base " + id + " had an error quickloading!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	private void QSManager_OnQuicksave(ConfigNode configNode)
	{
		try
		{
			ConfigNode configNode2 = new ConfigNode("mapBase_" + id);
			configNode.AddNode(configNode2);
			OnQuicksave(configNode2);
		}
		catch (Exception ex)
		{
			Debug.LogError("Scenario base " + id + " had an error quicksaving!\n" + ex);
			QuicksaveManager.instance.IndicateError();
		}
	}

	protected virtual void OnQuickload(ConfigNode baseNode)
	{
	}

	protected virtual void OnQuicksave(ConfigNode baseNode)
	{
	}

	private void SetRunwayQueueAltitude(Runway r)
	{
		VTMapGenerator.fetch.BakeCollidersAtPositionRadius(r.landingQueueOrbitTf.position, r.landingQueueRadius);
		Vector3 position = r.landingQueueOrbitTf.position;
		position.y = r.transform.position.y + 10000f;
		if (Physics.BoxCast(position, r.landingQueueRadius * Vector3.one, Vector3.down, out var hitInfo, Quaternion.identity, 10000f, 1))
		{
			float num = Mathf.Max(0f, hitInfo.point.y - r.transform.position.y);
			r.landingQueueAltitude = Mathf.Max(r.landingQueueAltitude, num + 500f);
		}
	}

	public AirportManager GetAirport()
	{
		return airportManager;
	}
}
