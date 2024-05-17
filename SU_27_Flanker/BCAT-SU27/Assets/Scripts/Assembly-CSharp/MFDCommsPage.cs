using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using VTOLVR.DLC.Rotorcraft;

public class MFDCommsPage : MonoBehaviour, IQSVehicleComponent, ILocalizationUser
{
	public GameObject replyObject;

	public Text replyText;

	private List<ModuleEngine> _engs;

	private WeaponManager _wm;

	private FlightInfo _fi;

	public TargetingMFDPage targetingPage;

	public MFDRadarUI radarPage;

	public MFDAntiRadarAttackDisplay aradPage;

	public MFDPTacticalSituationDisplay tsdPage;

	public MFDPortalPage portalPage;

	private VehicleMaster _vm;

	private ReArmingPoint currentRP;

	private Coroutine replyRoutine;

	public MFDOptionBrowser browser;

	private string s_page_wingmenCombat = "Wingmen / Combat";

	private string s_opt_attackTarget = "Attack Target";

	private string s_opt_disengage = "Disengage";

	private string s_opt_engage = "Engage";

	private string s_page_wingmenFlight = "Wingmen / Flight";

	private string s_opt_orbitHere = "Orbit Here";

	private string s_opt_formUp = "Form Up";

	private string s_opt_spreadClose = "Spread Close";

	private string s_opt_spreadMed = "Spread Med";

	private string s_opt_spreadFar = "Spread Far";

	private string s_opt_goRefuel = "Go Refuel";

	private string s_opt_rtb = "RTB";

	private string s_opt_rtbRearm = "RTB & Re-arm";

	private string s_page_wingmenEquip = "Wingmen / Equipment";

	private string s_opt_radarOn = "Radar On";

	private string s_opt_radarOff = "Radar Off";

	private string s_page_wingmen = "Wingmen";

	private string s_opt_combat = "Combat";

	private string s_opt_flight = "Flight";

	private string s_opt_equipment = "Equipment";

	private string s_page_groundCrew = "Ground Crew";

	private string s_opt_requestRearm = "Request Re-arm";

	private string s_page_atc = "ATC";

	private string s_opt_takeOff = "Take Off";

	private string s_opt_landing = "Landing";

	private string s_opt_cancelRequest = "Cancel Request";

	private string s_opt_vertTakeoff = "Vert T/O";

	private string s_opt_vertLanding = "Vert Landing";

	private string s_page_awacs = "AWACS";

	private string s_opt_bogeyDope = "Bogey Dope";

	private string s_opt_picture = "Picture";

	private string s_opt_rtbVec = "Request RTB Vector";

	private string s_page_comms = "Comms";

	private string s_opt_wingmen = "Wingmen";

	private string s_opt_ground = "Ground";

	private string s_opt_atc = "ATC";

	private string s_opt_awacs = "AWACS";

	private string s_opt_lastContact = "Last Contact";

	private string s_page_contactTower = "Contact Tower";

	private MFDOptionBrowser.MFDOptionPage atcCommsPage;

	private AirFormationLeader _playerFormation;

	private Transform wingOrbitTransform;

	private KeywordRecognizer wingmanRecognizer;

	private KeywordRecognizer atcRecognizer;

	private KeywordRecognizer awacsRecognizer;

	private bool isRecognitionEnabled;

	private List<KeywordRecognizer> specifiedATCRecognizers = new List<KeywordRecognizer>();

	private bool createdSpecifiedATCRecognizers;

	private Action<bool> OnBallCalled;

	private Action PlayerWavedOff;

	private bool awaitingBallCall;

	private AirportManager mfdSelectedAirport;

	private float lastTakeoffRequestTime;

	private float lastVTakeoffRequestTime;

	private float lastVLandingRequestTime;

	private float lastLandingRequestTime;

	private AirportManager lastContactedAirport;

	private List<AIPilot> commandablePilots = new List<AIPilot>();

	private Coroutine foxWaitRoutine;

	private string s_rearmingNotAvailable = "Rearming not available.";

	private string s_notAtRearmingStation = "Not at a rearming station. Return to base.";

	private string s_turnOffEngines = "Please turn off your engines.";

	private string s_disarmWeapons = "Please disarm your weapon system.";

	private string s_unhookTailhook = "Please disengage your arrestor hook.";

	private string s_rearmPointOccupied = "This station is occupied!";

	private string s_taxiToRearm = "Please taxi to a rearming station.";

	private string s_wingmenBusy = "Your wingmen are busy.";

	private string s_someWingmenBusy = "Some of your wingmen are busy.";

	private string s_noWingmen = "You have no wingmen.";

	private string s_wingmenOrbiting = "Wingmen orbiting here.";

	private string s_wingmenEngaging = "Wingmen engaging targets at will!";

	private string s_wingmenDisengaging = "Wingmen disengaging from combat!";

	private string s_wingmenFormationSpread = "Wingmen setting formation spread.";

	private string s_wingman = "Wingman";

	private string s_attackingTSD = "attacking your TSD target!";

	private string s_attackingRadar = "attacking your radar target!";

	private string s_attackingTGP = "attacking your TGP target!";

	private string s_attackingARAD = "attacking your ARAD target!";

	private string s_noWingmenForAttack = "No wingmen available for attack.";

	private string s_noTarget = "No target selected.";

	private string s_noAirfield = "No airfield available.";

	private string s_cancellingATC = "Cancelling ATC request.";

	private string s_acknowledged = "Acknowledged";

	private string s_wingmenRTB = "Your wingmen will RTB!";

	private string s_clearedVerticalLanding = "Cleared for vertical landing!";

	private string s_clearedVerticalTakeoff = "Cleared for vertical take off!";

	private string s_landingDenied = "Landing denied!";

	private string s_takeoffDenied = "Take off denied!";

	private string s_reply_noPrevAtc = "No previously contacted ATC available.";

	private string s_noHostilesFound = "No hostiles found.";

	private string s_awacsNotReady = "AWACS is not ready to report.";

	private string s_noAwacsAvailable = "No AWACS available";

	private string s_awacsPicRequested = "AWACS picture requested";

	private string s_awacsRequestedRTB = "Requested RTB vector from AWACS";

	private string s_hostile = "Hostile";

	private string s_comm_reply = "REPLY:";

	private List<ModuleEngine> engines
	{
		get
		{
			if (_engs == null)
			{
				GetEngines();
			}
			return _engs;
		}
	}

	public WeaponManager wm
	{
		get
		{
			if (!_wm)
			{
				_wm = GetComponentInParent<WeaponManager>();
			}
			return _wm;
		}
	}

	private FlightInfo flightInfo
	{
		get
		{
			if (!_fi)
			{
				_fi = GetComponentInParent<FlightInfo>();
			}
			return _fi;
		}
	}

	private VehicleMaster vm
	{
		get
		{
			GetVM();
			return _vm;
		}
	}

	public bool forceDisallowRearm { get; set; }

	private AirFormationLeader playerFormation
	{
		get
		{
			if (!_playerFormation)
			{
				_playerFormation = FlightSceneManager.instance.playerActor.GetComponent<AirFormationLeader>();
			}
			return _playerFormation;
		}
	}

	public event Action OnRequestingRearming;

	private void GetVM()
	{
		if (!_vm)
		{
			_vm = GetComponentInParent<VehicleMaster>();
			_vm.OnPilotDied += OnPilotDied;
		}
	}

	private void OnEnable()
	{
		replyObject.SetActive(value: false);
	}

	private void Awake()
	{
		Debug.Log("MFDCommsPage Awake");
		ApplyLocalization();
		SetupPages();
		if ((bool)browser.mfdPage)
		{
			browser.mfdPage.OnActivatePage.AddListener(browser.GoHomepage);
		}
		if ((bool)portalPage)
		{
			portalPage.OnShowPage.AddListener(browser.GoHomepage);
		}
	}

	private void OnPilotDied()
	{
		StopRecognition();
	}

