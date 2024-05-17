using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashRWR : ElectronicComponent, ILocalizationUser
{
	public enum RWRModes
	{
		On,
		Silent,
		Off
	}

	public float drain = 1f;

	private bool powered;

	[Header("New Radar Properties")]
	public float receiverSensitivity;

	[Header("Rest of properties")]
	public GameObject missileIconTemplate;

	public GameObject radarIconTemplate;

	public RectTransform circleTransform;

	public AudioSource rwrAudioSource;

	public AudioClip missileBlip;

	public AudioClip radarBlip;

	public AudioClip newContactBlip;

	public AudioClip lockBlip;

	public bool loopLockTone;

	public AudioSource loopLockAudioSource;

	public AudioSource missileLockLoopAudioSource;

	[Header("Launch detector")]
	public MissileDetector missileDetector;

	public Transform launchDetectTf;

	public float launchDetectBlinkTime = 5f;

	private Coroutine launchDetectRoutine;

	public Transform mwsDirTf;

	private float circleRadius;

	private ObjectPool radarIconPool;

	private Actor myActor;

	public float iconWidth;

	private string[] rwrModeLabels = new string[3] { "On", "Silent", "Off" };

	public StringEvent OnSetModeLabel;

	private float lastBlipTime;

	private float blipTimeThreshold = 0.5f;

	private float lastLockBlipTime;

	private float lockBlipTimeThreshold = 0.05f;

	private RWRIcon lastDetectedIcon;

	private RWRIcon threatIcon;

	public ModuleRWR moduleRWR;

	private Dictionary<int, RWRIcon> detectedPings = new Dictionary<int, RWRIcon>();

	private float radarLockLoopPlayTime;

	private int myInstId => myActor.actorID;

	public RWRModes mode { get; private set; }

	public void ApplyLocalization()
	{
		for (int i = 0; i < rwrModeLabels.Length; i++)
		{
			string[] array = rwrModeLabels;
			int num = i;
			string key = $"rwrMode_{i}";
			RWRModes rWRModes = (RWRModes)i;
			array[num] = VTLocalizationManager.GetString(key, rWRModes.ToString(), "RWR mode");
		}
	}

	private void Awake()
	{
		ApplyLocalization();
	}

	private void Start()
	{
		myActor = GetComponentInParent<Actor>();
		circleRadius = circleTransform.rect.width / 2f;
		if (lockBlip == null)
		{
			lockBlip = radarBlip;
			loopLockTone = false;
		}
		if (newContactBlip == null)
		{
			newContactBlip = radarBlip;
		}
		CreatePools();
		if ((bool)moduleRWR)
		{
			moduleRWR.OnDetectPing += ModuleRWR_OnDetectPing;
			moduleRWR.OnLockDetectPing += ModuleRWR_OnLockDetectPing;
		}
		else
		{
			Radar.OnDetect += Radar_OnDetect;
			Radar.OnLockPing += Radar_OnLockPing;
		}
		missileDetector.OnMissileLaunchDetected2 += MissileDetector_OnMissileLaunchDetected2;
		if ((bool)mwsDirTf)
		{
			mwsDirTf.gameObject.SetActive(value: false);
		}
		SetMasterMode((int)mode);
	}

	private void ModuleRWR_OnLockDetectPing(ModuleRWR.RWRContact contact)
	{
		Radar_OnLockPing(myActor, contact.radarActor, contact.radarSymbol, 0.5f, contact.detectedPosition, contact.signalStrength);
	}

	private void ModuleRWR_OnDetectPing(ModuleRWR.RWRContact contact)
	{
		Radar_OnDetect(myActor, contact.radarActor, contact.radarSymbol, 1f, contact.detectedPosition, contact.signalStrength);
	}

	private void MissileDetector_OnMissileLaunchDetected2(Missile m)
	{
		if (!powered)
		{
			return;
		}
		if (m.guidanceMode == Missile.GuidanceModes.Radar)
		{
			LockingRadar surrogateRadar = m.surrogateRadar;
			if ((bool)surrogateRadar && (bool)surrogateRadar.radar && (bool)surrogateRadar.radar.myActor)
			{
				int actorID = surrogateRadar.radar.myActor.actorID;
				if (detectedPings.ContainsKey(actorID))
				{
					detectedPings[actorID].SetFiredMissile();
				}
			}
		}
		if ((bool)launchDetectTf)
		{
			if (launchDetectRoutine != null)
			{
				StopCoroutine(launchDetectRoutine);
			}
			launchDetectRoutine = StartCoroutine(LaunchDetectedRoutine(m.transform.position));
		}
	}

	private void UpdateMWS()
	{
		if ((bool)mwsDirTf)
		{
			if (missileDetector.detectIncoming && missileDetector.missileIncomingDetected)
			{
				mwsDirTf.gameObject.SetActive(value: true);
				Vector3 upwards = WorldToRWRPosition(myActor.position + missileDetector.GetIncomingMissileVector() * 5000f);
				mwsDirTf.localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
			}
			else
			{
				mwsDirTf.gameObject.SetActive(value: false);
			}
		}
	}

	private void OnDestroy()
	{
		Radar.OnDetect -= Radar_OnDetect;
		Radar.OnLockPing -= Radar_OnLockPing;
		if ((bool)radarIconPool)
		{
			radarIconPool.DestroyPool();
		}
	}

	private void Radar_OnLockPing(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength)
	{
		if (mode != RWRModes.Off && powered && detectedActor.actorID == myInstId && IsAdvDetected(signalStrength, radarPosition))
		{
			RadarPing(sourceActor, radarSymbol, persistTime, radarPosition, lockPing: true);
		}
	}

	private void Radar_OnDetect(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPos, float signalStrength)
	{
		if (mode != RWRModes.Off && powered && detectedActor.actorID == myInstId && IsAdvDetected(signalStrength, radarPos))
		{
			RadarPing(sourceActor, radarSymbol, persistTime, radarPos, lockPing: false);
		}
	}

	private bool IsAdvDetected(float signalStrength, Vector3 radarPosition)
	{
		if (Radar.ADV_RADAR && signalStrength * 100f / (radarPosition - base.transform.position).sqrMagnitude < 1f / receiverSensitivity)
		{
			return false;
		}
		return true;
	}

	public void RemovePing(int actorID)
	{
		if (detectedPings.ContainsKey(actorID))
		{
			RWRIcon rWRIcon = detectedPings[actorID];
			detectedPings.Remove(actorID);
			if (rWRIcon == lastDetectedIcon)
			{
				lastDetectedIcon = null;
			}
			if (rWRIcon == threatIcon)
			{
				threatIcon = null;
			}
		}
	}

	private void RadarPing(Actor radarActor, string radarSymbol, float persistTime, Vector3 radarPos, bool lockPing)
	{
		if (!radarActor || !myActor)
		{
			return;
		}
		bool flag = false;
		int actorID = radarActor.actorID;
		RWRIcon rWRIcon;
		if (detectedPings.ContainsKey(actorID))
		{
			rWRIcon = detectedPings[actorID];
		}
		else
		{
			GameObject pooledObject = radarIconPool.GetPooledObject();
			pooledObject.transform.SetParent(circleTransform, worldPositionStays: false);
			pooledObject.transform.localScale = radarIconTemplate.transform.localScale;
			pooledObject.transform.localRotation = Quaternion.identity;
			pooledObject.SetActive(value: true);
			rWRIcon = pooledObject.GetComponent<RWRIcon>();
			rWRIcon.rwr = this;
			if ((bool)lastDetectedIcon)
			{
				lastDetectedIcon.SetLastDetected(lastDetected: false);
			}
			rWRIcon.SetLastDetected(lastDetected: true);
			lastDetectedIcon = rWRIcon;
			detectedPings.Add(actorID, rWRIcon);
			flag = true;
		}
		rWRIcon.UpdateStatus(radarActor, persistTime, radarSymbol);
		if (radarActor.role != Actor.Roles.Missile)
		{
			if (lockPing)
			{
				rWRIcon.SetLocked();
			}
			if (threatIcon == null || threatIcon.actor == null)
			{
				threatIcon = rWRIcon;
				rWRIcon.SetAsThreat(threat: true);
			}
			else
			{
				bool flag2 = (threatIcon.actor.position - myActor.position).sqrMagnitude > (radarActor.position - myActor.position).sqrMagnitude;
				if ((flag2 && !threatIcon.isLocked) || (flag2 && lockPing))
				{
					threatIcon.SetAsThreat(threat: false);
					rWRIcon.SetAsThreat(threat: true);
					threatIcon = rWRIcon;
				}
			}
		}
		rWRIcon.transform.localPosition = WorldToRWRPosition(radarPos);
		rWRIcon.detectionPoint = new FixedPoint(radarPos);
		if (mode != 0)
		{
			return;
		}
		if (lockPing)
		{
			lockBlipTimeThreshold = persistTime;
			if (loopLockTone)
			{
				if (radarActor.role == Actor.Roles.Missile)
				{
					if ((bool)missileLockLoopAudioSource && !missileLockLoopAudioSource.isPlaying)
					{
						missileLockLoopAudioSource.Play();
					}
					radarLockLoopPlayTime = Time.time;
					if ((bool)loopLockAudioSource && loopLockAudioSource.isPlaying)
					{
						loopLockAudioSource.Stop();
					}
				}
				else if ((bool)loopLockAudioSource && !loopLockAudioSource.isPlaying && (!missileLockLoopAudioSource || !missileLockLoopAudioSource.isPlaying))
				{
					loopLockAudioSource.clip = lockBlip;
					loopLockAudioSource.Play();
					loopLockAudioSource.loop = true;
				}
				lastLockBlipTime = Time.time;
			}
			else if (Time.time - lastLockBlipTime > lockBlipTimeThreshold)
			{
				lastLockBlipTime = Time.time;
				if ((bool)rwrAudioSource)
				{
					rwrAudioSource.PlayOneShot(lockBlip);
				}
			}
		}
		else if (Time.time - lastBlipTime > blipTimeThreshold || flag)
		{
			if ((bool)rwrAudioSource)
			{
				rwrAudioSource.PlayOneShot(flag ? newContactBlip : radarBlip);
			}
			lastBlipTime = Time.time;
		}
	}

	private Vector2 WorldToRWRPosition(Vector3 worldPos)
	{
		Vector3 vector = missileDetector.GetDetectorTransform().InverseTransformPoint(worldPos);
		Vector2 vector2 = new Vector2(vector.x, vector.z);
		vector2 = Mathf.Clamp01(circleRadius / missileDetector.detectRange) * vector2;
		return Vector3.ClampMagnitude(vector2, circleRadius - iconWidth / 2f);
	}

	private void Update()
	{
		powered = battery.Drain(drain * Time.deltaTime);
		if (powered)
		{
			if (mode != RWRModes.Off)
			{
				circleTransform.gameObject.SetActive(value: true);
			}
			if (loopLockTone && Time.time - lastLockBlipTime > lockBlipTimeThreshold + 0.5f)
			{
				if ((bool)loopLockAudioSource && loopLockAudioSource.isPlaying)
				{
					loopLockAudioSource.Stop();
				}
				if ((bool)missileLockLoopAudioSource && missileLockLoopAudioSource.isPlaying)
				{
					missileLockLoopAudioSource.Stop();
				}
			}
			if ((bool)missileLockLoopAudioSource && missileLockLoopAudioSource.isPlaying && Time.time - radarLockLoopPlayTime > lockBlipTimeThreshold + 0.5f)
			{
				missileLockLoopAudioSource.Stop();
			}
			UpdateMWS();
			return;
		}
		circleTransform.gameObject.SetActive(value: false);
		if (loopLockTone)
		{
			if ((bool)loopLockAudioSource && loopLockAudioSource.isPlaying)
			{
				loopLockAudioSource.Stop();
			}
			if ((bool)missileLockLoopAudioSource && missileLockLoopAudioSource.isPlaying)
			{
				missileLockLoopAudioSource.Stop();
			}
		}
		if ((bool)mwsDirTf)
		{
			mwsDirTf.gameObject.SetActive(value: false);
		}
	}

	public void SetMasterMode(int mode)
	{
		this.mode = (RWRModes)mode;
		circleTransform.gameObject.SetActive(this.mode != RWRModes.Off);
		if (OnSetModeLabel != null)
		{
			OnSetModeLabel.Invoke(rwrModeLabels[(int)this.mode]);
		}
	}

	public void ToggleMasterMode()
	{
		int num = (int)mode;
		num = (num + 1) % 3;
		SetMasterMode(num);
	}

	private void CreatePools()
	{
		if ((bool)missileIconTemplate)
		{
			missileIconTemplate.SetActive(value: false);
		}
		radarIconPool = ObjectPool.CreateObjectPool(radarIconTemplate, 5, canGrow: true, destroyOnLoad: true);
		radarIconTemplate.SetActive(value: false);
	}

	private IEnumerator LaunchDetectedRoutine(Vector3 launchPos)
	{
		Vector3 upwards = WorldToRWRPosition(launchPos);
		Quaternion localRotation = Quaternion.LookRotation(Vector3.forward, upwards);
		launchDetectTf.localRotation = localRotation;
		float t = Time.time;
		while (Time.time - t < launchDetectBlinkTime)
		{
			launchDetectTf.gameObject.SetActive(!launchDetectTf.gameObject.activeSelf);
			yield return new WaitForSeconds(0.15f);
		}
		launchDetectTf.gameObject.SetActive(value: false);
	}

	public void UpdateIconsForUISize(float iconScale)
	{
		circleRadius = circleTransform.rect.width / 2f;
		radarIconTemplate.transform.localScale = iconScale * Vector3.one;
		foreach (RWRIcon value in detectedPings.Values)
		{
			value.transform.localPosition = WorldToRWRPosition(value.detectionPoint.point);
			value.transform.localScale = iconScale * Vector3.one;
		}
	}
}
