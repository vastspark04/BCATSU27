using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using VTOLVR.Multiplayer;

public class MFDPTacticalSituationDisplay : MFDPortalPage
{
	[Header("TSD")]
	public TacticalSituationController tsc;

	public MeasurementManager measurements;

	public GameObject contactTemplate;

	public Transform viewTransform;

	public Transform referenceTf;

	public Transform myIconTf;

	public Transform compassTf;

	public VRTouchScreenInteractable dragInteractable;

	public Transform dragTransform;

	public GameObject resetViewButtonObj;

	[Header("TSD Cursor")]
	public Transform selectorTf;

	public float cursorSpeed;

	public float cursorSnapRadius;

	[Header("Target Selection")]
	public Transform selectedTargetIconTf;

	public Transform activeTargetIconTf;

	private TSDContactIcon currSelectedIcon;

	private List<TSDContactIcon> tscContactIcons = new List<TSDContactIcon>();

	[Header("Radar")]
	public UILineRenderer radarLockLr;

	public Transform radarLockIconTf;

	public MFDRadarUI radarUI;

	public Transform radarParentTf;

	public RectTransform radarRightEdgeTf;

	public RectTransform radarLeftEdgeTf;

	public UICircle radarEdgeCircle;

	public GameObject radarSettingsDisplayObj;

	public GameObject radarPointLockIndicator;

	[Header("HUD")]
	public HUDTacticalSituationSymbols hud;

	public GameObject hudSettingsDisplayObj;

	public GameObject hudSettingsToggleButton;

	public Color hudSettingActiveColor;

	public Image hud_alliedImg;

	public Image hud_enemyImg;

	public Image hud_groundImg;

	public Image hud_airImg;

	public Image hud_missileImg;

	private Color hudInactiveColor;

	[Header("TGP")]
	public GameObject slewTGPButtonObj;

	public GameObject gpsSendButtonObj;

	public TargetingMFDPage tgpUI;

	[Header("Target Info")]
	public GameObject tgtInfoDisplayObj;

	public Text braNumsText;

	public GameObject mhLabelsObj;

	public Text mhNumsText;

	public Text tgtTypeText;

	public GameObject playerNameBox;

	public UIMaskedTextScroller playerNameMask;

	[Header("HUD")]
	public HUDWeaponInfo hudInfo;

	[Header("GPS")]
	public GameObject gpsTargetTemplate;

	public Transform gpsSelectedIcon;

	public UILineRenderer gpsPathLine;

	public GameObject gpsInfoBox;

	public Text gpsBRText;

	public Text gpsCoordsText;

	[Header("TGP")]
	public Transform tgpIconTf;

	[Header("Bullseye")]
	public Transform bullseyeTf;
    [Header("Datalink")]
    public GameObject buddyTargetTemplate;

    // Token: 0x04000B24 RID: 2852
    public GameObject enemyRadarLockTemplate;
    [Header("Display")]
	public float viewPersistanceTime = 30f;

	public float preDisappearFlashTime = 5f;

	private int viewScaleIdx;

	public float baseRadius = 50f;

	public float[] meterViewScales;

	public float[] mileViewScales;

	public float[] nautMileViewScales;

	public float[] feetViewScales;

	public Text viewScaleText;

	private Vector3 dragCenteredPos;

	private Vector3 lastTouchLP;

	private bool hasInit;

	private TSDContactIcon snapIcon;

	private bool updating;

	private Coroutine updateRoutine;

	private Vector2[] radarLockLrPoints = new Vector2[2]
	{
		Vector2.zero,
		Vector2.zero
	};

	private Vector3 worldViewOffsetPos;

	private Actor lastInfoActor;

	private float slewToTargetExpectedPositionAngleThreshold = 1f;

	private Vector2[] linePoints = new Vector2[2];

	private bool r_ptLock;

	private float cursorSnapSqrRad => cursorSnapRadius * cursorSnapRadius;
public void ToggleRadarPointLock()
{
	
}
	public void ToggleHUDAlly()
{
	
}	public void ToggleHUDEnemy()
{
	
}
public void ToggleHUDGround()
{
	
}

public void ToggleHUDMissile()
{
	
}

public void ToggleHUDSettingsDisplay()
{
	
}

public void RecenterView()
{
	
}
public void ToggleHUDAir()
{
	
}
	public void ToggleRadarSettingsDisplay()
{
	
}
	public void GPSSend()
{
	
}
	public void SlewTGP()
{
	
}

	public void PrevViewScale()
{
	
}
	public void NextViewScale()
{
	
}
}
