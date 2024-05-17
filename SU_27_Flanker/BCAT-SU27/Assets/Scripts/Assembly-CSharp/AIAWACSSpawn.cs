using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking;
using VTOLVR.Multiplayer;

public class AIAWACSSpawn : AIAircraftSpawn
{
	public delegate void ContactListDelegate(List<ContactGroup> contacts);

	public delegate void ThreatToAwacsDelegate(int count, Vector3 position, Vector3 velocity);

	public delegate void ContactDelegate(ContactGroup contact, bool braaOnly);

	public class ContactGroup
	{
		public int count;

		public FixedPoint globalPos;

		public Vector3 velocity;

		public float sqrDistToPlayer;
	}

	public static List<AIAWACSSpawn> alliedAwacs = new List<AIAWACSSpawn>();

	public static List<AIAWACSSpawn> enemyAwacs = new List<AIAWACSSpawn>();

	[Header("AWACS")]
	[UnitSpawnAttributeConditional("IsAllied")]
	[UnitSpawn("AWACS Voice")]
	public AWACSVoiceProfile awacsVoiceProfile;

	private static List<AWACSVoiceProfile> voiceProfiles = null;

	private static int awacsVoiceIdx;

	[UnitSpawnAttributeConditional("IsAllied")]
	[UnitSpawn("Comms Enabled")]
	public bool commsEnabled = true;

	private List<ContactGroup> popupGroups = new List<ContactGroup>();

	private bool collectingPopups;

	private List<Actor> knownHostiles = new List<Actor>();

	private Coroutine grandslamRoutine;

	private List<ContactGroup> contactPicture = new List<ContactGroup>();

	private const float SQR_GROUP_THRESHOLD = 2250000f;

	private const float GROUP_VEL_DOT_THRESHOLD = 0.7f;

	private float timePictureGenerated;

	private Comparison<ContactGroup> sortByDistToPlayer = (ContactGroup a, ContactGroup b) => a.sqrDistToPlayer.CompareTo(b.sqrDistToPlayer);

	public event ContactListDelegate OnCalledPopups;

	public event ThreatToAwacsDelegate OnReportedThreatToAWACS;

	public event Action OnReportedGrandSlam;

	public event ContactListDelegate OnReportedPicture;

	public event Action OnReportUnable;

	public event ContactDelegate OnReportedContact;

	public event Action<Actor> OnDetectedActor;

	public event Action OnRequestedPicture;

	public event Action OnRequestedNearestHostile;

	public static AIAWACSSpawn GetClosestAlliedAwacs()
	{
		float num = float.MaxValue;
		AIAWACSSpawn result = null;
		List<AIAWACSSpawn> list = alliedAwacs;
		if (VTOLMPUtils.IsMultiplayer() && VTOLMPLobbyManager.localPlayerInfo.team == Teams.Enemy)
		{
			list = enemyAwacs;
		}
		foreach (AIAWACSSpawn item in list)
		{
			float sqrMagnitude = (FlightSceneManager.instance.playerActor.position - item.actor.position).sqrMagnitude;
			if (sqrMagnitude < num)
			{
				num = sqrMagnitude;
				result = item;
			}
		}
		return result;
	}

