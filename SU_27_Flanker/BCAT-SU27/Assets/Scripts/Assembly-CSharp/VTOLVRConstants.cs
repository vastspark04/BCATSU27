using UnityEngine;

[CreateAssetMenu]
public class VTOLVRConstants : ScriptableObject
{
	private const string resourcePath = "VTOLVRConstants";

	private static VTOLVRConstants _instance;

	private static bool fetched;

	[Header("Prices")]
	public float fuelUnitCost = 1.5f;

	public float repairUnitCost = 50f;

	[Header("Warnings")]
	public float pullUpMinAlt = 5f;

	[Header("Heat")]
	public float missileAirspeedHeatMult = 0.2f;

	public float missileThrustHeatMult = 25f;

	public float missileCooldownSqrSpeedDiv = 750f;

	public float globalCooldownFactor = 1f;

	[Header("Editors")]
	public float doubleClickTime = 0.35f;

	[Header("AI")]
	public float airbaseNavQueueDeccelDiv = 30f;

	public float airbaseNavQueueSpacing = 6f;

	[Header("Sound")]
	public float commBGMDuckerAtten = -30f;

	[Header("Physics")]
	public float brakeAnchorPFactor = 2.5f;

	public float brakeAnchorDFactor = 0.5f;

	[Header("Graphics")]
	public float unitIconNVGMultiplier = 0.15f;

	private static VTOLVRConstants fetch
	{
		get
		{
			if (!fetched)
			{
				fetched = true;
				_instance = Resources.Load<VTOLVRConstants>("VTOLVRConstants");
			}
			return _instance;
		}
	}

	public static float FUEL_UNIT_COST => fetch.fuelUnitCost;

	public static float REPAIR_UNIT_COST => fetch.repairUnitCost;

	public static float WARN_PULL_UP_MIN_ALT => fetch.pullUpMinAlt;

	public static float MISSILE_AIRSPEED_HEAT_MULT => fetch.missileAirspeedHeatMult;

	public static float MISSILE_THRUST_HEAT_MULT => fetch.missileThrustHeatMult;

	public static float MISSILE_COOLDOWN_SQRSPEED_DIV => fetch.missileCooldownSqrSpeedDiv;

	public static float GLOBAL_COOLDOWN_FACTOR => fetch.globalCooldownFactor;

	public static float DOUBLE_CLICK_TIME => fetch.doubleClickTime;

	public static float AIRBASE_NAV_DECCEL_DIV => fetch.airbaseNavQueueDeccelDiv;

	public static float AIRBASE_NAV_SPACING => fetch.airbaseNavQueueSpacing;

	public static float COMM_BGM_DUCKER_ATTEN => fetch.commBGMDuckerAtten;

	public static float PHYS_BRAKE_ANCHOR_P_FACTOR => fetch.brakeAnchorPFactor;

	public static float PHYS_BRAKE_ANCHOR_D_FACTOR => fetch.brakeAnchorDFactor;

	public static float UNITICON_NVG_MUL => fetch.unitIconNVGMultiplier;
}
