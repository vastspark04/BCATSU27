using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MissileDetector : MonoBehaviour
{
	public delegate void MissileDelegate(Missile m);

	private int missileCount;

	private List<Missile> missiles = new List<Missile>();

	[HideInInspector]
	public List<Missile> detectedMissiles = new List<Missile>();

	public float detectInterval = 1f;

	private float lastDetect;

	public float detectRange = 5000f;

	[HideInInspector]
	public List<Vector3> missilePositions;

	private Transform detectorTransform;

	public List<MissileLaunchDetector> launchDetectors;

	[Tooltip("Event is invoked if attached launch detectors successfully detect a launch")]
	public UnityEvent OnMissileLaunchDetected;

	public bool detectIncoming;

	private Rigidbody parentRb;

	public FixedPoint lastDetectedMissileLaunchPoint;

	public float missileIncomingSpeedThresh = 400f;

	public float missileIncomingDotThresh = 0.9997f;

	public float missileIncomingHeatOverDistSqrThresh = 1f;

	private HeatEmitter detectedEmitter;

	private Vector3 lastEmitterDir;

	public Missile nearestThreat { get; private set; }

	public bool launchWasDetected { get; private set; }

	public bool missileDetected
	{
		get
		{
			if (missileCount > 0)
			{
				return nearestThreat != null;
			}
			return false;
		}
	}

	public bool missileIncomingDetected { get; private set; }

	private static event MissileDelegate OnMissileLaunch;

	public event MissileDelegate OnMissileLaunchDetected2;

	private void Start()
	{
		detectorTransform = new GameObject("DetectorTransform").transform;
		detectorTransform.parent = base.transform;
		detectorTransform.localPosition = Vector3.zero;
	}

	private void OnEnable()
	{
		OnMissileLaunch += MissileDetector_OnMissileLaunch;
		OnMissileLaunchDetected2 += MissileDetector_OnMissileLaunchDetected2;
		StartCoroutine(UpdateRoutine());
		if (detectIncoming)
		{
			StartCoroutine(IncomingMissileWarningRoutine());
		}
	}

	private void MissileDetector_OnMissileLaunchDetected2(Missile m)
	{
		launchWasDetected = true;
		lastDetectedMissileLaunchPoint = new FixedPoint(m.transform.position);
	}

	private IEnumerator UpdateRoutine()
	{
		WaitForSeconds wait = new WaitForSeconds(detectInterval);
		while (!detectorTransform)
		{
			yield return null;
		}
		while (base.enabled)
		{
			detectedMissiles.Clear();
			lastDetect = Time.time;
			missiles.RemoveAll((Missile x) => x == null || !x.hasTarget);
			missileCount = missiles.Count;
			detectorTransform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(base.transform.forward, Vector3.up));
			nearestThreat = null;
			float num = detectRange * detectRange;
			float num2 = num;
			missilePositions.Clear();
			for (int i = 0; i < missileCount; i++)
			{
				float sqrMagnitude = (missiles[i].transform.position - base.transform.position).sqrMagnitude;
				if (sqrMagnitude < num2)
				{
					num2 = sqrMagnitude;
					nearestThreat = missiles[i];
				}
				if (sqrMagnitude < num)
				{
					missilePositions.Add(detectorTransform.InverseTransformPoint(missiles[i].transform.position));
					detectedMissiles.Add(missiles[i]);
				}
			}
			yield return wait;
		}
	}

	private void OnDisable()
	{
		OnMissileLaunch -= MissileDetector_OnMissileLaunch;
		OnMissileLaunchDetected2 -= MissileDetector_OnMissileLaunchDetected2;
	}

	private void MissileDetector_OnMissileLaunch(Missile m)
	{
		bool flag = false;
		foreach (MissileLaunchDetector launchDetector in launchDetectors)
		{
			if (launchDetector.TryDetectLaunch(m.transform.position, m.transform.forward))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			if (OnMissileLaunchDetected != null)
			{
				OnMissileLaunchDetected.Invoke();
			}
			if (this.OnMissileLaunchDetected2 != null)
			{
				this.OnMissileLaunchDetected2(m);
			}
		}
	}

	public void AddMissile(Missile m)
	{
		missileCount++;
		missiles.Add(m);
	}

	public static void AnnounceMissileLaunch(Missile m)
	{
		if (MissileDetector.OnMissileLaunch != null)
		{
			MissileDetector.OnMissileLaunch(m);
		}
	}

	public Transform GetDetectorTransform()
	{
		return detectorTransform;
	}

	public Vector3 GetIncomingMissileVector()
	{
		if (!missileIncomingDetected)
		{
			return Vector3.zero;
		}
		if ((bool)detectedEmitter)
		{
			lastEmitterDir = (detectedEmitter.transform.position - parentRb.position).normalized;
		}
		return lastEmitterDir;
	}

	private IEnumerator IncomingMissileWarningRoutine()
	{
		if (!parentRb)
		{
			parentRb = GetComponentInParent<Rigidbody>();
		}
		yield return null;
		WaitForSeconds intervalWait = new WaitForSeconds(2f);
		while (base.enabled && detectIncoming)
		{
			bool _d = false;
			for (int i = 0; i < HeatEmitter.emitters.Count; i++)
			{
				if (_d)
				{
					break;
				}
				HeatEmitter heatEmitter = HeatEmitter.emitters[i];
				if (!heatEmitter || !heatEmitter.actor || heatEmitter.actor.finalCombatRole != Actor.Roles.Missile)
				{
					continue;
				}
				Vector3 lhs = heatEmitter.velocity - parentRb.velocity;
				Vector3 vector = parentRb.position - heatEmitter.transform.position;
				Vector3 normalized = vector.normalized;
				if (Vector3.Dot(lhs, normalized) > missileIncomingSpeedThresh && Vector3.Dot(lhs.normalized, normalized) > missileIncomingDotThresh && heatEmitter.heat / vector.sqrMagnitude > missileIncomingHeatOverDistSqrThresh)
				{
					for (int j = 0; j < launchDetectors.Count; j++)
					{
						if (_d)
						{
							break;
						}
						if (launchDetectors[j].TryDetectLaunch(heatEmitter.transform.position, heatEmitter.velocity))
						{
							detectedEmitter = heatEmitter;
							_d = true;
						}
					}
				}
				yield return null;
			}
			missileIncomingDetected = _d;
			if (_d)
			{
				yield return intervalWait;
				continue;
			}
			detectedEmitter = null;
			yield return null;
		}
	}
}
