using System;
using UnityEngine;
using VTOLVR.Multiplayer;

public class HPEquipGunTurret : HPEquipGun
{
	public enum TurretStates
	{
		Locked,
		Gimbal,
		SpeedLimit,
		Disabled
	}

	public ModuleTurret turret;

	public Transform stowedTarget;

	public Transform lockedTarget;

	public float maxSlavedAirspeed = 200f;

	public float maxHeadtrackDist = 8000f;

	[Tooltip("Default setting for slaved mode")]
	public bool turretLocked = true;

	private TargetingMFDPage targetingPage;

	private FlightInfo flightInfo;

	private TurretStates turretState;

	public bool allowHeadtrackNoTGP;

	private MultiUserVehicleSync muvs;

	public bool allowRadarTrack = true;

	private MFDRadarUI radarUI;

	private string s_locked = "LOCKED";

	private string s_slaved = "SLAVED";

	private string s_gimbal = "GIMBAL";

	private string s_speedlock = "SPEEDLOCK";

	private bool mpRemote;

	public bool isTurretLocked => turretLocked;

	
}
