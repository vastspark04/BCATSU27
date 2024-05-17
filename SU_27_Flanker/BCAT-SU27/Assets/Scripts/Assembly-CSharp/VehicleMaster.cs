using System;
using UnityEngine;
using VTOLVR.DLC.Rotorcraft;

public class VehicleMaster : MonoBehaviour, IQSVehicleComponent, IPersistentVehicleData
{
	public PlayerVehicle playerVehicle;

	[Header("Engines")]
	public ModuleEngine[] engines;

	[Header("Fuel Tanks")]
	public FuelTank[] fuelTanks;

	[Header("HUD")]
	public HUDMessages hudMessages;

	public HUDWeaponInfo hudWeaponInfo;

	[Header("Wing Folding (Optional)")]
	public VRLever wingFoldSwitch;

	public RotationToggle wingFolder;

	[Header("Launch bar")]
	public VRLever launchBarSwitch;

	[Header("Chopper")]
	public HelicopterRotor mainRotor;

	public HeliPowerGovernor powerGovernor;

	private MeasurementManager _meMan;

	public bool isVTOLCapable;

	public bool isHelicopter;

	private CamRigRotationInterpolator camShaker;

	private float enabledTime;

	private bool _useRadarAlt;

	private float lastRearmStationTime;

	private int rp_framesLanded;

	private float _normBingoLevel = 0.1f;

	public FlightInfo flightInfo { get; private set; }

	public ReArmingPoint currentRearmingPoint { get; private set; }

	public Actor actor { get; private set; }

	public FlightWarnings flightWarnings { get; private set; }

	public TiltController tiltController { get; private set; }

	public MFDCommsPage comms { get; private set; }

	public MeasurementManager measurementManager
	{
		get
		{
			if (!_meMan)
			{
				_meMan = GetComponent<MeasurementManager>();
			}
			return _meMan;
		}
	}

	public bool pilotIsDead { get; private set; }

	public bool useRadarAlt
	{
		get
		{
			return _useRadarAlt;
		}
		set
		{
			_useRadarAlt = value;
			if (this.OnSetRadarAltMode != null)
			{
				this.OnSetRadarAltMode(_useRadarAlt);
			}
		}
	}

	public float normBingoLevel
	{
		get
		{
			return _normBingoLevel;
		}
		set
		{
			_normBingoLevel = Mathf.Clamp01(value);
			if (this.OnSetNormBingoFuel != null)
			{
				this.OnSetNormBingoFuel(_normBingoLevel);
			}
		}
	}

	public event Action OnPilotDied;

	public event Action<bool> OnSetRadarAltMode;

	public event Action<float> OnSetNormBingoFuel;

