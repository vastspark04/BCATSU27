using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class VTSTriggerEventBehaviour : MonoBehaviour
{
	public ScenarioTriggerEvents.TriggerEvent tEvent;

	private float sqrRad;

	private UnitSpawner unitSpawner;

	private bool initialized;

	private bool triggered;

	private Coroutine tRoutine;

	private event UnityAction finalEvent;

	public bool WasTriggered()
	{
		return triggered;
	}

	public void Initialize(ScenarioTriggerEvents.TriggerEvent evt)
	{
		tEvent = evt;
		tEvent.behaviour = this;
		sqrRad = tEvent.radius * tEvent.radius;
		foreach (VTEventTarget action in tEvent.eventInfo.actions)
		{
			if (action != null)
			{
				finalEvent += action.Invoke;
			}
		}
		if (tEvent.enabled && base.gameObject.activeInHierarchy)
		{
			tRoutine = StartCoroutine(TriggerRoutine());
		}
		initialized = true;
	}

	private void OnEnable()
	{
		if (tEvent != null && tEvent.enabled && initialized && !triggered)
		{
			if (tRoutine != null)
			{
				StopCoroutine(tRoutine);
			}
			tRoutine = StartCoroutine(TriggerRoutine());
		}
	}

	public void Enable()
	{
		tEvent.enabled = true;
		if (tRoutine == null && initialized)
		{
			tRoutine = StartCoroutine(TriggerRoutine());
		}
	}

	public void Disable()
	{
		tEvent.enabled = false;
		if (tRoutine != null)
		{
			StopCoroutine(tRoutine);
			tRoutine = null;
		}
	}

	public void PermaDisable()
	{
		tEvent.enabled = false;
		if (tRoutine != null)
		{
			StopCoroutine(tRoutine);
			tRoutine = null;
		}
		triggered = true;
	}

	public void Trigger(bool remoteTriggered = false)
	{
		if ((VTScenario.isScenarioHost || remoteTriggered) && !triggered)
		{
			Debug.Log("Firing trigger event: " + tEvent.eventName);
			if (this.finalEvent != null)
			{
				this.finalEvent();
			}
			triggered = true;
			if (VTScenario.isScenarioHost)
			{
				VTScenario.current.triggerEvents.ReportEventFired(tEvent.id);
			}
		}
	}

	private IEnumerator TriggerRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		while (!VTMapManager.fetch.scenarioReady)
		{
			yield return null;
		}
		for (int k = 0; k < 5; k++)
		{
			yield return null;
		}
		if (!VTOLMPUtils.IsMultiplayer())
		{
			while (!PlayerSpawn.playerVehicleReady)
			{
				yield return null;
			}
		}
		else if (!VTScenario.isScenarioHost)
		{
			yield break;
		}
		List<Actor> unitCache = null;
		if (tEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Proximity)
		{
			if (tEvent.triggerMode == TriggerEventModes.Unit)
			{
				unitSpawner = tEvent.unit.GetSpawner();
				if (unitSpawner == null)
				{
					Debug.Log("Trigger event '" + tEvent.eventName + "' is supposed to be trigged by a unit but no unit was set.");
					yield break;
				}
			}
			unitCache = new List<Actor>();
		}
		yield return null;
		bool fired = false;
		while (!fired)
		{
			fired = false;
			if (tEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Proximity)
			{
				switch (tEvent.triggerMode)
				{
				case TriggerEventModes.Player:
					fired = DoesPlayerTrigger();
					break;
				case TriggerEventModes.Unit:
					if (unitSpawner.spawned)
					{
						fired = DoesTransformTrigger(unitSpawner.spawnedUnit.actor ? unitSpawner.spawnedUnit.actor.transform : unitSpawner.spawnedUnit.transform);
					}
					break;
				default:
				{
					if (tEvent.proxyMode == TriggerProximityModes.OnExit)
					{
						List<Actor> list = ((tEvent.triggerMode == TriggerEventModes.AnyUnit) ? TargetManager.instance.allActors : ((tEvent.triggerMode != TriggerEventModes.AnyAllied) ? TargetManager.instance.enemyUnits : TargetManager.instance.alliedUnits));
						unitCache.Clear();
						for (int l = 0; l < list.Count; l++)
						{
							if (fired)
							{
								break;
							}
							Actor actor = list[l];
							if ((bool)actor && actor.alive)
							{
								unitCache.Add(actor);
							}
						}
					}
					else if (tEvent.triggerMode == TriggerEventModes.AnyUnit)
					{
						Actor.GetActorsInRadius(tEvent.waypoint.worldPosition, tEvent.radius, Teams.Allied, TeamOptions.BothTeams, unitCache, clearList: true, tEvent.sphericalRadius);
					}
					else if (tEvent.triggerMode == TriggerEventModes.AnyAllied)
					{
						Actor.GetActorsInRadius(tEvent.waypoint.worldPosition, tEvent.radius, Teams.Allied, TeamOptions.SameTeam, unitCache, clearList: true, tEvent.sphericalRadius);
					}
					else
					{
						Actor.GetActorsInRadius(tEvent.waypoint.worldPosition, tEvent.radius, Teams.Enemy, TeamOptions.SameTeam, unitCache, clearList: true, tEvent.sphericalRadius);
					}
					int k = unitCache.Count;
					for (int i = 0; i < k; i++)
					{
						if (fired)
						{
							break;
						}
						Actor actor2 = unitCache[i];
						if (actor2 != null && actor2.alive)
						{
							fired = DoesTransformTrigger(actor2.transform);
						}
						if (i > 0 && i % 3 == 0)
						{
							yield return null;
						}
					}
					break;
				}
				}
			}
			else if (tEvent.triggerType == ScenarioTriggerEvents.TriggerEvent.TriggerTypes.Conditional)
			{
				if (tEvent.conditional == null)
				{
					break;
				}
				fired = tEvent.conditional.GetCondition();
			}
			if (fired)
			{
				Trigger();
				break;
			}
			yield return null;
		}
	}

	private bool DoesPlayerTrigger()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			List<PlayerInfo> connectedPlayers = VTOLMPLobbyManager.instance.connectedPlayers;
			for (int i = 0; i < connectedPlayers.Count; i++)
			{
				PlayerInfo playerInfo = connectedPlayers[i];
				if (playerInfo != null && (bool)playerInfo.vehicleActor && playerInfo.vehicleActor.alive && DoesTransformTrigger(playerInfo.vehicleActor.transform))
				{
					return true;
				}
			}
			return false;
		}
		if ((bool)FlightSceneManager.instance.playerActor)
		{
			return DoesTransformTrigger(FlightSceneManager.instance.playerActor.transform);
		}
		return false;
	}

	private bool DoesTransformTrigger(Transform tf)
	{
		if (!tf)
		{
			return false;
		}
		if (tf == tEvent.waypoint.GetTransform())
		{
			return false;
		}
		Vector3 vector = tf.position - tEvent.waypoint.worldPosition;
		if (!tEvent.sphericalRadius)
		{
			vector.y = 0f;
		}
		float sqrMagnitude = vector.sqrMagnitude;
		if (tEvent.proxyMode == TriggerProximityModes.OnEnter)
		{
			return sqrMagnitude < sqrRad;
		}
		return sqrMagnitude > sqrRad;
	}
}
