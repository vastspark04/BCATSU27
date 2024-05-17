using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using VTOLVR.Multiplayer;

public class MFDRadarUI : MonoBehaviour, IQSVehicleComponent
{
	public delegate void SetPlayerRadarDelegate(Radar r, LockingRadar lr);

	public class UIRadarContact
	{
		public Actor actor;

		public int actorID;

		public float timeFound;

		public FixedPoint detectedPosition;

		public Vector3 detectedVelocity;

		public GameObject iconObject;

		public Teams team;

		public bool active = true;

		public ConfigNode SaveToConfigNode(string nodeName)
		{
			ConfigNode configNode = new ConfigNode(nodeName);
			QuicksaveManager.SaveActorIdentifier(actor, out var id, out var globalPos, out var subUnitID);
			configNode.SetValue("unitID", id);
			configNode.SetValue("globalPos", globalPos);
			configNode.SetValue("subUnitID", subUnitID);
			configNode.SetValue("timeFoundElapsed", Time.time - timeFound);
			configNode.SetValue("detectedVelocity", detectedVelocity);
			configNode.SetValue("active", active);
			configNode.SetValue("actorID", actorID);
			return configNode;
		}
	}

	public MFDPage mfdPage;

	public MFDPortalPage portalPage;

	public Battery battery;

	public Radar playerRadar;

	public LockingRadar lockingRadar;

	public AdvancedRadarController radarCtrlr;

	public MeasurementManager measurements;

	public GameObject hudBoresightObj;

	public WeaponManager wm;

	public bool doMultiTrack = true;

	public GameObject noRadarObj;

	public GameObject radarAvailableDisplayObj;

	public RawImage groundRadarImage;

	public GroundRadarDispatcher grDispatcher;

	public bool canToggleAG;

	public Text agModeText;

	private bool agMode = true;

	public ErrorFlasher errorFlasher;

	private bool isMulticrew;

	public bool controlsRadar = true;

	public bool defaultRadarOff = true;

	public float refreshInterval = 0.1f;

	public float boresightFOV;

	public float boresightRange;

	private bool boresightMode;

	public GameObject boresightDisplayObj;

	public Transform radarDisplayTf;

	public Transform scanLineTf;

	public Transform hardLockLine;

	public GameObject friendlyIconTemplate;

	private ObjectPool friendlyIconPool;

	public GameObject enemyIconTemplate;

	private ObjectPool enemyIconPool;

	public GameObject missileIconTemplate;

	private ObjectPool missileIconPool;

	public GameObject enemyGroundTemplate;

	private ObjectPool enemyGroundPool;

	public GameObject friendlyGroundTemplate;

	private ObjectPool friendlyGroundPool;

	public GameObject radarOffObject;

	public float uiHeight = 80f;

	public float uiAngle = 80f;

	public UILineRenderer boundsLine;

	public UILineRenderer range1Line;

	public UILineRenderer range2Line;

	public int rangeVertCount = 10;

	public Transform cursorTransform;

	public float cursorMoveSpeed = 20f;

	public float cursorLockSqrDist = 64f;
    public float[] unitRangeFactors;

    public Text farDistText;

	public Text closeDistText;

	public GameObject lockInfoObj;

	public Text lockBearingText;

	public Text lockRangeText;

	public Text lockAltitudeText;

	public Text targetTypeText;

	public Text lockMachText;

	public Text lockIdxText;

	public Text elevationAngleText;

	public float[] viewRanges;

	public int viewRangeIdx;

	private Dictionary<int, UIRadarContact> contacts = new Dictionary<int, UIRadarContact>();

	public int softLockCount = 4;

	public Text[] softLockTexts;

	public UIRadarContact[] softLocks;

	public Text[] softLockIdentifiers;

	private UIRadarContact cursorLock;

	private UIRadarContact hardLock;

	public float[] fovs;

	public int fovIdx;

	public Text fovText;

	private float origScanRate;

	private bool radarSetOn;

	private bool powerWasRemoteSet;

	private float rDownCounter;

	private int hLockIdx = -1;

	private UIRadarContact remoteHardlock;

	private bool hasClearedGRLockImage = true;

	public float rotationSpeedMul = 1f;

	public OpticalTargeter tgp;

	public TargetingMFDPage tgpPage;

	public MultiUserVehicleSync muvs;

	public float gRadarLockedCamFovFactor = 250f;

	public float gRadarLockedScanSpeed = 1f;

	private UIRadarContact[] cArray = new UIRadarContact[100];

	public Actor currentLockedActor
	{
		get
		{
			if (isRemoteUI)
			{
				if (remoteHardlock != null)
				{
					return remoteHardlock.actor;
				}
				return null;
			}
			if ((bool)lockingRadar && lockingRadar.IsLocked())
			{
				return lockingRadar.currentLock.actor;
			}
			return null;
		}
	}

	public float viewRange => viewRanges[viewRangeIdx];

	public bool isSOI
	{
		get
		{
			if (!mfdPage || !mfdPage.isSOI)
			{
				if ((bool)portalPage)
				{
					return portalPage.isSOI;
				}
				return false;
			}
			return true;
		}
	}

	public bool isRemoteUI { get; set; }

	public event Action<bool> OnToggledAGMode;

	public event Action<int> OnSetRadarPower;

	public event Action OnUnlocked;

	public event SetPlayerRadarDelegate OnSetPlayerRadar;

	public event Action<Actor> OnHardLockActor;

	public event Action<Actor> OnRemoteAttemptHardlock;