	private static AWACSVoiceProfile GetShuffledVoiceProfile()
	{
		if (voiceProfiles == null || awacsVoiceIdx >= voiceProfiles.Count)
		{
			List<AWACSVoiceProfile> list = ((voiceProfiles == null) ? VTResources.GetAllAWACSVoices() : voiceProfiles);
			voiceProfiles = new List<AWACSVoiceProfile>();
			while (list.Count > 0)
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				voiceProfiles.Add(list[index]);
				list.RemoveAt(index);
			}
			awacsVoiceIdx = 0;
		}
		AWACSVoiceProfile result = voiceProfiles[awacsVoiceIdx];
		awacsVoiceIdx++;
		return result;
	}

	public bool IsAllied()
	{
		return actor.team == Teams.Allied;
	}

	public override void OnPreSpawnUnit()
	{
		base.OnPreSpawnUnit();
		if (awacsVoiceProfile == null)
		{
			awacsVoiceProfile = GetShuffledVoiceProfile();
		}
	}

	public override void OnSpawnUnit()
	{
		base.OnSpawnUnit();
		if (actor.team == Teams.Allied)
		{
			alliedAwacs.Add(this);
		}
		else
		{
			enemyAwacs.Add(this);
		}
		if (VTScenario.isScenarioHost)
		{
			aiPilot.detectionRadar.OnDetectedActor += AwacsDetectedActor;
			Actor.OnActorKilled += Actor_OnActorKilled;
			StartCoroutine(CheckForThreatsRoutine());
		}
	}

	public void AwacsDetectedActor(Actor a)
	{
		AwacsDetectedActor(a, callPopups: true);
	}

	public void AwacsDetectedActor(Actor a, bool callPopups)
	{
		if (!commsEnabled || a.team == actor.team || a.role == Actor.Roles.Missile || !a.alive)
		{
			return;
		}
		this.OnDetectedActor?.Invoke(a);
		if (knownHostiles.Contains(a))
		{
			return;
		}
		knownHostiles.Add(a);
		if (!callPopups || !FlightSceneManager.instance.playerActor)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < popupGroups.Count; i++)
		{
			if (flag)
			{
				break;
			}
			ContactGroup contactGroup = popupGroups[i];
			if ((a.position - contactGroup.globalPos.point).sqrMagnitude < 2250000f && Vector3.Dot(contactGroup.velocity.normalized, a.velocity.normalized) > 0.7f)
			{
				int count = contactGroup.count;
				contactGroup.count++;
				contactGroup.globalPos = new FixedPoint((contactGroup.globalPos.point * count + a.position) / contactGroup.count);
				contactGroup.velocity = (contactGroup.velocity * count + a.velocity) / contactGroup.count;
				contactGroup.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup.globalPos.point).sqrMagnitude;
				flag = true;
			}
		}
		if (!flag)
		{
			ContactGroup contactGroup2 = new ContactGroup();
			contactGroup2.count = 1;
			contactGroup2.globalPos = new FixedPoint(a.position);
			contactGroup2.velocity = a.velocity;
			contactGroup2.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup2.globalPos.point).sqrMagnitude;
			popupGroups.Add(contactGroup2);
		}
		if (!collectingPopups)
		{
			StartCoroutine(PopupRoutine());
		}
	}

	private IEnumerator PopupRoutine()
	{
		collectingPopups = true;
		yield return new WaitForSeconds(4f);
		collectingPopups = false;
		popupGroups.Sort(sortByDistToPlayer);
		if (actor.alive && IsPlayerTeam())
		{
			awacsVoiceProfile.ReportPopups(popupGroups, 0, 3);
			this.OnCalledPopups?.Invoke(popupGroups);
		}
		popupGroups.Clear();
	}

	private bool IsPlayerTeam()
	{
		if (VTOLMPUtils.IsMultiplayer())
		{
			PlayerInfo localPlayerInfo = VTOLMPLobbyManager.localPlayerInfo;
			if (localPlayerInfo != null && localPlayerInfo.chosenTeam)
			{
				return localPlayerInfo.team == actor.team;
			}
			return false;
		}
		return actor.team == Teams.Allied;
	}

	private IEnumerator CheckForThreatsRoutine()
	{
		float interval = 4f;
		float minThreatAnnouncementInterval = 60f;
		float timeThreatAnnounced = 0f;
		WaitForSeconds wait = new WaitForSeconds(interval);
		ContactGroup threatGroup = null;
		while (base.enabled && (bool)actor && actor.alive)
		{
			if (commsEnabled)
			{
				if (Time.time - timePictureGenerated > interval)
				{
					GeneratePicture();
				}
				float num = 900000000f;
				foreach (Actor item in (actor.team == Teams.Allied) ? TargetManager.instance.alliedUnits : TargetManager.instance.enemyUnits)
				{
					if (!item)
					{
						continue;
					}
					Actor.Roles roles = item.role;
					if (item.overrideCombatTarget)
					{
						roles = item.overriddenCombatRole;
					}
					if (roles != Actor.Roles.Air || !item.alive || !(item != actor))
					{
						continue;
					}
					AIPilot component = item.GetComponent<AIPilot>();
					if (!component || component.combatRole == AIPilot.CombatRoles.Fighter || component.combatRole == AIPilot.CombatRoles.FighterAttack)
					{
						float sqrMagnitude = (item.position - actor.position).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
						}
					}
				}
				ContactGroup contactGroup = null;
				float num2 = float.MaxValue;
				foreach (ContactGroup item2 in contactPicture)
				{
					Vector3 velocity = item2.velocity;
					velocity.y = 0f;
					velocity.Normalize();
					Vector3 lhs = aiPilot.transform.position - item2.globalPos.point;
					lhs.y = 0f;
					float magnitude = lhs.magnitude;
					lhs /= magnitude;
					if (magnitude * magnitude < num && Vector3.Dot(lhs, velocity) > 0.8f && magnitude < num2)
					{
						num2 = magnitude;
						contactGroup = item2;
					}
				}
				if (contactGroup != null && contactGroup != threatGroup && Time.time - timeThreatAnnounced > minThreatAnnouncementInterval && IsPlayerTeam())
				{
					timeThreatAnnounced = Time.time;
					threatGroup = contactGroup;
					awacsVoiceProfile.ReportThreatToAwacs(threatGroup.count, threatGroup.globalPos.point, threatGroup.velocity);
					this.OnReportedThreatToAWACS?.Invoke(threatGroup.count, threatGroup.globalPos.point, threatGroup.velocity);
				}
			}
			yield return wait;
		}
	}

	private void OnDestroy()
	{
		if (alliedAwacs != null)
		{
			alliedAwacs.Remove(this);
		}
		Actor.OnActorKilled -= Actor_OnActorKilled;
	}

	public List<Actor> GetKnownHostiles()
	{
		return knownHostiles;
	}

	private void Actor_OnActorKilled(Actor killedActor)
	{
		if (killedActor == actor)
		{
			if (alliedAwacs != null)
			{
				alliedAwacs.Remove(this);
			}
		}
		else if (actor.team == Teams.Allied && knownHostiles.Remove(killedActor) && knownHostiles.Count == 0)
		{
			if (grandslamRoutine != null)
			{
				StopCoroutine(grandslamRoutine);
			}
			grandslamRoutine = StartCoroutine(DelayedReportGrandslam());
		}
	}

	private IEnumerator DelayedReportGrandslam()
	{
		yield return new WaitForSeconds(2f);
		if (actor.alive && commsEnabled && IsPlayerTeam())
		{
			awacsVoiceProfile.ReportGrandSlam();
			this.OnReportedGrandSlam?.Invoke();
		}
	}

	public void RequestRTB()
	{
		if (!commsEnabled || !actor.alive)
		{
			return;
		}
		Transform rTBWaypoint = VTScenario.current.GetRTBWaypoint();
		if ((bool)rTBWaypoint)
		{
			awacsVoiceProfile.ReportHomeplateBra(rTBWaypoint.position);
			return;
		}
		AirportManager airportManager = GetClosestAirport();
		if ((bool)airportManager)
		{
			awacsVoiceProfile.ReportHomeplateBra(airportManager.transform.position);
		}
		else
		{
			awacsVoiceProfile.ReportUnable();
		}
	}

	private AirportManager GetClosestAirport()
	{
		FlightInfo flightInfo = FlightSceneManager.instance.playerActor.flightInfo;
		if (!flightInfo)
		{
			Debug.LogError("MFDCommsPage has no FlightInfo reference!!");
			return null;
		}
		AirportManager result = null;
		float num = float.MaxValue;
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			if (airport.team == Teams.Allied)
			{
				float sqrMagnitude = (flightInfo.transform.position - airport.transform.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = airport;
				}
			}
		}
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if (value.spawned && (bool)value.spawnedUnit && value.spawnedUnit is AICarrierSpawn && value.spawnedUnit.actor.alive)
			{
				float sqrMagnitude2 = (flightInfo.transform.position - value.spawnedUnit.transform.position).sqrMagnitude;
				if (sqrMagnitude2 < num)
				{
					num = sqrMagnitude2;
					result = ((AICarrierSpawn)value.spawnedUnit).airportManager;
				}
			}
		}
		return result;
	}

	public void RequestPicture()
	{
		this.OnRequestedPicture?.Invoke();
		if (!commsEnabled || !actor.alive)
		{
			return;
		}
		if (!aiPilot.detectionRadar.radarEnabled)
		{
			awacsVoiceProfile.ReportUnable();
			this.OnReportUnable?.Invoke();
			return;
		}
		GeneratePicture();
		if (contactPicture.Count > 0)
		{
			awacsVoiceProfile.ReportGroups(contactPicture, 0, 3);
			this.OnReportedPicture?.Invoke(contactPicture);
		}
		else
		{
			awacsVoiceProfile.ReportPictureClean();
			this.OnReportedPicture?.Invoke(null);
		}
	}

	public bool CanReportTargets()
	{
		if (commsEnabled && actor.alive)
		{
			return aiPilot.detectionRadar.radarEnabled;
		}
		return false;
	}

	public ContactGroup RequestNearestHostile()
	{
		this.OnRequestedNearestHostile?.Invoke();
		if (!CanReportTargets())
		{
			return null;
		}
		GeneratePicture();
		if (contactPicture.Count > 0)
		{
			ReportContactGroup(contactPicture[0], braaOnly: true);
			return contactPicture[0];
		}
		awacsVoiceProfile.ReportPictureClean();
		this.OnReportedPicture?.Invoke(null);
		return null;
	}

	private void ReportContactGroup(ContactGroup g, bool braaOnly)
	{
		if (g.count > 1)
		{
			awacsVoiceProfile.ReportGroup(g.globalPos.point, g.velocity, braaOnly);
		}
		else
		{
			awacsVoiceProfile.ReportHostile(g.globalPos.point, g.velocity, braaOnly);
		}
		this.OnReportedContact?.Invoke(g, braaOnly);
	}

	private void GeneratePicture()
	{
		contactPicture.Clear();
		float time = (VTOLMPUtils.IsMultiplayer() ? VTNetworkManager.GetNetworkTimestamp() : Time.time);
		knownHostiles.RemoveAll((Actor x) => !x || !x.alive || time - x.LastSeenTime(actor.team) > 80f);
		if (!FlightSceneManager.instance.playerActor)
		{
			return;
		}
		foreach (Actor knownHostile in knownHostiles)
		{
			if (!knownHostile || knownHostile.team == actor.team || !knownHostile.discovered || !knownHostile.detectedByAllied || !knownHostile.alive)
			{
				continue;
			}
			bool flag = false;
			for (int i = 0; i < contactPicture.Count; i++)
			{
				if (flag)
				{
					break;
				}
				ContactGroup contactGroup = contactPicture[i];
				if ((knownHostile.position - contactGroup.globalPos.point).sqrMagnitude < 2250000f && Vector3.Dot(contactGroup.velocity.normalized, knownHostile.velocity.normalized) > 0.7f)
				{
					int count = contactGroup.count;
					contactGroup.count++;
					contactGroup.globalPos = new FixedPoint((contactGroup.globalPos.point * count + knownHostile.position) / contactGroup.count);
					contactGroup.velocity = (contactGroup.velocity * count + knownHostile.velocity) / contactGroup.count;
					contactGroup.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup.globalPos.point).sqrMagnitude;
					flag = true;
				}
			}
			if (!flag)
			{
				ContactGroup contactGroup2 = new ContactGroup();
				contactGroup2.count = 1;
				contactGroup2.globalPos = new FixedPoint(knownHostile.position);
				contactGroup2.velocity = knownHostile.velocity;
				contactGroup2.sqrDistToPlayer = (FlightSceneManager.instance.playerActor.position - contactGroup2.globalPos.point).sqrMagnitude;
				contactPicture.Add(contactGroup2);
			}
		}
		contactPicture.Sort(sortByDistToPlayer);
		timePictureGenerated = Time.time;
	}

	public override void Quicksave(ConfigNode qsNode)
	{
		base.Quicksave(qsNode);
		qsNode.SetValue("awacsVoiceProfile", awacsVoiceProfile.name);
	}

	public override void Quickload(ConfigNode qsNode)
	{
		base.Quickload(qsNode);
		string value = qsNode.GetValue("awacsVoiceProfile");
		awacsVoiceProfile = VTResources.GetAWACSVoice(value);
	}
}