	public void KillPilot()
	{
		if (!pilotIsDead)
		{
			pilotIsDead = true;
			Health component = GetComponent<Health>();
			if ((bool)component)
			{
				component.Kill();
			}
			TempPilotDetacher[] componentsInChildren = GetComponentsInChildren<TempPilotDetacher>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].DetachPilot();
			}
			this.OnPilotDied?.Invoke();
		}
	}

	private void OnEnable()
	{
		enabledTime = Time.time;
	}

	public void ToggleRadarAltMode()
	{
		useRadarAlt = !useRadarAlt;
	}

	public void SetRadarAltMode(int mode)
	{
		useRadarAlt = mode > 0;
	}

	private void Awake()
	{
		actor = GetComponent<Actor>();
		camShaker = GetComponentInChildren<CamRigRotationInterpolator>();
		flightInfo = GetComponent<FlightInfo>();
		flightWarnings = GetComponent<FlightWarnings>();
		tiltController = GetComponentInChildren<TiltController>(includeInactive: true);
		comms = GetComponentInChildren<MFDCommsPage>(includeInactive: true);
	}

	public void SetWingFoldImmediate(bool folded)
	{
		if ((bool)wingFoldSwitch && (bool)wingFolder)
		{
			wingFoldSwitch.RemoteSetState(folded ? 1 : 0);
			wingFolder.SetNormalizedRotationImmediate(folded ? 1 : 0);
		}
	}

	private void Update()
	{
		if ((bool)camShaker)
		{
			float num = Mathf.Max(0f, 0.0325f * (Mathf.Abs(flightInfo.playerGs) - 3f));
			CamRigRotationInterpolator.ShakeAll(UnityEngine.Random.onUnitSphere * num);
			if (FlybyCameraMFDPage.instance.finalBehavior == FlybyCameraMFDPage.SpectatorBehaviors.Fixed || FlybyCameraMFDPage.instance.finalBehavior == FlybyCameraMFDPage.SpectatorBehaviors.Camcorder)
			{
				FlybyCameraMFDPage.ShakeSpectatorCamera(4f * num);
			}
		}
		UpdateRearmingPoint();
	}

	private void UpdateRearmingPoint()
	{
		if (PilotSaveManager.currentScenario == null || !PilotSaveManager.currentScenario.equipConfigurable)
		{
			return;
		}
		if (flightInfo.isLanded && flightInfo.surfaceSpeed < 5f)
		{
			if (rp_framesLanded < 0)
			{
				rp_framesLanded = 0;
			}
			if (rp_framesLanded < 3)
			{
				rp_framesLanded++;
				return;
			}
			bool flag = currentRearmingPoint != null;
			if (flag && (currentRearmingPoint.team != actor.team || (currentRearmingPoint.transform.position - actor.position).sqrMagnitude > currentRearmingPoint.radius * currentRearmingPoint.radius))
			{
				currentRearmingPoint = null;
			}
			if (!currentRearmingPoint)
			{
				if ((bool)ReArmingPoint.active)
				{
					return;
				}
				float num = float.MaxValue;
				foreach (ReArmingPoint reArmingPoint in ReArmingPoint.reArmingPoints)
				{
					if ((bool)reArmingPoint && reArmingPoint.team == actor.team && reArmingPoint.CheckIsClear(actor))
					{
						float sqrMagnitude = (reArmingPoint.transform.position - actor.position).sqrMagnitude;
						if (sqrMagnitude < num && sqrMagnitude < reArmingPoint.radius * reArmingPoint.radius)
						{
							num = sqrMagnitude;
							currentRearmingPoint = reArmingPoint;
						}
					}
				}
				if ((bool)currentRearmingPoint && !flag && Time.time - enabledTime > 5f && Time.time - lastRearmStationTime > 15f)
				{
					currentRearmingPoint.voiceProfile.PlayMessage(GroundCrewVoiceProfile.GroundCrewMessages.EnteredStation);
				}
			}
			else
			{
				lastRearmStationTime = Time.time;
			}
		}
		else
		{
			if (rp_framesLanded > 0)
			{
				rp_framesLanded = 0;
			}
			if (rp_framesLanded > -3)
			{
				rp_framesLanded--;
			}
			else
			{
				currentRearmingPoint = null;
			}
		}
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = qsNode.AddNode("VehicleMaster");
		configNode.SetValue("useRadarAlt", useRadarAlt);
		configNode.SetValue("normBingoLevel", normBingoLevel);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		ConfigNode node = qsNode.GetNode("VehicleMaster");
		if (node != null)
		{
			useRadarAlt = node.GetValue<bool>("useRadarAlt");
			if (node.HasValue("normBingoLevel"))
			{
				normBingoLevel = node.GetValue<float>("normBingoLevel");
			}
		}
	}

	public void OnSaveVehicleData(ConfigNode vDataNode)
	{
		ConfigNode configNode = vDataNode.AddOrGetNode("VehicleMaster");
		configNode.SetValue("useRadarAlt", useRadarAlt);
		configNode.SetValue("normBingoLevel", normBingoLevel);
	}

	public void OnLoadVehicleData(ConfigNode vDataNode)
	{
		ConfigNode node = vDataNode.GetNode("VehicleMaster");
		if (node != null)
		{
			useRadarAlt = node.GetValue<bool>("useRadarAlt");
			if (node.HasValue("normBingoLevel"))
			{
				normBingoLevel = node.GetValue<float>("normBingoLevel");
			}
		}
	}
}