	public event Action OnRemoteUnlock;

	public event Action<Actor> OnUIDetectedActor;

	public event Action<int> OnRangeIdx;

	public void ToggleAGMode()
	{
		if (canToggleAG)
		{
			agMode = !agMode;
			UpdateAGToggleRadar();
			this.OnToggledAGMode?.Invoke(agMode);
			if ((bool)currentLockedActor)
			{
				Unlock();
			}
		}
	}

	public void RemoteSetAGMode(bool agm)
	{
		agMode = agm;
		UpdateAGToggleRadar();
		if (agMode && (bool)grDispatcher)
		{
			grDispatcher.ClearImage();
		}
	}

	private void UpdateAGToggleRadar()
	{
		if (!canToggleAG)
		{
			return;
		}
		if ((bool)playerRadar)
		{
			playerRadar.detectAircraft = !agMode;
			playerRadar.detectGround = agMode;
			playerRadar.detectShips = agMode;
			playerRadar.detectMissiles = !agMode;
			if ((bool)agModeText)
			{
				agModeText.text = (agMode ? "GND" : "AIR");
			}
			if (!agMode)
			{
				playerRadar.rotationRange = fovs[fovIdx];
			}
		}
		else if ((bool)agModeText)
		{
			agModeText.text = string.Empty;
		}
		if (!grDispatcher)
		{
			return;
		}
		if (agMode)
		{
			grDispatcher.enabled = true;
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
			}
		}
		else
		{
			grDispatcher.enabled = false;
		}
	}

	public void ToggleBoresightMode()
	{
		if (boresightMode || ((bool)playerRadar && playerRadar.radarEnabled))
		{
			if ((bool)lockingRadar && lockingRadar.IsLocked())
			{
				Unlock();
				ClearSoftLocks();
			}
			boresightMode = !boresightMode;
			boresightDisplayObj.SetActive(boresightMode);
			hudBoresightObj.SetActive(boresightMode);
			radarDisplayTf.gameObject.SetActive(!boresightMode);
			cursorTransform.gameObject.SetActive(!boresightMode);
			if (boresightMode)
			{
				Unlock();
				ClearSoftLocks();
			}
			else
			{
				fovIdx = -1;
				ToggleFov();
			}
		}
	}

	public void ToggleFov()
	{
		fovIdx = (fovIdx + 1) % fovs.Length;
		if ((bool)playerRadar)
		{
			playerRadar.rotationRange = fovs[fovIdx];
		}
		UpdateFOVText();
	}

	private void UpdateFOVText()
	{
		fovText.text = (fovs[fovIdx] / 2f).ToString("0");
	}

	public void ToggleRadarPower()
	{
		if ((bool)playerRadar)
		{
			if (playerRadar.radarEnabled)
			{
				SetRadarPower(0);
			}
			else
			{
				SetRadarPower(1);
			}
		}
	}

	public void SetRadarPower(int st)
	{
		SetPower(st, sendEvt: true);
	}

	private void SetPower(int st, bool sendEvt)
	{
		Debug.Log($"MFDRadarUI.SetPower({st}, {sendEvt})");
		if (st < 1)
		{
			if ((bool)lockingRadar && lockingRadar.IsLocked() && controlsRadar)
			{
				lockingRadar.Unlock();
			}
			if (boresightMode)
			{
				ToggleBoresightMode();
			}
			hardLock = null;
			ClearSoftLocks();
			if ((bool)playerRadar)
			{
				playerRadar.radarEnabled = false;
				lockingRadar.enabled = false;
			}
			ClearAll();
			radarOffObject.SetActive(value: true);
			cursorTransform.gameObject.SetActive(value: false);
			hudBoresightObj.SetActive(value: false);
			radarSetOn = false;
			UpdateAGToggleRadar();
		}
		else
		{
			if ((bool)playerRadar)
			{
				playerRadar.radarEnabled = true;
				lockingRadar.enabled = true;
				radarOffObject.SetActive(value: false);
				if (!boresightMode)
				{
					cursorTransform.gameObject.SetActive(value: true);
				}
				if (boresightMode)
				{
					hudBoresightObj.SetActive(value: true);
				}
				radarSetOn = true;
			}
			if ((bool)grDispatcher)
			{
				grDispatcher.enabled = false;
				grDispatcher.radarCamera.enabled = false;
			}
		}
		if (sendEvt)
		{
			this.OnSetRadarPower?.Invoke(st);
		}
		UpdateAGToggleRadar();
	}

	public void RemoteRadarPower(int st)
	{
		powerWasRemoteSet = true;
		SetPower(st, sendEvt: false);
	}

	private void Awake()
	{
		isMulticrew = wm.GetComponent<MultiUserVehicleSync>();
		SetupPools();
		SetupDisplay();
		softLocks = new UIRadarContact[softLockCount];
		radarCtrlr.OnElevationAdjusted += UpdateElevationText;
		if ((bool)playerRadar)
		{
			origScanRate = playerRadar.rotationSpeed;
			lockingRadar.OnUnlocked += LockingRadar_OnUnlocked;
		}
	}

	private void LockingRadar_OnUnlocked()
	{
		this.OnUnlocked?.Invoke();
	}

	private void OnDestroy()
	{
		if ((bool)enemyIconPool)
		{
			enemyIconPool.DestroyPool();
		}
		if ((bool)friendlyIconPool)
		{
			friendlyIconPool.DestroyPool();
		}
		if ((bool)missileIconPool)
		{
			missileIconPool.DestroyPool();
		}
	}

	public void SetPlayerRadar(Radar r, LockingRadar lr)
	{
		playerRadar = r;
		lockingRadar = lr;
		radarCtrlr.lockingRadar = lr;
		if ((bool)playerRadar)
		{
			origScanRate = playerRadar.rotationSpeed;
			playerRadar.OnDetectedActor += OnRadarSweepDetectedActor;
			lockingRadar.OnUnlocked += LockingRadar_OnUnlocked;
			if (canToggleAG)
			{
				UpdateAGToggleRadar();
			}
		}
		if ((bool)noRadarObj)
		{
			noRadarObj.SetActive(!playerRadar);
		}
		if ((bool)radarAvailableDisplayObj)
		{
			radarAvailableDisplayObj.SetActive(playerRadar);
		}
		this.OnSetPlayerRadar?.Invoke(r, lr);
	}

	private void Start()
	{
		SetPlayerRadar(playerRadar, lockingRadar);
		measurements.OnChangedDistanceMode += Measurements_OnChangedDistanceMode;
		if ((bool)mfdPage)
		{
			mfdPage.OnInputAxis.AddListener(OnInputAxis);
			mfdPage.OnInputAxisReleased.AddListener(OnInputReleased);
			mfdPage.OnInputButtonDown.AddListener(OnInputButtonDown);
		}
		if ((bool)portalPage)
		{
			portalPage.OnInputAxis.AddListener(OnInputAxis);
			portalPage.OnInputAxisReleased.AddListener(OnInputReleased);
			portalPage.OnInputButtonDown.AddListener(OnInputButtonDown);
		}
		UpdateFOVText();
		StartCoroutine(RefreshRoutine());
		if (defaultRadarOff && !powerWasRemoteSet)
		{
			SetPower(0, sendEvt: false);
		}
		UpdateAGToggleRadar();
	}

	public void UpdateElevationText(float e)
	{
		if ((bool)elevationAngleText)
		{
			elevationAngleText.text = Mathf.Round(e).ToString();
		}
	}

	public void SoftLockButton(int idx)
	{
		UIRadarContact uIRadarContact = softLocks[idx];
		if (uIRadarContact == null || !uIRadarContact.active)
		{
			return;
		}
		if (hardLock != null && hardLock.actorID == uIRadarContact.actorID)
		{
			if (controlsRadar)
			{
				lockingRadar.Unlock();
			}
		}
		else
		{
			HardLock(uIRadarContact);
		}
	}

	private void OnInputAxis(Vector3 axis)
	{
		if (boresightMode)
		{
			return;
		}
		Vector3 localPosition = cursorTransform.localPosition;
		localPosition += cursorMoveSpeed * Time.deltaTime * axis;
		localPosition = Vector3.RotateTowards(Vector3.up, localPosition, 0.5f * uiAngle * ((float)Math.PI / 180f), float.MaxValue);
		float num = localPosition.magnitude;
		if (num > uiHeight)
		{
			if (viewRangeIdx < viewRanges.Length - 1)
			{
				RangeUp();
				num /= 2f;
			}
			else
			{
				num = uiHeight;
			}
		}
		else if (num < uiHeight * 0.1f && axis.y < -0.5f)
		{
			rDownCounter += Time.deltaTime;
			if (rDownCounter > 0.75f && viewRangeIdx > 0)
			{
				RangeDown();
				num *= 2.5f;
			}
			else if (num < uiHeight * 0.05f)
			{
				num = uiHeight * 0.05f;
			}
		}
		else
		{
			rDownCounter = 0f;
		}
		localPosition = localPosition.normalized * num;
		localPosition.x = Mathf.Clamp(localPosition.x, -50f, 50f);
		cursorTransform.localPosition = localPosition;
		cursorTransform.localRotation = Quaternion.LookRotation(Vector3.forward, cursorTransform.localPosition);
		cursorLock = null;
	}

	private void OnInputReleased()
	{
		if (boresightMode)
		{
			return;
		}
		float num = cursorLockSqrDist;
		UIRadarContact uIRadarContact = null;
		for (int i = 0; i < cArray.Length; i++)
		{
			UIRadarContact uIRadarContact2 = cArray[i];
			if (uIRadarContact2 != null && uIRadarContact2.active)
			{
				float sqrMagnitude = (uIRadarContact2.iconObject.transform.localPosition - cursorTransform.localPosition).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					uIRadarContact = uIRadarContact2;
				}
			}
		}
		cursorLock = uIRadarContact;
		UpdateCursorLock();
	}

	private void HardLock(UIRadarContact c, bool eventOnRemote = true)
	{
		if (isRemoteUI)
		{
			if (eventOnRemote)
			{
				this.OnRemoteAttemptHardlock?.Invoke(c.actor);
				return;
			}
			remoteHardlock = c;
			hLockIdx = 0;
			hudBoresightObj.SetActive(value: false);
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
			}
			return;
		}
		if (lockingRadar.IsLocked() && controlsRadar)
		{
			lockingRadar.Unlock();
		}
		if (!controlsRadar || lockingRadar.GetLock(c.actor, out var _))
		{
			if (softLocks == null)
			{
				softLocks = new UIRadarContact[softLockCount];
			}
			hardLock = c;
			hudBoresightObj.SetActive(value: false);
			hLockIdx = softLocks.IndexOf(c);
			this.OnHardLockActor?.Invoke(c.actor);
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
			}
		}
	}

	public void RemoteHardLock(Actor a)
	{
		if (lockingRadar.IsLocked() && controlsRadar)
		{
			Unlock();
		}
		OnDetectedActor(a);
		if (contacts.TryGetValue(a.actorID, out var value))
		{
			HardLock(value, eventOnRemote: false);
		}
		playerRadar.RemoteAddActor(a, 0.4f);
	}

	public void RemoteDetectActor(Actor actor)
	{
		OnDetectedActor(actor);
		playerRadar.RemoteAddActor(actor, playerRadar.detectionPersistanceTime);
	}

	public void Unlock()
	{
		if ((bool)lockingRadar && lockingRadar.IsLocked())
		{
			if (controlsRadar)
			{
				lockingRadar.Unlock();
			}
			if (boresightMode && playerRadar.radarEnabled)
			{
				hudBoresightObj.SetActive(value: true);
			}
			hardLock = null;
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
				hasClearedGRLockImage = true;
			}
		}
		if (isRemoteUI)
		{
			this.OnRemoteUnlock?.Invoke();
			remoteHardlock = null;
		}
	}

	public int SoftLockActor(Actor a)
	{
		int num = -1;
		UIRadarContact uIRadarContact = null;
		for (int i = 0; i < contacts.Count; i++)
		{
			if (uIRadarContact != null)
			{
				break;
			}
			if (contacts[i] != null && contacts[i].actor == a)
			{
				uIRadarContact = contacts[i];
			}
		}
		if (uIRadarContact != null)
		{
			for (int j = 0; j < softLocks.Length; j++)
			{
				if (num >= 0)
				{
					break;
				}
				if (softLocks[j] == null || !softLocks[j].active)
				{
					softLocks[j] = cursorLock;
					num = j;
				}
			}
		}
		return num;
	}

	private void OnInputButtonDown()
	{
		if (!playerRadar.radarEnabled)
		{
			return;
		}
		if (boresightMode && lockingRadar.IsLocked())
		{
			Unlock();
		}
		else if (!boresightMode)
		{
			if (cursorLock != null && cursorLock.active)
			{
				bool flag = false;
				if (doMultiTrack)
				{
					for (int i = 0; i < softLocks.Length; i++)
					{
						UIRadarContact uIRadarContact = softLocks[i];
						if (uIRadarContact != null && uIRadarContact.actorID == cursorLock.actorID)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag || !doMultiTrack)
				{
					HardLock(cursorLock);
				}
				else
				{
					bool flag2 = false;
					for (int j = 0; j < softLocks.Length; j++)
					{
						if (flag2)
						{
							break;
						}
						if (softLocks[j] == null || !softLocks[j].active)
						{
							softLocks[j] = cursorLock;
							flag2 = true;
						}
					}
					if (!flag2)
					{
						softLocks[softLockCount - 1] = cursorLock;
					}
				}
			}
			else if ((bool)currentLockedActor)
			{
				Unlock();
			}
		}
		UpdateLocks();
	}

	private void UpdateLocks()
	{
		if (!playerRadar)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < softLockCount; i++)
		{
			UIRadarContact uIRadarContact = softLocks[i];
			if (uIRadarContact != null)
			{
				if (uIRadarContact.active && lockingRadar.CheckLockAbility(uIRadarContact.actor))
				{
					UpdateActorIcon(uIRadarContact, resetTime: true);
					bool flag = hardLock != null && hardLock.actorID == uIRadarContact.actorID;
					softLockTexts[i].text = string.Format("[{0}]{1}", i + 1, flag ? " LOCK" : string.Empty);
					softLockIdentifiers[i].gameObject.SetActive(value: true);
					softLockIdentifiers[i].transform.localPosition = uIRadarContact.iconObject.transform.localPosition;
					lockingRadar.UpdateTWSLock(uIRadarContact.actor);
					num++;
				}
				else
				{
					lockingRadar.RemoveTWSLock(uIRadarContact.actor);
					softLocks[i] = null;
					softLockTexts[i].text = string.Empty;
					softLockIdentifiers[i].gameObject.SetActive(value: false);
				}
			}
			else
			{
				softLockTexts[i].text = string.Empty;
				softLockIdentifiers[i].gameObject.SetActive(value: false);
			}
		}
		playerRadar.rotationSpeed = rotationSpeedMul * origScanRate / (float)(num + 1);
		scanLineTf.gameObject.SetActive(value: true);
		hardLockLine.gameObject.SetActive(value: false);
		lockInfoObj.SetActive(value: false);
		if (isRemoteUI && remoteHardlock != null)
		{
			UpdateActorIcon(remoteHardlock, resetTime: true);
			scanLineTf.gameObject.SetActive(value: false);
			hardLockLine.gameObject.SetActive(value: true);
			hardLockLine.transform.localRotation = Quaternion.LookRotation(Vector3.forward, remoteHardlock.iconObject.transform.localPosition);
			UpdateLockBRA();
		}
		else
		{
			if (hardLock == null)
			{
				return;
			}
			if (lockingRadar.IsLocked())
			{
				if (!controlsRadar || lockingRadar.currentLock.actor == hardLock.actor)
				{
					UpdateActorIcon(hardLock, resetTime: true);
					scanLineTf.gameObject.SetActive(value: false);
					hardLockLine.gameObject.SetActive(value: true);
					hardLockLine.transform.localRotation = Quaternion.LookRotation(Vector3.forward, hardLock.iconObject.transform.localPosition);
					UpdateLockBRA();
				}
				else if (controlsRadar)
				{
					lockingRadar.Unlock();
					hardLock = null;
				}
			}
			else if (controlsRadar)
			{
				hardLock = null;
			}
		}
	}

	private void UpdateLockBRA()
	{
		UIRadarContact uIRadarContact = hardLock;
		if (isRemoteUI)
		{
			uIRadarContact = remoteHardlock;
		}
		lockInfoObj.SetActive(value: true);
		lockBearingText.text = Mathf.Round(VectorUtils.Bearing(playerRadar.transform.position, uIRadarContact.actor.position)).ToString();
		lockRangeText.text = Mathf.Round(measurements.ConvertedDistance((playerRadar.transform.position - uIRadarContact.actor.position).magnitude)).ToString();
		float altitude = WaterPhysics.GetAltitude(uIRadarContact.actor.position);
		lockAltitudeText.text = Mathf.Round(measurements.ConvertedAltitude(altitude)).ToString();
		if ((bool)lockMachText)
		{
			lockMachText.text = MeasurementManager.SpeedToMach(uIRadarContact.actor.velocity.magnitude, altitude).ToString("0.00");
		}
		if ((bool)lockIdxText)
		{
			if (hLockIdx >= 0)
			{
				lockIdxText.gameObject.SetActive(value: true);
				lockIdxText.text = $"[{hLockIdx + 1}]";
			}
			else
			{
				lockIdxText.gameObject.SetActive(value: false);
			}
		}
		if (!targetTypeText)
		{
			return;
		}
		if ((bool)uIRadarContact.actor.unitSpawn)
		{
			if (uIRadarContact.actor.unitSpawn is AIAircraftSpawn)
			{
				targetTypeText.text = ((AIAircraftSpawn)uIRadarContact.actor.unitSpawn).vehicleName;
			}
			else if (uIRadarContact.actor.unitSpawn is MultiplayerSpawn)
			{
				MultiplayerSpawn multiplayerSpawn = (MultiplayerSpawn)uIRadarContact.actor.unitSpawn;
				targetTypeText.text = uIRadarContact.actor.actorName + "\n" + multiplayerSpawn.VehicleName();
			}
			else
			{
				targetTypeText.text = uIRadarContact.actor.unitSpawn.unitSpawner.unitName;
			}
		}
		else
		{
			targetTypeText.text = uIRadarContact.actor.actorName;
		}
	}

	private void ClearAll()
	{
		foreach (UIRadarContact value in contacts.Values)
		{
			value?.iconObject.SetActive(value: false);
		}
		contacts.Clear();
		for (int i = 0; i < cArray.Length; i++)
		{
			cArray[i] = null;
		}
		lockInfoObj.SetActive(value: false);
		hardLock = null;
		remoteHardlock = null;
		for (int j = 0; j < softLockCount; j++)
		{
			softLocks[j] = null;
		}
		scanLineTf.gameObject.SetActive(value: false);
		hardLockLine.gameObject.SetActive(value: false);
		if ((bool)grDispatcher)
		{
			grDispatcher.ClearImage();
		}
	}

	public void ClearSoftLocks()
	{
		for (int i = 0; i < softLockCount; i++)
		{
			if (softLocks[i] != null)
			{
				if ((bool)softLocks[i].actor && (bool)lockingRadar)
				{
					lockingRadar.RemoveTWSLock(softLocks[i].actor);
				}
				if (hardLock == null || softLocks[i].actorID != hardLock.actorID)
				{
					softLocks[i] = null;
				}
			}
		}
		UpdateLocks();
	}

	private IEnumerator RefreshRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(refreshInterval);
		while (true)
		{
			if (!playerRadar || !playerRadar.radarEnabled)
			{
				yield return null;
				continue;
			}
			if (boresightMode && !lockingRadar.IsLocked())
			{
				UpdateBoresight();
			}
			else
			{
				RefreshAllIcons();
			}
			yield return wait;
		}
	}

	public void TGPToLockButton()
	{
		if ((!isMulticrew) && (bool)tgp && tgp.powered && (bool)currentLockedActor)
		{
			tgpPage.SlewAndLockActor(currentLockedActor, 360f);
		}
	}

	public void GPSSendLockButton()
	{
		if (!currentLockedActor)
		{
			return;
		}
		if ((bool)muvs && VTOLMPUtils.IsMultiplayer() && !muvs.isMine)
		{
			muvs.RemoteGPS_AddTarget("RDR", VTMapManager.WorldToGlobalPoint(currentLockedActor.position));
			return;
		}
		GPSTargetSystem gpsSystem = wm.gpsSystem;
		if (gpsSystem.noGroups)
		{
			wm.gpsSystem.CreateCustomGroup();
		}
		gpsSystem.AddTarget(currentLockedActor.position, "RDR");
	}

	private void Update()
	{
		if (!playerRadar)
		{
			return;
		}
		if ((((bool)mfdPage && mfdPage.isOpen) || ((bool)portalPage && portalPage.pageState != MFDPortalPage.PageStates.Minimized && portalPage.pageState != MFDPortalPage.PageStates.SubSized)) && playerRadar.radarEnabled)
		{
			scanLineTf.localRotation = Quaternion.Euler(0f, 0f, (0f - playerRadar.currentAngle) * (uiAngle / playerRadar.rotationRange));
		}
		radarCtrlr.boresightMode = boresightMode;
		if (radarSetOn)
		{
			if (battery.Drain(0.01f * Time.deltaTime))
			{
				if (!playerRadar.radarEnabled)
				{
					SetRadarPower(1);
				}
			}
			else if (playerRadar.radarEnabled)
			{
				Unlock();
				ClearSoftLocks();
				playerRadar.radarEnabled = false;
				lockingRadar.enabled = false;
				ClearAll();
			}
		}
		if (!grDispatcher)
		{
			return;
		}
		if (playerRadar.radarEnabled && agMode)
		{
			groundRadarImage.gameObject.SetActive(value: true);
			grDispatcher.radarCamera.enabled = true;
			UIRadarContact uIRadarContact = (isRemoteUI ? remoteHardlock : hardLock);
			grDispatcher.isLocked = uIRadarContact != null;
			if (grDispatcher.isLocked)
			{
				float magnitude = (uIRadarContact.actor.position - grDispatcher.radarCamera.transform.position).magnitude;
				float num = uIRadarContact.actor.physicalRadius * gRadarLockedCamFovFactor / magnitude;
				grDispatcher.radarCamera.fieldOfView = num;
				radarCtrlr.overrideLookatTransform = uIRadarContact.actor.transform;
				playerRadar.rotationSpeed = num * gRadarLockedScanSpeed;
				playerRadar.rotationRange = num;
				hasClearedGRLockImage = false;
			}
			else
			{
				grDispatcher.radarCamera.transform.localRotation = Quaternion.identity;
				grDispatcher.radarCamera.fieldOfView = playerRadar.sweepFov;
				playerRadar.rotationRange = fovs[fovIdx];
				radarCtrlr.overrideLookatTransform = null;
			}
		}
		else
		{
			groundRadarImage.gameObject.SetActive(value: false);
			grDispatcher.radarCamera.enabled = false;
			radarCtrlr.overrideLookatTransform = null;
		}
		if (!hasClearedGRLockImage && !currentLockedActor)
		{
			grDispatcher.ClearImage();
			hasClearedGRLockImage = true;
		}
	}

	private void RefreshAllIcons()
	{
		if (!playerRadar || !playerRadar.radarEnabled)
		{
			return;
		}
		int count = contacts.Count;
		if (cArray.Length < count)
		{
			cArray = new UIRadarContact[count * 2];
		}
		contacts.Values.CopyTo(cArray, 0);
		for (int i = 0; i < count; i++)
		{
			UIRadarContact uIRadarContact = cArray[i];
			if (uIRadarContact != null && (uIRadarContact.actor == null || Time.time - uIRadarContact.timeFound > playerRadar.detectionPersistanceTime || Vector3.Angle(uIRadarContact.iconObject.transform.localPosition, Vector3.up) > uiAngle / 2f || uIRadarContact.actor.team != uIRadarContact.team))
			{
				contacts.Remove(uIRadarContact.actorID);
				uIRadarContact.iconObject.SetActive(value: false);
				uIRadarContact.active = false;
			}
		}
		UpdateCursorLock();
		UpdateLocks();
	}

	private void UpdateCursorLock()
	{
		if (cursorLock != null)
		{
			if (cursorLock.active)
			{
				cursorTransform.localPosition = cursorLock.iconObject.transform.localPosition;
				cursorTransform.localRotation = Quaternion.LookRotation(Vector3.forward, cursorTransform.localPosition);
			}
			else
			{
				cursorLock = null;
			}
		}
	}

	private void UpdateBoresight()
	{
		if (lockingRadar.IsLocked())
		{
			return;
		}
		lockInfoObj.SetActive(value: false);
		boresightDisplayObj.SetActive(value: true);
		radarDisplayTf.gameObject.SetActive(value: false);
		cursorTransform.gameObject.SetActive(value: false);
		hudBoresightObj.SetActive(value: true);
		Vector3 forward = playerRadar.myActor.transform.forward;
		int roleMask = 8;
		Actor opticalTargetFromView = TargetManager.instance.GetOpticalTargetFromView(playerRadar.myActor, boresightRange, roleMask, 50f, playerRadar.transform.position, forward, boresightFOV, random: false, allActors: true);
		if (!(opticalTargetFromView != null))
		{
			return;
		}
		OnDetectedActor(opticalTargetFromView);
		boresightDisplayObj.SetActive(value: false);
		radarDisplayTf.gameObject.SetActive(value: true);
		cursorTransform.gameObject.SetActive(value: false);
		foreach (UIRadarContact value in contacts.Values)
		{
			if (value.actor == opticalTargetFromView)
			{
				HardLock(value);
				break;
			}
		}
	}

	private void OnRadarSweepDetectedActor(Actor a)
	{
		if (!lockingRadar.IsLocked())
		{
			OnDetectedActor(a);
		}
	}

	private void OnDetectedActor(Actor a)
	{
		if (!a)
		{
			return;
		}
		int actorID = a.actorID;
		if (contacts.ContainsKey(actorID))
		{
			UpdateActorIcon(contacts[actorID], resetTime: true);
		}
		else
		{
			UIRadarContact uIRadarContact = new UIRadarContact();
			try
			{
				uIRadarContact.detectedPosition = new FixedPoint(a.position);
			}
			catch (MissingReferenceException)
			{
				uIRadarContact.detectedPosition = new FixedPoint(a.transform.position);
			}
			uIRadarContact.actor = a;
			uIRadarContact.team = a.team;
			if (a.finalCombatRole == Actor.Roles.Missile)
			{
				uIRadarContact.iconObject = missileIconPool.GetPooledObject();
			}
			else if (a.team != playerRadar.myActor.team)
			{
				if (a.finalCombatRole == Actor.Roles.Air || !enemyGroundPool)
				{
					uIRadarContact.iconObject = enemyIconPool.GetPooledObject();
				}
				else
				{
					uIRadarContact.iconObject = enemyGroundPool.GetPooledObject();
				}
			}
			else if (a.finalCombatRole == Actor.Roles.Air || !friendlyGroundPool)
			{
				uIRadarContact.iconObject = friendlyIconPool.GetPooledObject();
			}
			else
			{
				uIRadarContact.iconObject = friendlyGroundPool.GetPooledObject();
			}
			uIRadarContact.iconObject.SetActive(value: true);
			uIRadarContact.iconObject.transform.SetParent(radarDisplayTf);
			uIRadarContact.iconObject.transform.localScale = Vector3.one;
			uIRadarContact.actorID = actorID;
			uIRadarContact.active = true;
			UpdateActorIcon(uIRadarContact, resetTime: true);
			contacts.Add(actorID, uIRadarContact);
		}
		this.OnUIDetectedActor?.Invoke(a);
	}

	private void UpdateActorIcon(UIRadarContact contact, bool resetTime)
	{
		if (resetTime)
		{
			contact.detectedPosition.point = contact.actor.position;
			contact.detectedVelocity = contact.actor.velocity;
			contact.timeFound = Time.time;
		}
		float num = -1f;
		if (hardLock != null || remoteHardlock != null)
		{
			num = fovs[0];
		}
		contact.iconObject.transform.localPosition = WorldToRadarPoint(contact.detectedPosition.point, num);
		if (contact.actor.finalCombatRole == Actor.Roles.Air || contact.actor.finalCombatRole == Actor.Roles.Missile)
		{
			contact.iconObject.transform.localRotation = Quaternion.LookRotation(Vector3.forward, WorldToRadarDirection(contact.detectedPosition.point, contact.detectedVelocity, num));
		}
		else
		{
			contact.iconObject.transform.localRotation = Quaternion.identity;
		}
	}

	private void Measurements_OnChangedDistanceMode()
	{
		UpdateDistanceTexts();
	}

	public void RangeUp()
	{
		if (viewRangeIdx != viewRanges.Length - 1)
		{
			viewRangeIdx++;
			UpdateDistanceTexts();
			RefreshAllIcons();
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
			}
			this.OnRangeIdx?.Invoke(viewRangeIdx);
		}
	}

	public void RangeDown()
	{
		if (viewRangeIdx != 0)
		{
			viewRangeIdx--;
			UpdateDistanceTexts();
			RefreshAllIcons();
			if ((bool)grDispatcher)
			{
				grDispatcher.ClearImage();
			}
			this.OnRangeIdx?.Invoke(viewRangeIdx);
		}
	}

	public void RemoteSetRange(int idx)
	{
		viewRangeIdx = idx;
		UpdateDistanceTexts();
		RefreshAllIcons();
		if ((bool)grDispatcher)
		{
			grDispatcher.ClearImage();
		}
	}

	private void SetupPools()
	{
		friendlyIconPool = ObjectPool.CreateObjectPool(friendlyIconTemplate, 10, canGrow: true, destroyOnLoad: true);
		friendlyIconTemplate.SetActive(value: false);
		enemyIconPool = ObjectPool.CreateObjectPool(enemyIconTemplate, 10, canGrow: true, destroyOnLoad: true);
		enemyIconTemplate.SetActive(value: false);
		missileIconPool = ObjectPool.CreateObjectPool(missileIconTemplate, 10, canGrow: true, destroyOnLoad: true);
		missileIconTemplate.SetActive(value: false);
		if ((bool)enemyGroundTemplate)
		{
			enemyGroundPool = ObjectPool.CreateObjectPool(enemyGroundTemplate, 10, canGrow: true, destroyOnLoad: true);
			enemyGroundTemplate.gameObject.SetActive(value: false);
		}
		if ((bool)friendlyGroundTemplate)
		{
			friendlyGroundPool = ObjectPool.CreateObjectPool(friendlyGroundTemplate, 10, canGrow: true, destroyOnLoad: true);
			friendlyGroundTemplate.gameObject.SetActive(value: false);
		}
	}

	private void SetupDisplay()
	{
		Vector2[] array = new Vector2[3];
		array[0] = Quaternion.AngleAxis((0f - uiAngle) / 2f, Vector3.forward) * new Vector2(0f, uiHeight);
		array[2] = Quaternion.AngleAxis(uiAngle / 2f, Vector3.forward) * new Vector2(0f, uiHeight);
		array[1] = Vector2.zero;
		boundsLine.Points = array;
		boundsLine.SetAllDirty();
		Vector2[] array2 = new Vector2[rangeVertCount + 1];
		Vector2[] array3 = new Vector2[rangeVertCount + 1];
		float num = uiAngle / (float)rangeVertCount;
		for (int i = 0; i <= rangeVertCount; i++)
		{
			array3[i] = 0.5f * (array2[i] = Quaternion.AngleAxis((float)i * num - uiAngle / 2f, Vector3.forward) * new Vector2(0f, uiHeight));
		}
		range1Line.Points = array2;
		range2Line.Points = array3;
		UpdateDistanceTexts();
	}

	private void UpdateDistanceTexts()
	{
		float num = viewRange;
		num = measurements.distanceMode switch
		{
			MeasurementManager.DistanceModes.Meters => Mathf.Round(num / 1000f), 
			MeasurementManager.DistanceModes.NautMiles => Mathf.Round(MeasurementManager.DistToNauticalMile(num)), 
			_ => Mathf.Round(MeasurementManager.DistToMiles(num)), 
		};
		float num2 = num / 2f;
		farDistText.text = num.ToString();
		closeDistText.text = num2.ToString();
	}

	private Vector3 WorldToRadarPoint(Vector3 worldPoint, float overrideFov = -1f)
	{
		if (overrideFov < 0f)
		{
			overrideFov = playerRadar.rotationRange;
		}
		worldPoint.y = playerRadar.transform.position.y;
		Vector3 forward = playerRadar.transform.forward;
		forward.y = 0f;
		float angle = VectorUtils.SignedAngle(forward, worldPoint - playerRadar.transform.position, Vector3.Cross(Vector3.up, forward)) * (uiAngle / overrideFov);
		float num = Vector3.Distance(worldPoint, playerRadar.transform.position);
		num *= uiHeight / viewRange;
		return Quaternion.AngleAxis(angle, -Vector3.forward) * new Vector3(0f, num, 0f);
	}

	private Vector3 WorldToRadarDirection(Vector3 worldPosition, Vector3 worldDirection, float fovOverride = -1f)
	{
		return (WorldToRadarPoint(worldPosition + worldDirection, fovOverride) - WorldToRadarPoint(worldPosition, fovOverride)).normalized;
	}

	public void OnQuicksave(ConfigNode qsNode)
	{
		ConfigNode configNode = new ConfigNode("MFDRadarUI");
		qsNode.AddNode(configNode);
		configNode.SetValue("radarPower", playerRadar.radarEnabled);
		foreach (UIRadarContact value in contacts.Values)
		{
			ConfigNode node = value.SaveToConfigNode("contact");
			configNode.AddNode(node);
		}
		for (int i = 0; i < softLocks.Length; i++)
		{
			ConfigNode configNode2 = new ConfigNode("softLock");
			configNode2.SetValue("idx", i);
			configNode2.SetValue("actorID", (softLocks[i] == null) ? (-1) : softLocks[i].actorID);
			configNode.AddNode(configNode2);
		}
		if (cursorLock != null && cursorLock.active)
		{
			ConfigNode configNode3 = new ConfigNode("cursorLock");
			configNode3.SetValue("actorID", cursorLock.actorID);
			configNode.AddNode(configNode3);
		}
		if (hardLock != null && hardLock.active)
		{
			ConfigNode configNode4 = new ConfigNode("hardLock");
			configNode4.SetValue("actorID", hardLock.actorID);
			configNode.AddNode(configNode4);
		}
		configNode.SetValue("viewRangeIdx", viewRangeIdx);
		configNode.SetValue("fovIdx", fovIdx);
		configNode.SetValue("boresightMode", boresightMode);
	}

	public void OnQuickload(ConfigNode qsNode)
	{
		string text = "MFDRadarUI";
		if (!qsNode.HasNode(text))
		{
			return;
		}
		ConfigNode node = qsNode.GetNode(text);
		bool value = node.GetValue<bool>("radarPower");
		SetRadarPower(value ? 1 : 0);
		int num = -1;
		int num2 = -1;
		int[] array = new int[softLocks.Length];
		if (node.HasNode("hardLock"))
		{
			num = node.GetNode("hardLock").GetValue<int>("actorID");
		}
		if (node.HasNode("cursorLock"))
		{
			num2 = node.GetNode("cursorLock").GetValue<int>("actorID");
		}
		foreach (ConfigNode node2 in node.GetNodes("softLock"))
		{
			array[node2.GetValue<int>("idx")] = node2.GetValue<int>("actorID");
		}
		foreach (ConfigNode node3 in node.GetNodes("contact"))
		{
			int value2 = node3.GetValue<int>("unitID");
			Vector3D value3 = node3.GetValue<Vector3D>("globalPos");
			int value4 = node3.GetValue<int>("subUnitID");
			Actor actor = QuicksaveManager.RetrieveActor(value2, value3, value4);
			if (!actor)
			{
				continue;
			}
			int value5 = node3.GetValue<int>("actorID");
			int actorID = actor.actorID;
			OnDetectedActor(actor);
			UIRadarContact uIRadarContact = contacts[actorID];
			float num3 = (uIRadarContact.timeFound = Time.time - node3.GetValue<float>("timeFoundElapsed"));
			if (value5 == num2)
			{
				cursorLock = uIRadarContact;
				UpdateCursorLock();
			}
			if (value5 == num)
			{
				hardLock = uIRadarContact;
				lockingRadar.ForceLock(actor, out var _);
			}
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i] == value5)
				{
					softLocks[i] = uIRadarContact;
					Debug.Log(" - Quickloaded softlock: " + uIRadarContact.actor.actorName);
				}
			}
		}
		viewRangeIdx = node.GetValue<int>("viewRangeIdx");
		fovIdx = node.GetValue<int>("fovIdx");
		boresightMode = node.GetValue<bool>("boresightMode");
		if (boresightMode)
		{
			if (!playerRadar.radarEnabled)
			{
				Debug.Log("Quickloading player radar boresight but playerRadar.enabled == false");
			}
			boresightMode = false;
			ToggleBoresightMode();
		}
		playerRadar.rotationRange = fovs[fovIdx];
		UpdateFOVText();
		UpdateDistanceTexts();
		RefreshAllIcons();
		UpdateLocks();
		Debug.Log("MFDRadarUI Quickloaded.");
	}
}