	private void GetEngines()
	{
		if (_engs != null && _engs.Count != 0)
		{
			return;
		}
		VTOLQuickStart componentInChildren = GetComponentInParent<Actor>().GetComponentInChildren<VTOLQuickStart>();
		_engs = new List<ModuleEngine>();
		if ((bool)componentInChildren)
		{
			VTOLQuickStart.QuickStartComponents.QSEngine[] array = componentInChildren.quickStartComponents.engines;
			foreach (VTOLQuickStart.QuickStartComponents.QSEngine qSEngine in array)
			{
				_engs.Add(qSEngine.engine);
			}
		}
	}

	private bool PlayerHasWingmen()
	{
		if (commandablePilots.Count == 0)
		{
			return false;
		}
		return true;
	}

	public void RequestRearming()
	{
		if (forceDisallowRearm)
		{
			ShowReply(s_rearmingNotAvailable, 5f);
		}
		else
		{
			if ((bool)currentRP)
			{
				return;
			}
			GetEngines();
			if (PilotSaveManager.currentScenario == null || !PilotSaveManager.currentScenario.equipConfigurable)
			{
				ShowReply(s_rearmingNotAvailable, 5f);
				ReArmingPoint closestRearmingPoint = GetClosestRearmingPoint();
				if ((bool)closestRearmingPoint)
				{
					closestRearmingPoint.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.NotAvailable);
				}
				else
				{
					VTResources.GetDefaultGroundCrewVoice().PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.NotAvailable);
				}
				return;
			}
			if (!flightInfo.isLanded)
			{
				ShowReply(s_notAtRearmingStation, 5f);
				GetClosestRearmingPoint()?.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.IsAirborne);
				return;
			}
			currentRP = vm.currentRearmingPoint;
			if ((bool)currentRP)
			{
				if (!currentRP.CheckIsClear(FlightSceneManager.instance.playerActor))
				{
					ShowReply(s_rearmPointOccupied, 5f);
					currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.NotAvailable);
					currentRP = null;
					return;
				}
				foreach (ModuleEngine engine in engines)
				{
					if (!engine.useTorquePhysics && (engine.startedUp || engine.startingUp))
					{
						ShowReply(s_turnOffEngines, 5f);
						currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.TurnOffEngines);
						currentRP = null;
						return;
					}
				}
				HelicopterRotor[] componentsInChildren = FlightSceneManager.instance.playerActor.GetComponentsInChildren<HelicopterRotor>();
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					if (componentsInChildren[i].inputShaft.outputRPM > 30f)
					{
						ShowReply(s_turnOffEngines, 5f);
						currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.TurnOffEngines);
						currentRP = null;
						return;
					}
				}
				if (wm.isMasterArmed)
				{
					ShowReply(s_disarmWeapons, 5f);
					currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.DisarmWeapons);
					currentRP = null;
					return;
				}
				Tailhook componentInChildren = wm.GetComponentInChildren<Tailhook>();
				if ((bool)componentInChildren && (bool)componentInChildren.hookedCable)
				{
					ShowReply(s_unhookTailhook, 5f);
					currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.NotAvailable);
					currentRP = null;
				}
				else
				{
					currentRP.OnEndRearm += CurrentRP_OnEndRearm;
					currentRP.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.Success);
					Debug.Log("Comms beginning rearm.");
					this.OnRequestingRearming?.Invoke();
					currentRP.BeginReArm();
				}
			}
			else
			{
				GetClosestRearmingPoint()?.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.TaxiToStation);
				ShowReply(s_taxiToRearm, 4f);
			}
		}
	}

	private ReArmingPoint GetClosestRearmingPoint()
	{
		ReArmingPoint result = null;
		float num = float.MaxValue;
		foreach (ReArmingPoint reArmingPoint in ReArmingPoint.reArmingPoints)
		{
			if ((bool)reArmingPoint && reArmingPoint.team == wm.actor.team)
			{
				float sqrMagnitude = (reArmingPoint.transform.position - wm.actor.position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = reArmingPoint;
				}
			}
		}
		return result;
	}

	private void CurrentRP_OnEndRearm()
	{
		if ((bool)currentRP)
		{
			currentRP.OnEndRearm -= CurrentRP_OnEndRearm;
			currentRP = null;
		}
	}

	private void ShowReply(string reply, float time)
	{
		if (replyRoutine != null)
		{
			StopCoroutine(replyRoutine);
		}
		if (base.enabled && base.gameObject.activeInHierarchy)
		{
			replyRoutine = wm.StartCoroutine(ShowReplyRoutine(reply, time));
		}
	}

	private IEnumerator ShowReplyRoutine(string reply, float time)
	{
		StringBuilder sb = new StringBuilder();
		sb.Append(s_comm_reply);
		sb.Append(" ");
		replyText.text = sb.ToString();
		replyObject.SetActive(value: true);
		yield return new WaitForSeconds(0.5f);
		for (int i = 0; i < reply.Length; i++)
		{
			sb.Append(reply[i]);
			replyText.text = sb.ToString();
			yield return null;
		}
		yield return new WaitForSeconds(time);
		replyObject.SetActive(value: false);
	}

	private void ApplyOptionBrowserLocalization()
	{
		Func<string, string> func = (string title) => VTLocalizationManager.GetString("MFDOptionPage:" + title, title, "Title of an MFD option page");
		Func<string, string> func2 = (string label) => VTLocalizationManager.GetString("MFDOption:" + label, label, "Label of an MFD option");
		s_page_wingmenCombat = func(s_page_wingmenCombat);
		s_opt_attackTarget = func2(s_opt_attackTarget);
		s_opt_disengage = func2(s_opt_disengage);
		s_opt_engage = func2(s_opt_engage);
		s_page_wingmenFlight = func(s_page_wingmenFlight);
		s_opt_orbitHere = func2(s_opt_orbitHere);
		s_opt_formUp = func2(s_opt_formUp);
		s_opt_spreadClose = func2(s_opt_spreadClose);
		s_opt_spreadMed = func2(s_opt_spreadMed);
		s_opt_spreadFar = func2(s_opt_spreadFar);
		s_opt_goRefuel = func2(s_opt_goRefuel);
		s_opt_rtb = func2(s_opt_rtb);
		s_opt_rtbRearm = func2(s_opt_rtbRearm);
		s_page_wingmenEquip = func(s_page_wingmenEquip);
		s_opt_radarOn = func2(s_opt_radarOn);
		s_opt_radarOff = func2(s_opt_radarOff);
		s_page_wingmen = func(s_page_wingmen);
		s_opt_combat = func2(s_opt_combat);
		s_opt_flight = func2(s_opt_flight);
		s_opt_equipment = func2(s_opt_equipment);
		s_page_groundCrew = func(s_page_groundCrew);
		s_opt_requestRearm = func2(s_opt_requestRearm);
		s_page_atc = func(s_page_atc);
		s_opt_takeOff = func2(s_opt_takeOff);
		s_opt_landing = func2(s_opt_landing);
		s_opt_cancelRequest = func2(s_opt_cancelRequest);
		s_opt_vertTakeoff = func2(s_opt_vertTakeoff);
		s_opt_vertLanding = func2(s_opt_vertLanding);
		s_opt_lastContact = func2(s_opt_lastContact);
		s_page_contactTower = func(s_page_contactTower);
		s_page_awacs = func(s_page_awacs);
		s_opt_bogeyDope = func2(s_opt_bogeyDope);
		s_opt_picture = func2(s_opt_picture);
		s_opt_rtbVec = func2(s_opt_rtbVec);
		s_page_comms = func(s_page_comms);
		s_opt_wingmen = func2(s_opt_wingmen);
		s_opt_ground = func2(s_opt_ground);
		s_opt_atc = func2(s_opt_atc);
		s_opt_awacs = func2(s_opt_awacs);
	}

	private void SetupPages()
	{
		MFDOptionBrowser.MFDOptionPage targetPage = new MFDOptionBrowser.MFDOptionPage(s_page_wingmenCombat, new MFDOptionBrowser.MFDOption(s_opt_attackTarget, AttackMyTarget), new MFDOptionBrowser.MFDOption(s_opt_disengage, DisengageEnemies), new MFDOptionBrowser.MFDOption(s_opt_engage, EngageEnemies));
		MFDOptionBrowser.MFDOptionPage targetPage2 = new MFDOptionBrowser.MFDOptionPage(s_page_wingmenFlight, new MFDOptionBrowser.MFDOption(s_opt_orbitHere, OrbitHere), new MFDOptionBrowser.MFDOption(s_opt_formUp, FormOnMe), new MFDOptionBrowser.MFDOption(s_opt_spreadClose, FormationClose), new MFDOptionBrowser.MFDOption(s_opt_spreadMed, FormationMed), new MFDOptionBrowser.MFDOption(s_opt_spreadFar, FormationFar), new MFDOptionBrowser.MFDOption(s_opt_goRefuel, CommandGoRefuel), new MFDOptionBrowser.MFDOption(s_opt_rtb, CommandRTB), new MFDOptionBrowser.MFDOption(s_opt_rtbRearm, CommandRearm));
		MFDOptionBrowser.MFDOptionPage targetPage3 = new MFDOptionBrowser.MFDOptionPage(s_page_wingmenEquip, new MFDOptionBrowser.MFDOption(s_opt_radarOn, WingmenRadarOn), new MFDOptionBrowser.MFDOption(s_opt_radarOff, WingmenRadarOff));
		MFDOptionBrowser.MFDOptionPage targetPage4 = new MFDOptionBrowser.MFDOptionPage(s_page_wingmen, new MFDOptionBrowser.MFDOption(s_opt_combat, targetPage, browser), new MFDOptionBrowser.MFDOption(s_opt_flight, targetPage2, browser), new MFDOptionBrowser.MFDOption(s_opt_equipment, targetPage3, browser));
		MFDOptionBrowser.MFDOptionPage targetPage5 = new MFDOptionBrowser.MFDOptionPage(s_page_groundCrew, new MFDOptionBrowser.MFDOption(s_opt_requestRearm, RequestRearming));
		if (vm.isVTOLCapable)
		{
			atcCommsPage = new MFDOptionBrowser.MFDOptionPage(s_page_atc, new MFDOptionBrowser.MFDOption(s_opt_takeOff, delegate
			{
				RequestTakeoff(mfdSelectedAirport);
			}), new MFDOptionBrowser.MFDOption(s_opt_landing, delegate
			{
				RequestLanding(mfdSelectedAirport);
			}), new MFDOptionBrowser.MFDOption(s_opt_cancelRequest, CancelATCRequest), new MFDOptionBrowser.MFDOption(s_opt_vertTakeoff, delegate
			{
				RequestVerticalTakeoff(mfdSelectedAirport);
			}), new MFDOptionBrowser.MFDOption(s_opt_vertLanding, delegate
			{
				RequestVerticalLanding(mfdSelectedAirport);
			}));
		}
		else
		{
			atcCommsPage = new MFDOptionBrowser.MFDOptionPage(s_page_atc, new MFDOptionBrowser.MFDOption(s_opt_takeOff, delegate
			{
				RequestTakeoff(mfdSelectedAirport);
			}), new MFDOptionBrowser.MFDOption(s_opt_landing, delegate
			{
				RequestLanding(mfdSelectedAirport);
			}), new MFDOptionBrowser.MFDOption(s_opt_cancelRequest, CancelATCRequest));
		}
		MFDOptionBrowser.MFDOptionPage targetPage6 = new MFDOptionBrowser.MFDOptionPage(s_page_awacs, new MFDOptionBrowser.MFDOption(s_opt_bogeyDope, MFDAwacsBogeyDope), new MFDOptionBrowser.MFDOption(s_opt_picture, MFDAwacsPicture), new MFDOptionBrowser.MFDOption(s_opt_rtbVec, MFDAwacsRTB));
		browser.homePage = new MFDOptionBrowser.MFDOptionPage(s_page_comms, new MFDOptionBrowser.MFDOption(s_opt_wingmen, targetPage4, browser), new MFDOptionBrowser.MFDOption(s_opt_ground, targetPage5, browser), new MFDOptionBrowser.MFDOption(s_opt_atc, OpenAirportSelectionPage), new MFDOptionBrowser.MFDOption(s_opt_awacs, targetPage6, browser));
	}

	private void MFDAwacsBogeyDope()
	{
		AIAWACSSpawn closestAlliedAwacs = AIAWACSSpawn.GetClosestAlliedAwacs();
		if ((bool)closestAlliedAwacs)
		{
			if (closestAlliedAwacs.CanReportTargets())
			{
				AIAWACSSpawn.ContactGroup contactGroup = closestAlliedAwacs.RequestNearestHostile();
				if (contactGroup != null)
				{
					float distance = Mathf.Sqrt(contactGroup.sqrDistToPlayer);
					float f = VectorUtils.Bearing(contactGroup.globalPos.point - flightInfo.transform.position);
					_ = $"{s_hostile} {Mathf.RoundToInt(f)}° {Mathf.RoundToInt(vm.measurementManager.ConvertedDistance(distance))}{vm.measurementManager.DistanceLabel()}";
				}
				else
				{
					ShowReply(s_noHostilesFound, 3f);
				}
			}
			else
			{
				if (closestAlliedAwacs.actor.alive && (bool)closestAlliedAwacs.awacsVoiceProfile)
				{
					closestAlliedAwacs.awacsVoiceProfile.ReportUnable();
				}
				ShowReply(s_awacsNotReady, 3f);
			}
		}
		else
		{
			ShowReply(s_noAwacsAvailable, 3f);
		}
	}

	private void MFDAwacsPicture()
	{
		AIAWACSSpawn closestAlliedAwacs = AIAWACSSpawn.GetClosestAlliedAwacs();
		if ((bool)closestAlliedAwacs)
		{
			if (closestAlliedAwacs.CanReportTargets())
			{
				closestAlliedAwacs.RequestPicture();
				ShowReply(s_awacsPicRequested, 3f);
				return;
			}
			if (closestAlliedAwacs.actor.alive && (bool)closestAlliedAwacs.awacsVoiceProfile)
			{
				closestAlliedAwacs.awacsVoiceProfile.ReportUnable();
			}
			ShowReply(s_awacsNotReady, 3f);
		}
		else
		{
			ShowReply(s_noAwacsAvailable, 3f);
		}
	}

	private void MFDAwacsRTB()
	{
		AIAWACSSpawn closestAlliedAwacs = AIAWACSSpawn.GetClosestAlliedAwacs();
		if ((bool)closestAlliedAwacs && closestAlliedAwacs.commsEnabled && closestAlliedAwacs.actor.alive)
		{
			closestAlliedAwacs.RequestRTB();
			ShowReply(s_awacsRequestedRTB, 3f);
		}
		else
		{
			ShowReply(s_noAwacsAvailable, 3f);
		}
	}

	private int CompareDists(AirportManager a, AirportManager b)
	{
		return SqrDist(a).CompareTo(SqrDist(b));
	}

	private float SqrDist(AirportManager a)
	{
		return (a.transform.position - vm.transform.position).sqrMagnitude;
	}

	private void OpenAirportSelectionPage()
	{
		List<AirportManager> list = new List<AirportManager>();
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			if (airport.team == wm.actor.team)
			{
				list.Add(airport);
			}
		}
		foreach (UnitSpawner item2 in (wm.actor.team == Teams.Allied) ? VTScenario.current.units.alliedUnits.Values : VTScenario.current.units.enemyUnits.Values)
		{
			if (item2.spawned && (bool)item2.spawnedUnit && item2.spawnedUnit is AICarrierSpawn && item2.spawnedUnit.actor.alive)
			{
				list.Add(((AICarrierSpawn)item2.spawnedUnit).airportManager);
			}
		}
		list.Sort(CompareDists);
		List<MFDOptionBrowser.MFDOption> list2 = new List<MFDOptionBrowser.MFDOption>();
		list2.Add(new MFDOptionBrowser.MFDOption(s_opt_lastContact, delegate
		{
			if ((bool)lastContactedAirport)
			{
				SetMFDContactAirport(lastContactedAirport);
				atcCommsPage.title = lastContactedAirport.airportName + " ATC";
				browser.OpenSubPage(atcCommsPage);
			}
			else
			{
				ShowReply(s_reply_noPrevAtc, 3f);
			}
		}));
		foreach (AirportManager item3 in list)
		{
			AirportManager contactAp = item3;
			MFDOptionBrowser.MFDOption item = new MFDOptionBrowser.MFDOption(item3.airportName, delegate
			{
				SetMFDContactAirport(contactAp);
				atcCommsPage.title = contactAp.airportName + " ATC";
				browser.OpenSubPage(atcCommsPage);
			});
			list2.Add(item);
		}
		MFDOptionBrowser.MFDOptionPage page = new MFDOptionBrowser.MFDOptionPage(s_page_contactTower, list2.ToArray());
		browser.OpenSubPage(page);
	}

	public void FormationClose()
	{
		SetFormationSpread(55f);
	}

	public void FormationMed()
	{
		SetFormationSpread(125f);
	}

	public void FormationFar()
	{
		SetFormationSpread(250f);
	}

	private void TryNonCombatCommand(string successReply)
	{
		List<AIPilot> list = commandablePilots;
		AIPilot aIPilot = null;
		AIPilot aIPilot2 = null;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].allowPlayerCommands && (list[i].commandState == AIPilot.CommandStates.Orbit || list[i].commandState == AIPilot.CommandStates.Navigation || list[i].commandState == AIPilot.CommandStates.FollowLeader))
			{
				aIPilot2 = list[i];
			}
			else
			{
				aIPilot = list[i];
			}
		}
		if (!aIPilot)
		{
			ShowReply(successReply, 4f);
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
		}
		else if (!aIPilot2)
		{
			ShowReply(s_wingmenBusy, 4f);
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
		else
		{
			ShowReply(s_someWingmenBusy, 4f);
			aIPilot.PlayRadioMessage(WingmanVoiceProfile.Messages.Deny, 2f);
			aIPilot2.PlayRadioMessage(WingmanVoiceProfile.Messages.Copy, 2f);
		}
	}

	private bool CheckIfWingmenCommandable()
	{
		if (commandablePilots.Count == 0)
		{
			if ((bool)AIWing.playerWing && AIWing.playerWing.pilots.Count > 0)
			{
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
				ShowReply(s_wingmenBusy, 4f);
			}
			else
			{
				ShowReply(s_noWingmen, 4f);
			}
			return false;
		}
		return true;
	}

	public void WingmenRadarOn()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		bool flag = false;
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			if (commandablePilot.allowPlayerCommands && (bool)commandablePilot.detectionRadar && !commandablePilot.detectionRadar.destroyed)
			{
				commandablePilot.playerComms_radarEnabled = true;
				flag = true;
			}
		}
		if (flag)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
		}
		else
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	public void WingmenRadarOff()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		bool flag = false;
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			commandablePilot.playerComms_radarEnabled = false;
			flag = true;
		}
		if (flag)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
		}
		else
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	public void FormOnMe()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		TryNonCombatCommand("Wingmen forming on you.");
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			commandablePilot.FormOnPlayer();
		}
	}

	public void OrbitHere()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		if (!wingOrbitTransform)
		{
			wingOrbitTransform = new GameObject("WingOrbitTransform").transform;
			wingOrbitTransform.gameObject.AddComponent<FloatingOriginTransform>();
		}
		wingOrbitTransform.position = FlightSceneManager.instance.playerActor.position;
		float defaultAltitude = Mathf.Max(500f, WaterPhysics.GetAltitude(wingOrbitTransform.position));
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			commandablePilot.defaultAltitude = defaultAltitude;
			commandablePilot.orbitRadius = 2000f;
			commandablePilot.OrbitTransform(wingOrbitTransform);
		}
		TryNonCombatCommand(s_wingmenOrbiting);
	}

	public void EngageEnemies()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		int num = 0;
		int num2 = commandablePilots.Count;
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			if ((bool)commandablePilot)
			{
				if ((bool)commandablePilot.aiSpawn)
				{
					commandablePilot.aiSpawn.SetEngageEnemies(engage: true);
				}
				else
				{
					commandablePilot.SetEngageEnemies(engage: true);
				}
				num++;
			}
			else
			{
				num2--;
			}
		}
		if (num > 0)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
			ShowReply(s_wingmenEngaging, 4f);
		}
		if (num < num2 && num2 > 0)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	public void DisengageEnemies()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		int num = 0;
		int num2 = commandablePilots.Count;
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			if ((bool)commandablePilot)
			{
				if ((bool)commandablePilot.aiSpawn)
				{
					commandablePilot.aiSpawn.SetEngageEnemies(engage: false);
				}
				else
				{
					commandablePilot.SetEngageEnemies(engage: false);
				}
				num++;
			}
			else
			{
				num2--;
			}
		}
		if (num > 0)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
			ShowReply(s_wingmenDisengaging, 4f);
		}
		if (num < num2 && num2 > 0)
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	private void SetFormationSpread(float spread)
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		TryNonCombatCommand(s_wingmenFormationSpread);
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			commandablePilot.FormOnPlayer();
		}
		playerFormation.SetSpread(spread);
	}

	public void AttackMyTarget()
	{
		GetEngines();
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		if ((bool)tsdPage && tsdPage.isSOI && tsdPage.tsc.GetCurrentSelectionActor() != null)
		{
			int num = AIWing.playerWing.AttackTarget(tsdPage.tsc.GetCurrentSelectionActor(), commandablePilots);
			if (num >= 0)
			{
				ShowReply($"{s_wingman} {num + 1}: {s_attackingTSD}", 5f);
				commandablePilots[num].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)radarPage && radarPage.isSOI && (bool)radarPage.currentLockedActor)
		{
			int num2 = AIWing.playerWing.AttackTarget(radarPage.currentLockedActor, commandablePilots);
			if (num2 >= 0)
			{
				ShowReply($"{s_wingman} {num2 + 1}: {s_attackingRadar}", 5f);
				commandablePilots[num2].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)targetingPage && targetingPage.isSOI && (bool)wm.opticalTargeter && wm.opticalTargeter.locked && (bool)wm.opticalTargeter.lockedActor)
		{
			int num3 = AIWing.playerWing.AttackTarget(wm.opticalTargeter.lockedActor, commandablePilots);
			if (num3 >= 0)
			{
				ShowReply($"{s_wingman} {num3 + 1}: {s_attackingTGP}", 5f);
				commandablePilots[num3].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)aradPage && (bool)aradPage.mfdPage && aradPage.mfdPage.isSOI && (bool)aradPage.selectedActor)
		{
			int num4 = AIWing.playerWing.AttackTarget(aradPage.selectedActor, commandablePilots);
			if (num4 >= 0)
			{
				ShowReply($"{s_wingman} {num4 + 1}: {s_attackingARAD}", 5f);
				commandablePilots[num4].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)tsdPage && tsdPage.tsc.GetCurrentSelectionActor() != null)
		{
			int num5 = AIWing.playerWing.AttackTarget(tsdPage.tsc.GetCurrentSelectionActor(), commandablePilots);
			if (num5 >= 0)
			{
				ShowReply($"{s_wingman} {num5 + 1}: {s_attackingTSD}", 5f);
				commandablePilots[num5].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)wm.lockingRadar && wm.lockingRadar.IsLocked())
		{
			int num6 = AIWing.playerWing.AttackTarget(wm.lockingRadar.currentLock.actor, commandablePilots);
			if (num6 >= 0)
			{
				ShowReply($"{s_wingman} {num6 + 1}: {s_attackingRadar}", 5f);
				commandablePilots[num6].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)wm.opticalTargeter && wm.opticalTargeter.locked && (bool)wm.opticalTargeter.lockedActor)
		{
			int num7 = AIWing.playerWing.AttackTarget(wm.opticalTargeter.lockedActor, commandablePilots);
			if (num7 >= 0)
			{
				ShowReply($"{s_wingman} {num7 + 1}: {s_attackingTGP}", 5f);
				commandablePilots[num7].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else if ((bool)aradPage && (bool)aradPage.selectedActor)
		{
			int num8 = AIWing.playerWing.AttackTarget(aradPage.selectedActor, commandablePilots);
			if (num8 >= 0)
			{
				ShowReply($"{s_wingman} {num8 + 1}: {s_attackingARAD}", 5f);
				commandablePilots[num8].PlayRadioMessage(WingmanVoiceProfile.Messages.CopyAttackOrder, 2f);
			}
			else
			{
				ShowReply(s_noWingmenForAttack, 5f);
				PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
			}
		}
		else
		{
			ShowReply(s_noTarget, 5f);
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	private void PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages m, float cooldown = 2f)
	{
		List<AIPilot> list = commandablePilots;
		list.RemoveAll((AIPilot x) => x == null);
		if (list.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			list[index].PlayRadioMessage(m, cooldown);
		}
	}

	public void SetCommsVolume(float t)
	{
		if ((bool)CommRadioManager.instance)
		{
			CommRadioManager.instance.SetCommsVolume(t);
		}
	}

	public void SetCommsVolumeMP(float t)
	{
		if ((bool)CommRadioManager.instance)
		{
			CommRadioManager.instance.SetCommsVolumeMP(t);
		}
	}

	public void SetCommsVolumeCopilot(float t)
	{
		if ((bool)CommRadioManager.instance)
		{
			CommRadioManager.instance.SetCommsVolumeCopilot(t);
		}
	}

	public void SetRecognition(int st)
	{
		if (st > 0)
		{
			StartRecognition();
		}
		else
		{
			StopRecognition();
		}
	}

	public void StartRecognition()
	{
		if (PhraseRecognitionSystem.isSupported && !isRecognitionEnabled)
		{
			isRecognitionEnabled = true;
			GetVM();
			if ((bool)FlightSceneManager.instance)
			{
				FlightSceneManager.instance.OnExitScene -= DisposeRecognizers;
				FlightSceneManager.instance.OnExitScene += DisposeRecognizers;
			}
			StartCoroutine(SetupRecognizersWhenReady());
		}
	}

	private IEnumerator SetupRecognizersWhenReady()
	{
		while (!FlightSceneManager.instance.playerActor)
		{
			yield return null;
		}
		if (wingmanRecognizer == null)
		{
			Debug.Log("Creating wingman keyword recognizer.");
			wingmanRecognizer = new KeywordRecognizer(VTVoiceRecognition.wingmanRecognitionPhrases);
			wingmanRecognizer.OnPhraseRecognized += WingmanRecognizer_OnRecognized;
		}
		if (!wingmanRecognizer.IsRunning)
		{
			wingmanRecognizer.Start();
		}
		if (atcRecognizer == null)
		{
			Debug.Log("Creating ATC keyword recognizer.");
			atcRecognizer = new KeywordRecognizer(VTVoiceRecognition.GetPlayerATCCommands());
			atcRecognizer.OnPhraseRecognized += AtcRecognizer_OnPhraseRecognized;
		}
		if (!atcRecognizer.IsRunning)
		{
			atcRecognizer.Start();
		}
		if (specifiedATCRecognizers.Count == 0)
		{
			CreateSpecifiedATCRecognizers();
		}
		foreach (KeywordRecognizer specifiedATCRecognizer in specifiedATCRecognizers)
		{
			if (!specifiedATCRecognizer.IsRunning)
			{
				specifiedATCRecognizer.Start();
			}
		}
		if (awacsRecognizer == null)
		{
			Debug.Log("Creating AWACS keyword recognizer.");
			awacsRecognizer = new KeywordRecognizer(VTVoiceRecognition.GetPlayerAWACSCommands());
			awacsRecognizer.OnPhraseRecognized += AwacsRecognizer_OnPhraseRecognized;
		}
		if (!awacsRecognizer.IsRunning)
		{
			awacsRecognizer.Start();
		}
	}

	private void CreateSpecifiedATCRecognizers()
	{
		if (createdSpecifiedATCRecognizers)
		{
			return;
		}
		createdSpecifiedATCRecognizers = true;
		VTVoiceRecognition.ClearSpecifiedATCCommands();
		List<string> list = new List<string>();
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			string text = airport.airportName.ToLower();
			if (!list.Contains(text))
			{
				list.Add(text);
				specifiedATCRecognizers.Add(CreateATCRecognizerForAirport(text));
			}
		}
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if (value.spawned && (bool)value.spawnedUnit && value.spawnedUnit is AICarrierSpawn && value.spawnedUnit.actor.alive)
			{
				string text2 = ((AICarrierSpawn)value.spawnedUnit).GetAirport().airportName.ToLower();
				if (!list.Contains(text2))
				{
					list.Add(text2);
					specifiedATCRecognizers.Add(CreateATCRecognizerForAirport(text2));
				}
			}
		}
	}

	private KeywordRecognizer CreateATCRecognizerForAirport(string apName)
	{
		KeywordRecognizer keywordRecognizer = new KeywordRecognizer(VTVoiceRecognition.GetPlayerATCCommands(apName));
		keywordRecognizer.OnPhraseRecognized += delegate(PhraseRecognizedEventArgs args)
		{
			LaunchATCCommand(args.text, apName);
		};
		return keywordRecognizer;
	}

	public void CallTheBall(Action<bool> onBallCalled, Action playerWavedOff)
	{
		awaitingBallCall = true;
		OnBallCalled = onBallCalled;
		PlayerWavedOff = playerWavedOff;
	}

	private void PlayerCallBall()
	{
		OnBallCalled?.Invoke(obj: true);
		awaitingBallCall = false;
	}

	private void PlayerCallClara()
	{
		OnBallCalled?.Invoke(obj: false);
		awaitingBallCall = false;
	}

	private void PlayerWavingOff()
	{
		PlayerWavedOff?.Invoke();
		PlayerWavedOff = null;
	}

	private void AwacsRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
	{
		if ((bool)AIAWACSSpawn.GetClosestAlliedAwacs() && VTVoiceRecognition.TryRecognizeAwacsCommand(args.text, out var output))
		{
			switch (output)
			{
			case VTVoiceRecognition.AWACSCommands.Picture:
				MFDAwacsPicture();
				break;
			case VTVoiceRecognition.AWACSCommands.BogeyDope:
				MFDAwacsBogeyDope();
				break;
			case VTVoiceRecognition.AWACSCommands.RTB:
				MFDAwacsRTB();
				break;
			case VTVoiceRecognition.AWACSCommands.Unrecognized:
				break;
			}
		}
	}

	private void AtcRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
	{
		LaunchATCCommand(args.text);
	}

	private void SetMFDContactAirport(AirportManager airport)
	{
		Debug.LogFormat(airport.gameObject, "Setting mfdSelectedAirport: {0}", airport.airportName);
		mfdSelectedAirport = airport;
	}

	private AirportManager GetClosestAirport()
	{
		if (!flightInfo)
		{
			Debug.LogError("MFDCommsPage has no FlightInfo reference!!");
			return null;
		}
		AirportManager result = null;
		float num = float.MaxValue;
		foreach (AirportManager airport in VTMapManager.fetch.airports)
		{
			if (airport.team == wm.actor.team)
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

	private AirportManager GetContactAirport()
	{
		Debug.Log("GetContactAirport: ");
		if ((bool)lastContactedAirport)
		{
			Debug.LogFormat(" - lastContactedAirport: " + lastContactedAirport.airportName);
			return lastContactedAirport;
		}
		if ((bool)mfdSelectedAirport)
		{
			Debug.Log(" - mfdSelectedAirport: " + mfdSelectedAirport.airportName);
			return mfdSelectedAirport;
		}
		if (vm.flightInfo.isLanded)
		{
			AirportManager landedOnAirport = GetLandedOnAirport();
			if ((bool)landedOnAirport)
			{
				Debug.Log(" - landedOnAirport: " + landedOnAirport.airportName);
				return landedOnAirport;
			}
		}
		AirportManager closestAirport = GetClosestAirport();
		if ((bool)closestAirport)
		{
			Debug.Log(" - closest airport: " + closestAirport.airportName);
			return closestAirport;
		}
		Debug.Log(" - no airport found.");
		return null;
	}

	private AirportManager GetLandedOnAirport()
	{
		RaySpringDamper[] suspensions = vm.flightInfo.wheelsController.suspensions;
		foreach (RaySpringDamper raySpringDamper in suspensions)
		{
			if (raySpringDamper.isTouching && (bool)raySpringDamper.touchingCollider)
			{
				AirportManager.AirbaseSurfaceCollider component = raySpringDamper.touchingCollider.GetComponent<AirportManager.AirbaseSurfaceCollider>();
				if ((bool)component && component.airport.team == wm.actor.team)
				{
					return component.airport;
				}
			}
		}
		return null;
	}

	private AirportManager GetAirport(string vName)
	{
		List<AirportManager> list = new List<AirportManager>();
		foreach (AirportManager airport2 in VTMapManager.fetch.airports)
		{
			if (airport2.team == wm.actor.team && airport2.airportName.ToLower() == vName)
			{
				list.Add(airport2);
			}
		}
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if (value.spawned && (bool)value.spawnedUnit && value.spawnedUnit is AICarrierSpawn && value.spawnedUnit.actor.alive)
			{
				AirportManager airport = ((AICarrierSpawn)value.spawnedUnit).GetAirport();
				if (airport.team == wm.actor.team && airport.airportName.ToLower() == vName)
				{
					list.Add(airport);
				}
			}
		}
		list.Sort(CompareDists);
		return list[0];
	}

	private void MFDRequestTakeoff()
	{
		AirportManager contactAirport = GetContactAirport();
		RequestTakeoff(contactAirport);
	}

	private void RequestTakeoff(AirportManager airport)
	{
		if (!airport)
		{
			ShowReply(s_noAirfield, 3f);
		}
		else if (airport.vtolOnlyTakeoff)
		{
			RequestVerticalTakeoff(airport);
		}
		else
		{
			if (airport == lastContactedAirport && Time.time - lastTakeoffRequestTime < 6f)
			{
				return;
			}
			lastTakeoffRequestTime = Time.time;
			GetEngines();
			if (FlightSceneManager.instance.playerActor.flightInfo.isLanded)
			{
				if ((bool)lastContactedAirport && lastContactedAirport.playerRequestStatus != 0 && (lastContactedAirport != airport || !lastContactedAirport.HasPlayerRequestedTakeOff()))
				{
					lastContactedAirport.CancelPlayerRequest();
				}
				lastContactedAirport = airport;
				airport.PlayerRequestTakeoff();
				wm.StartCoroutine(ATCRequestRoutine(airport, takeOff: true));
			}
		}
	}

	private IEnumerator ATCRequestRoutine(AirportManager ap, bool takeOff)
	{
		while (lastContactedAirport == ap && ap != null)
		{
			if (flightInfo.isLanded == !takeOff)
			{
				awaitingBallCall = false;
			}
			yield return null;
		}
		if (takeOff && !flightInfo.isLanded && (bool)FlightSceneManager.instance.playerActor.parkingNode)
		{
			Debug.Log("Player took off.  Unreserving parking space.");
			FlightSceneManager.instance.playerActor.parkingNode.UnOccupyParking(FlightSceneManager.instance.playerActor);
		}
		if (!takeOff && flightInfo.isLanded)
		{
			FlightSceneManager.instance.playerActor.SetAutoUnoccupyParking(b: true);
		}
	}

	private void RequestVerticalTakeoff()
	{
		RequestVerticalTakeoff(GetContactAirport());
	}

	private void RequestVerticalTakeoff(AirportManager airport)
	{
		if (!airport)
		{
			ShowReply(s_noAirfield, 3f);
		}
		else
		{
			if (airport == lastContactedAirport && Time.time - lastVTakeoffRequestTime < 6f)
			{
				return;
			}
			lastVTakeoffRequestTime = Time.time;
			GetEngines();
			if (FlightSceneManager.instance.playerActor.flightInfo.isLanded)
			{
				if ((bool)lastContactedAirport && lastContactedAirport.playerRequestStatus != 0 && (lastContactedAirport != airport || !lastContactedAirport.HasPlayerRequestedVerticalTakeOff()))
				{
					lastContactedAirport.CancelPlayerRequest();
				}
				lastContactedAirport = airport;
				if (airport.PlayerRequestVerticalTakeoff())
				{
					ShowReply(s_clearedVerticalTakeoff, 5f);
					wm.StartCoroutine(ATCRequestRoutine(airport, takeOff: true));
				}
				else
				{
					ShowReply(s_takeoffDenied, 5f);
				}
			}
		}
	}

	private void RequestVerticalLanding()
	{
		RequestVerticalLanding(GetContactAirport());
	}

	private void RequestVerticalLanding(AirportManager airport)
	{
		if (!airport)
		{
			ShowReply(s_noAirfield, 3f);
		}
		else
		{
			if (airport == lastContactedAirport && Time.time - lastVLandingRequestTime < 6f)
			{
				return;
			}
			lastVLandingRequestTime = Time.time;
			GetEngines();
			if (FlightSceneManager.instance.playerActor.flightInfo.isLanded)
			{
				if ((bool)airport.voiceProfile)
				{
					airport.voiceProfile.PlayUnableMsg();
				}
				return;
			}
			if ((bool)lastContactedAirport && lastContactedAirport.playerRequestStatus != 0 && (lastContactedAirport != airport || !lastContactedAirport.HasPlayerRequestedVerticalLanding()))
			{
				lastContactedAirport.CancelPlayerRequest();
			}
			lastContactedAirport = airport;
			if (airport.PlayerRequestVerticalLanding(out var _, out var pSpace) == AirportManager.PlayerLandingReponses.ClearedToLand)
			{
				lastContactedAirport = airport;
				ShowReply(s_clearedVerticalLanding, 5f);
				pSpace.OccupyParking(FlightSceneManager.instance.playerActor);
				FlightSceneManager.instance.playerActor.SetAutoUnoccupyParking(b: false);
				wm.StartCoroutine(ATCRequestRoutine(airport, takeOff: false));
			}
			else
			{
				ShowReply(s_landingDenied, 5f);
			}
		}
	}

	private void RequestLanding()
	{
		AirportManager contactAirport = GetContactAirport();
		RequestLanding(contactAirport);
	}

	private void RequestLanding(AirportManager airport)
	{
		if (!airport)
		{
			ShowReply(s_noAirfield, 3f);
		}
		else if (airport.vtolOnlyLanding)
		{
			RequestVerticalLanding(airport);
		}
		else if (airport.isCarrier && vm.isHelicopter)
		{
			RequestVerticalLanding(airport);
		}
		else
		{
			if (airport == lastContactedAirport && Time.time - lastLandingRequestTime < 6f)
			{
				return;
			}
			lastLandingRequestTime = Time.time;
			if ((bool)lastContactedAirport && (lastContactedAirport != airport || !lastContactedAirport.HasPlayerRequestedLanding()) && lastContactedAirport.playerRequestStatus != 0)
			{
				lastContactedAirport.CancelPlayerRequest();
			}
			GetEngines();
			if (FlightSceneManager.instance.playerActor.flightInfo.isLanded)
			{
				if ((bool)airport.voiceProfile)
				{
					airport.voiceProfile.PlayUnableMsg();
				}
				return;
			}
			lastContactedAirport = airport;
			switch (airport.PlayerRequestLanding(vm.isHelicopter))
			{
			case AirportManager.PlayerLandingReponses.ClearedToLand:
				wm.StartCoroutine(ATCRequestRoutine(airport, takeOff: false));
				break;
			case AirportManager.PlayerLandingReponses.AlreadyRequested:
				Debug.Log("Requested landing from airbase but we already requested it before.");
				break;
			default:
				ShowReply(s_landingDenied, 5f);
				break;
			}
		}
	}

	private void CancelATCRequest()
	{
		CancelATCRequest(playVoice: true);
	}

	private void CancelATCRequest(bool playVoice)
	{
		if (lastContactedAirport != null)
		{
			if (lastContactedAirport.playerRequestStatus != 0)
			{
				ShowReply(s_cancellingATC, 3f);
				if (playVoice && (bool)lastContactedAirport.voiceProfile)
				{
					lastContactedAirport.voiceProfile.PlayCancelledRequestMsg();
				}
			}
			lastContactedAirport.CancelPlayerRequest();
		}
		awaitingBallCall = false;
	}

	private void LaunchATCCommand(string commandString, string specifiedAPName = null)
	{
		GetEngines();
		if (!(string.IsNullOrEmpty(specifiedAPName) ? VTVoiceRecognition.TryRecognizeATCCommand(commandString, out var output) : VTVoiceRecognition.TryRecognizeSpecifiedATCCommand(commandString, out output)))
		{
			return;
		}
		switch (output)
		{
		case VTVoiceRecognition.ATCCommands.RequestRearm:
			RequestRearming();
			return;
		case VTVoiceRecognition.ATCCommands.Unrecognized:
			return;
		}
		Debug.Log("Recognized ATC voice command: " + output);
		if (lastContactedAirport != null && output == VTVoiceRecognition.ATCCommands.CancelRequest)
		{
			CancelATCRequest();
			return;
		}
		AirportManager airportManager = null;
		if (!string.IsNullOrEmpty(specifiedAPName))
		{
			airportManager = GetAirport(specifiedAPName);
		}
		else
		{
			if (output == VTVoiceRecognition.ATCCommands.RequestingTakeoff || output == VTVoiceRecognition.ATCCommands.RequestingVerticalTakeoff)
			{
				airportManager = GetLandedOnAirport();
			}
			if (!airportManager)
			{
				airportManager = GetContactAirport();
			}
		}
		if (airportManager != null)
		{
			switch (output)
			{
			case VTVoiceRecognition.ATCCommands.RequestingLanding:
				RequestLanding(airportManager);
				break;
			case VTVoiceRecognition.ATCCommands.RequestingTakeoff:
				RequestTakeoff(airportManager);
				break;
			case VTVoiceRecognition.ATCCommands.RequestingVerticalTakeoff:
				RequestVerticalTakeoff(airportManager);
				break;
			case VTVoiceRecognition.ATCCommands.RequestingVerticalLanding:
				RequestVerticalLanding(airportManager);
				break;
			case VTVoiceRecognition.ATCCommands.Meatball:
				PlayerCallBall();
				break;
			case VTVoiceRecognition.ATCCommands.ClaraBall:
				PlayerCallClara();
				break;
			case VTVoiceRecognition.ATCCommands.WavingOff:
				PlayerWavingOff();
				break;
			case VTVoiceRecognition.ATCCommands.CancelRequest:
			case VTVoiceRecognition.ATCCommands.RequestRearm:
				break;
			}
		}
		else
		{
			Debug.Log("No airbase designated for voice command.");
		}
	}

	public void StopRecognition()
	{
		if (!isRecognitionEnabled)
		{
			return;
		}
		isRecognitionEnabled = false;
		if (wingmanRecognizer != null)
		{
			wingmanRecognizer.Stop();
		}
		if (atcRecognizer != null)
		{
			atcRecognizer.Stop();
		}
		if (awacsRecognizer != null)
		{
			awacsRecognizer.Stop();
		}
		foreach (KeywordRecognizer specifiedATCRecognizer in specifiedATCRecognizers)
		{
			specifiedATCRecognizer.Stop();
		}
	}

	private void OnDestroy()
	{
		DisposeRecognizers();
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= DisposeRecognizers;
		}
	}

	private void DisposeRecognizers()
	{
		Debug.Log("Disposing keyword recognizers.");
		if (wingmanRecognizer != null)
		{
			if (wingmanRecognizer.IsRunning)
			{
				wingmanRecognizer.Stop();
			}
			wingmanRecognizer.Dispose();
			wingmanRecognizer = null;
		}
		if (atcRecognizer != null)
		{
			if (atcRecognizer.IsRunning)
			{
				atcRecognizer.Stop();
			}
			atcRecognizer.Dispose();
			atcRecognizer = null;
		}
		if (awacsRecognizer != null)
		{
			if (awacsRecognizer.IsRunning)
			{
				awacsRecognizer.Stop();
			}
			awacsRecognizer.Dispose();
			awacsRecognizer = null;
		}
		foreach (KeywordRecognizer specifiedATCRecognizer in specifiedATCRecognizers)
		{
			if (specifiedATCRecognizer.IsRunning)
			{
				specifiedATCRecognizer.Stop();
			}
			specifiedATCRecognizer.Dispose();
		}
		specifiedATCRecognizers.Clear();
	}

	private void WingmanRecognizer_OnRecognized(PhraseRecognizedEventArgs args)
	{
		try
		{
			LaunchRecognizedWingmanCommand(args.text);
		}
		catch (Exception ex)
		{
			Debug.LogError("Exception when trying to launch recognized wingman command: " + ex);
		}
	}

	private void LaunchRecognizedWingmanCommand(string commandString)
	{
		if (VTVoiceRecognition.TryRecognizeWingmanCommand(commandString, out var output))
		{
			switch (output)
			{
			case VTVoiceRecognition.WingmanCommands.AttackMyTarget:
				AttackMyTarget();
				break;
			case VTVoiceRecognition.WingmanCommands.FormUp:
				FormOnMe();
				break;
			case VTVoiceRecognition.WingmanCommands.SpreadFar:
				FormationFar();
				break;
			case VTVoiceRecognition.WingmanCommands.Orbit:
				OrbitHere();
				break;
			case VTVoiceRecognition.WingmanCommands.EngageTargets:
				EngageEnemies();
				break;
			case VTVoiceRecognition.WingmanCommands.Disengage:
				DisengageEnemies();
				break;
			case VTVoiceRecognition.WingmanCommands.SpreadClose:
				FormationClose();
				break;
			case VTVoiceRecognition.WingmanCommands.SpreadMedium:
				FormationMed();
				break;
			case VTVoiceRecognition.WingmanCommands.DisengageAndFormUp:
				DisengageEnemies();
				FormOnMe();
				break;
			case VTVoiceRecognition.WingmanCommands.GoRefuel:
				CommandGoRefuel();
				break;
			case VTVoiceRecognition.WingmanCommands.ReturnToBase:
				CommandRTB(rearm: false);
				break;
			case VTVoiceRecognition.WingmanCommands.RearmAtBase:
				CommandRTB(rearm: true);
				break;
			case VTVoiceRecognition.WingmanCommands.Fox:
				CommandFox();
				break;
			case VTVoiceRecognition.WingmanCommands.RadarOn:
				WingmenRadarOn();
				break;
			case VTVoiceRecognition.WingmanCommands.RadarOff:
				WingmenRadarOff();
				break;
			case VTVoiceRecognition.WingmanCommands.Unrecognized:
				break;
			}
		}
	}

	private void RefreshCommandablePilots()
	{
		commandablePilots.Clear();
		foreach (UnitSpawner value in VTScenario.current.units.alliedUnits.Values)
		{
			if ((bool)value && value.spawned && (bool)value.spawnedUnit && value.spawnedUnit is AIAircraftSpawn)
			{
				AIAircraftSpawn aIAircraftSpawn = (AIAircraftSpawn)value.spawnedUnit;
				if (aIAircraftSpawn.actor.alive && (bool)aIAircraftSpawn.aiPilot && aIAircraftSpawn.aiPilot.allowPlayerCommands)
				{
					commandablePilots.Add(aIAircraftSpawn.aiPilot);
				}
			}
		}
	}

	private void CommandRearm()
	{
		CommandRTB(rearm: true);
	}

	private void CommandFox()
	{
		if (PlayerHasWingmen())
		{
			if (foxWaitRoutine != null)
			{
				StopCoroutine(foxWaitRoutine);
			}
			foxWaitRoutine = wm.StartCoroutine(FoxWaitForMissileRoutine(FlightSceneManager.instance.playerActor.weaponManager));
			ShowReply(s_acknowledged, 4f);
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Copy);
		}
	}

	private IEnumerator FoxWaitForMissileRoutine(WeaponManager playerWm)
	{
		float waitTime = 5f;
		float t = Time.time;
		while (!playerWm.lastFiredMissile || Time.time - playerWm.lastFiredMissile.timeFired > waitTime)
		{
			if (Time.time - t > waitTime)
			{
				yield break;
			}
			yield return null;
		}
		if (!playerWm.lastFiredMissile || !(Time.time - playerWm.lastFiredMissile.timeFired < waitTime))
		{
			yield break;
		}
		Missile lastFiredMissile = playerWm.lastFiredMissile;
		if (!lastFiredMissile || !lastFiredMissile.hasTarget)
		{
			yield break;
		}
		Actor actor = null;
		switch (lastFiredMissile.guidanceMode)
		{
		case Missile.GuidanceModes.Radar:
			if (lastFiredMissile.radarLock != null && lastFiredMissile.radarLock.locked)
			{
				actor = lastFiredMissile.radarLock.actor;
			}
			else if (lastFiredMissile.twsData != null)
			{
				actor = lastFiredMissile.twsData.actor;
			}
			break;
		case Missile.GuidanceModes.Heat:
			actor = lastFiredMissile.heatSeeker.likelyTargetActor;
			break;
		case Missile.GuidanceModes.Optical:
			actor = lastFiredMissile.opticalTargetActor;
			break;
		case Missile.GuidanceModes.Bomb:
			if ((bool)playerWm.opticalTargeter)
			{
				actor = playerWm.opticalTargeter.lockedActor;
			}
			break;
		case Missile.GuidanceModes.AntiRad:
			actor = lastFiredMissile.antiRadTargetActor;
			break;
		}
		if (actor != null)
		{
			AIWing.playerWing.ReportMissileOnTarget(FlightSceneManager.instance.playerActor, actor, lastFiredMissile);
		}
	}

	private void CommandGoRefuel()
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			return;
		}
		bool flag = false;
		AIPilot aIPilot = null;
		for (int i = 0; i < commandablePilots.Count; i++)
		{
			AIPilot aIPilot2 = commandablePilots[i];
			if (aIPilot2.CommandGoRefuel())
			{
				flag = true;
				if (!aIPilot || UnityEngine.Random.Range(0f, 100f) > 50f)
				{
					aIPilot = aIPilot2;
				}
			}
		}
		if (flag && (bool)aIPilot)
		{
			aIPilot.PlayRadioMessage(WingmanVoiceProfile.Messages.Copy, 2f);
		}
		else
		{
			PlayMessageFromRandomWingman(WingmanVoiceProfile.Messages.Deny);
		}
	}

	private void CommandRTB()
	{
		CommandRTB(rearm: false);
	}

	private void CommandRTB(bool rearm)
	{
		RefreshCommandablePilots();
		if (!CheckIfWingmenCommandable())
		{
			Debug.Log("No wingmen commandable for RTB");
			return;
		}
		bool flag = true;
		bool flag2 = false;
		AIPilot aIPilot = null;
		AIPilot aIPilot2 = null;
		foreach (AIPilot commandablePilot in commandablePilots)
		{
			if ((bool)commandablePilot && !commandablePilot.autoPilot.flightInfo.isLanded && commandablePilot.actor.alive && (commandablePilot.commandState == AIPilot.CommandStates.FollowLeader || commandablePilot.commandState == AIPilot.CommandStates.Navigation || commandablePilot.commandState == AIPilot.CommandStates.Orbit))
			{
				AIAircraftSpawn aiSpawn = commandablePilot.aiSpawn;
				bool flag3 = true;
				if ((bool)aiSpawn && (flag3 = aiSpawn.CommandRTB()))
				{
					commandablePilot.SetRearmAfterLanding(rearm);
					flag2 = true;
					if (!aIPilot || UnityEngine.Random.Range(0, 100) > 50)
					{
						aIPilot = commandablePilot;
					}
				}
				else
				{
					aIPilot2 = commandablePilot;
					if ((bool)aiSpawn && !flag3)
					{
						Debug.LogFormat("{0} can't RTB...", commandablePilot.actor.DebugName());
					}
				}
			}
			else
			{
				flag = false;
				if ((bool)commandablePilot)
				{
					aIPilot2 = commandablePilot;
				}
			}
		}
		if (flag)
		{
			ShowReply(s_wingmenRTB, 4f);
			if ((bool)aIPilot)
			{
				aIPilot.PlayRadioMessage(WingmanVoiceProfile.Messages.Copy, 3f);
			}
			return;
		}
		if (flag2)
		{
			if ((bool)aIPilot)
			{
				aIPilot.PlayRadioMessage(WingmanVoiceProfile.Messages.Copy, 3f);
			}
			ShowReply(s_someWingmenBusy, 3f);
		}
		else
		{
			ShowReply(s_wingmenBusy, 3f);
		}
		if ((bool)aIPilot2)
		{
			aIPilot2.PlayRadioMessage(WingmanVoiceProfile.Messages.Deny, 3f);
		}
	}

	public void ApplyLocalization()
	{
		Func<string, string, string> func = (string s, string keyN) => VTLocalizationManager.GetString("commReply_" + keyN, s, "A text reply in the communications menu.");
		s_comm_reply = func(s_comm_reply, "s_comm_reply");
		s_noHostilesFound = func(s_noHostilesFound, "s_noHostilesFound");
		s_awacsNotReady = func(s_awacsNotReady, "s_awacsNotReady");
		s_noAwacsAvailable = func(s_noAwacsAvailable, "s_noAwacsAvailable");
		s_awacsPicRequested = func(s_awacsPicRequested, "s_awacsPicRequested");
		s_awacsRequestedRTB = func(s_awacsRequestedRTB, "s_awacsRequestedRTB");
		s_hostile = VTLocalizationManager.GetString("s_hostile", s_hostile, "Prefix for AWACS bogey dope response");
		s_rearmingNotAvailable = func(s_rearmingNotAvailable, "s_rearmingNotAvailable");
		s_notAtRearmingStation = func(s_notAtRearmingStation, "s_notAtRearmingStation");
		s_turnOffEngines = func(s_turnOffEngines, "s_turnOffEngines");
		s_disarmWeapons = func(s_disarmWeapons, "s_disarmWeapons");
		s_unhookTailhook = func(s_unhookTailhook, "s_unhookTailhook");
		s_rearmPointOccupied = func(s_rearmPointOccupied, "s_rearmPointOccupied");
		s_taxiToRearm = func(s_taxiToRearm, "s_taxiToRearm");
		s_wingmenBusy = func(s_wingmenBusy, "s_wingmenBusy");
		s_someWingmenBusy = func(s_someWingmenBusy, "s_someWingmenBusy");
		s_noWingmen = func(s_noWingmen, "s_noWingmen");
		s_wingmenOrbiting = func(s_wingmenOrbiting, "s_wingmenOrbiting");
		s_wingmenEngaging = func(s_wingmenEngaging, "s_wingmenEngaging");
		s_wingmenDisengaging = func(s_wingmenDisengaging, "s_wingmenDisengaging");
		s_wingmenFormationSpread = func(s_wingmenFormationSpread, "s_wingmenFormationSpread");
		s_wingman = func(s_wingman, "s_wingman");
		s_attackingTSD = func(s_attackingTSD, "s_attackingTSD");
		s_attackingRadar = func(s_attackingRadar, "s_attackingRadar");
		s_attackingTGP = func(s_attackingTGP, "s_attackingTGP");
		s_attackingARAD = func(s_attackingARAD, "s_attackingARAD");
		s_noWingmenForAttack = func(s_noWingmenForAttack, "s_noWingmenForAttack");
		s_noTarget = func(s_noTarget, "s_noTarget");
		s_noAirfield = func(s_noAirfield, "s_noAirfield");
		s_cancellingATC = func(s_cancellingATC, "s_cancellingATC");
		s_acknowledged = func(s_acknowledged, "s_acknowledged");
		s_wingmenRTB = func(s_wingmenRTB, "s_wingmenRTB");
		s_clearedVerticalLanding = func(s_clearedVerticalLanding, "s_clearedVerticalLanding");
		s_clearedVerticalTakeoff = func(s_clearedVerticalTakeoff, "s_clearedVerticalTakeoff");
		s_landingDenied = func(s_landingDenied, "s_landingDenied");
		s_takeoffDenied = func(s_takeoffDenied, "s_takeoffDenied");
		s_reply_noPrevAtc = func(s_reply_noPrevAtc, "s_reply_noPrevAtc");
		ApplyOptionBrowserLocalization();
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("MFDCommsPage");
		if (lastContactedAirport != null)
		{
			configNode.AddNode(AirportManager.SaveAirportReferenceToNode("lastContactedAirport", lastContactedAirport));
		}
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("MFDCommsPage");
		if (node != null)
		{
			ConfigNode node2 = node.GetNode("lastContactedAirport");
			if (node2 != null)
			{
				lastContactedAirport = AirportManager.RetrieveAirportReferenceFromNode(node2);
			}
		}
	}
}
