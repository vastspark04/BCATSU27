using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using VTOLVR.Multiplayer;

public class Radar : MonoBehaviour
{
	public enum DetectionTeams
	{
		Allied,
		Enemy,
		Both
	}

	public delegate void RadarEvent(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength);

	private class RadarOccludeTaskResult
	{
		public bool done;

		public bool occluded;
	}

	public const float RADAR_OCCLUSION_THRESHOLD = 5f;

	public const float RADAR_OCCLUSION_THRESHOLD_SQR = 25f;

	public static bool ADV_RADAR = true;

	public bool isMissile;

	public Transform rotationTransform;

	public float rotationSpeed;

	[Range(0f, 360f)]
	public float rotationRange;

	public float sweepFov = 90f;

	private float cosHalfSweepFov;

	public Transform radarTransform;

	[Header("New Radar Params")]
	public float transmissionStrength = 10000f;

	public float receiverSensitivity = 500f;

	private float rotationDirection = 1f;

	private float angle;

	public float detectionPersistanceTime = 1f;

	public List<Actor> detectedUnits;

	public bool disableOnDeath = true;

	public DetectionTeams teamsToDetect;

	public bool detectAircraft = true;

	public bool detectMissiles;

	public bool detectShips;

	public bool detectGround;

	public float detectionDeltaTime = 0.5f;

	public Actor myActor;

	public string radarSymbol = "R";

	public bool debugRadar;

	public bool disabledOnAwake;

	[Header("Legacy/Deprecated")]
	public float detectionRange;

	private float rangeSqr;

	private bool mpRemote;

	private Coroutine detectionRoutine;

	private bool _rEnabled = true;

	public bool allowRotation = true;

	private bool myChunkColliderEnabled;

	private string debugString = string.Empty;

	public static bool useHeightmapTasks = true;

	private Dictionary<int, Coroutine> removalRoutines = new Dictionary<int, Coroutine>();

	public float currentAngle => angle;

	public bool destroyed { get; private set; }

	public bool radarEnabled
	{
		get
		{
			return _rEnabled;
		}
		set
		{
			if (_rEnabled != value)
			{
				_rEnabled = value;
				if (detectionRoutine != null)
				{
					StopCoroutine(detectionRoutine);
				}
				if (_rEnabled && !mpRemote)
				{
					detectionRoutine = StartCoroutine(DetectionRoutine());
				}
				else
				{
					detectedUnits.Clear();
				}
				this.OnRadarEnabled?.Invoke(value);
			}
		}
	}

	public static event RadarEvent OnDetect;

	public static event RadarEvent OnLockPing;

	public event UnityAction<Actor> OnDetectedActor;

	public event Action OnRadarDestroyed;

	public event Action<bool> OnRadarEnabled;

	public void SetToMPRemote()
	{
		mpRemote = true;
	}

	private void Awake()
	{
		if (!myActor)
		{
			myActor = GetComponentInParent<Actor>();
		}
		rangeSqr = detectionRange * detectionRange;
		if (!radarTransform)
		{
			radarTransform = rotationTransform;
		}
		Health componentInParent = GetComponentInParent<Health>();
		if ((bool)componentInParent)
		{
			componentInParent.OnDeath.AddListener(H_OnDeath);
		}
		if (disabledOnAwake)
		{
			radarEnabled = false;
		}
		if (debugRadar && !Application.isEditor)
		{
			debugRadar = false;
		}
	}

	private void Start()
	{
		if (detectionRoutine != null)
		{
			StopCoroutine(detectionRoutine);
		}
		if (_rEnabled)
		{
			detectionRoutine = StartCoroutine(DetectionRoutine());
		}
	}

	private void OnDrawGizmos()
	{
		if ((bool)rotationTransform)
		{
			Transform transform = (radarTransform ? radarTransform : rotationTransform);
			Vector3 vector = rotationTransform.parent.forward * 10f;
			Gizmos.color = Color.yellow;
			Vector3 vector2 = transform.forward * 10f;
			vector2 = Quaternion.AngleAxis((0f - sweepFov) / 2f, transform.right) * vector2;
			Gizmos.DrawLine(transform.position, transform.position + vector2);
			vector2 = Quaternion.AngleAxis(sweepFov, transform.right) * vector2;
			Gizmos.DrawLine(transform.position, transform.position + vector2);
			if (rotationRange < 360f)
			{
				Gizmos.color = Color.green;
				Vector3 vector3 = vector;
				vector3 = Quaternion.AngleAxis((0f - rotationRange) / 2f, rotationTransform.parent.up) * vector3;
				Gizmos.DrawLine(rotationTransform.position, rotationTransform.position + vector3);
				vector3 = Quaternion.AngleAxis(rotationRange, rotationTransform.parent.up) * vector3;
				Gizmos.DrawLine(rotationTransform.position, rotationTransform.position + vector3);
			}
		}
	}

	private void H_OnDeath()
	{
		if (disableOnDeath)
		{
			KillRadar();
		}
	}

	public void KillRadar()
	{
		detectedUnits.Clear();
		destroyed = true;
		radarEnabled = false;
		this.OnRadarDestroyed?.Invoke();
	}

	private void OnEnable()
	{
		cosHalfSweepFov = Mathf.Cos(sweepFov * ((float)Math.PI / 180f) / 2f);
		if (detectionRoutine != null)
		{
			StopCoroutine(detectionRoutine);
		}
		if (_rEnabled)
		{
			detectionRoutine = StartCoroutine(DetectionRoutine());
		}
	}

	private void Update()
	{
		if (!destroyed)
		{
			UpdateRotation();
			if (_rEnabled && detectionRoutine == null)
			{
				detectionRoutine = StartCoroutine(DetectionRoutine());
			}
		}
		else
		{
			if (_rEnabled)
			{
				radarEnabled = false;
			}
			base.enabled = false;
		}
	}

	private void UpdateRotation()
	{
		if (radarEnabled || !allowRotation)
		{
			rotationTransform.localRotation = Quaternion.Euler(0f, angle, 0f);
		}
		if (allowRotation)
		{
			if (rotationRange < 360f)
			{
				angle += rotationSpeed * rotationDirection * Time.deltaTime;
				if (Mathf.Abs(angle) > rotationRange / 2f)
				{
					rotationDirection = 0f - rotationDirection;
					angle = Mathf.Sign(angle) * (rotationRange / 2f);
				}
			}
			else
			{
				angle = Mathf.Repeat(angle + rotationSpeed * Time.deltaTime, 360f);
			}
		}
		else
		{
			angle = Mathf.MoveTowards(angle, 0f, rotationSpeed * Time.deltaTime);
		}
	}

	private IEnumerator DetectionRoutine()
	{
		while (!FlightSceneManager.isFlightReady)
		{
			yield return null;
		}
		while (!VTMapManager.fetch || !VTMapManager.fetch.scenarioReady || (!VTOLMPUtils.IsMultiplayer() && !PlayerSpawn.playerVehicleReady))
		{
			yield return null;
		}
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.01f, detectionDeltaTime));
		WaitForSeconds wait = new WaitForSeconds(detectionDeltaTime);
		while (_rEnabled && !destroyed && !mpRemote)
		{
			yield return wait;
			UpdateDetection(detectionDeltaTime);
		}
	}

	private void UpdateDetection(float deltaTime)
	{
		if ((!FlightSceneManager.instance || !(FlightSceneManager.instance.missionElapsedTime < 1f)) && allowRotation)
		{
			myChunkColliderEnabled = true;
			bool hasMapGen = false;
			if ((bool)VTMapGenerator.fetch)
			{
				myChunkColliderEnabled = VTMapGenerator.fetch.IsChunkColliderEnabled(radarTransform.position);
				hasMapGen = true;
			}
			List<Actor> tList = ((teamsToDetect == DetectionTeams.Both) ? TargetManager.instance.allActors : ((teamsToDetect == DetectionTeams.Allied) ? TargetManager.instance.alliedUnits : TargetManager.instance.enemyUnits));
			StartCoroutine(ProcessUnitsRoutine(tList, hasMapGen, deltaTime));
		}
	}

	private IEnumerator ProcessUnitsRoutine(List<Actor> tList, bool hasMapGen, float deltaTime)
	{
		float num = 1.05f * deltaTime * rotationSpeed;
		float dotThresh = Mathf.Cos(num * ((float)Math.PI / 180f));
		for (int i = 0; i < tList.Count; i++)
		{
			if (ProcessUnit(tList[i], dotThresh, hasMapGen))
			{
				yield return null;
			}
		}
	}

	private bool ProcessUnit(Actor a, float dotThresh, bool hasMapGen)
	{
		if (!a || !a.gameObject.activeSelf)
		{
			return false;
		}
		if (!a.alive)
		{
			return false;
		}
		if ((bool)a.parentActor)
		{
			return false;
		}
		bool isGroundTarget = false;
		bool flag = RoleFilterTarget(a, out isGroundTarget);
		if (!flag && (a.rwrs == null || a.rwrs.Count == 0))
		{
			return false;
		}
		Vector3 position = a.position;
		float sqrMagnitude = (position - rotationTransform.position).sqrMagnitude;
		if (sqrMagnitude < rangeSqr || ADV_RADAR)
		{
			Vector3 vector = rotationTransform.InverseTransformPoint(position);
			vector.y = 0f;
			if (Vector3.Dot(vector.normalized, Vector3.forward) < dotThresh)
			{
				return false;
			}
			Quaternion localRotation = rotationTransform.localRotation;
			float y = VectorUtils.SignedAngle(rotationTransform.parent.forward, Vector3.ProjectOnPlane(position - rotationTransform.position, rotationTransform.parent.up), rotationTransform.right);
			rotationTransform.localRotation = Quaternion.Euler(0f, y, 0f);
			if (debugRadar)
			{
				Debug.DrawLine(radarTransform.position, position, Color.red, 2f);
			}
			if (Vector3.Dot((position - radarTransform.position).normalized, radarTransform.forward) > cosHalfSweepFov)
			{
				if (debugRadar)
				{
					debugString = "RDR: " + a.actorName;
				}
				bool flag2 = !hasMapGen || VTMapGenerator.fetch.IsChunkColliderEnabled(a.position);
				if (myChunkColliderEnabled && Physics.Linecast(radarTransform.position, position, out var hitInfo, 1) && (hitInfo.point - position).sqrMagnitude > 25f)
				{
					Actor componentInParent = hitInfo.collider.GetComponentInParent<Actor>();
					if (!componentInParent || componentInParent != a)
					{
						if (debugRadar)
						{
							debugString += " occluded.";
							Debug.Log(debugString);
						}
						rotationTransform.localRotation = localRotation;
						return true;
					}
				}
				if (hasMapGen && flag2 && Physics.Linecast(position, radarTransform.position, out hitInfo, 1) && (hitInfo.point - radarTransform.position).sqrMagnitude > 25f)
				{
					Actor componentInParent2 = hitInfo.collider.GetComponentInParent<Actor>();
					if (!componentInParent2 || componentInParent2 != a)
					{
						if (debugRadar)
						{
							debugString += " occluded.";
							Debug.Log(debugString);
						}
						rotationTransform.localRotation = localRotation;
						return true;
					}
				}
				if (hasMapGen && (!myChunkColliderEnabled || !flag2))
				{
					StartCoroutine(HeightmapOccludeCheck(a));
					rotationTransform.localRotation = localRotation;
					return false;
				}
				SendRadarDetectEvent(a, myActor, radarSymbol, detectionPersistanceTime, rotationTransform.position, transmissionStrength);
				if (!flag)
				{
					return false;
				}
				if (ADV_RADAR)
				{
					float radarSignalStrength = GetRadarSignalStrength(radarTransform.position, a, isGroundTarget);
					float num = transmissionStrength * radarSignalStrength / sqrMagnitude;
					if (debugRadar)
					{
						debugString = debugString + " rcs: " + radarSignalStrength + " signal: " + num;
					}
					if (num < 1f / receiverSensitivity)
					{
						if (debugRadar)
						{
							debugString += " below sensitivity.";
							Debug.Log(debugString);
						}
						rotationTransform.localRotation = localRotation;
						return true;
					}
				}
				DetectActor(a);
			}
			rotationTransform.localRotation = localRotation;
			return true;
		}
		return false;
	}

	private bool RoleFilterTarget(Actor a, out bool isGroundTarget)
	{
		isGroundTarget = false;
		if (a.finalCombatRole == Actor.Roles.Air)
		{
			if ((bool)a.flightInfo && a.flightInfo.isLanded)
			{
				isGroundTarget = true;
				if (!detectGround)
				{
					return false;
				}
			}
			else if (!detectAircraft)
			{
				return false;
			}
		}
		else if (a.role == Actor.Roles.Missile)
		{
			if (!detectMissiles)
			{
				return false;
			}
		}
		else if (a.finalCombatRole == Actor.Roles.Ship)
		{
			if (!detectShips)
			{
				return false;
			}
			isGroundTarget = true;
		}
		else
		{
			if (a.finalCombatRole != Actor.Roles.Ground && a.finalCombatRole != Actor.Roles.GroundArmor)
			{
				return false;
			}
			if (!detectGround)
			{
				return false;
			}
			isGroundTarget = true;
		}
		return true;
	}

	private IEnumerator HeightmapOccludeCheck(Actor a)
	{
		if (useHeightmapTasks)
		{
			RadarOccludeTaskResult tResult = HeightmapOccludeTask(a);
			while (!tResult.done)
			{
				yield return null;
			}
			if (tResult.occluded)
			{
				yield break;
			}
		}
		else
		{
			FixedPoint aPos = new FixedPoint(a.position);
			int maxChecksPerFrame = 5;
			int num = 0;
			float distInterval = 150f;
			FixedPoint currPos = new FixedPoint(radarTransform.position);
			_ = currPos;
			while ((aPos.point - currPos.point).sqrMagnitude > 10f)
			{
				currPos = new FixedPoint(Vector3.MoveTowards(currPos.point, aPos.point, distInterval));
				float altitude = WaterPhysics.GetAltitude(currPos.point);
				float heightmapAltitude = VTMapGenerator.fetch.GetHeightmapAltitude(currPos.point);
				if (altitude < heightmapAltitude)
				{
					yield break;
				}
				num++;
				if (num >= maxChecksPerFrame)
				{
					yield return null;
					if (!a)
					{
						yield break;
					}
					num = 0;
				}
			}
		}
		if (!a)
		{
			yield break;
		}
		SendRadarDetectEvent(a, myActor, radarSymbol, detectionPersistanceTime, rotationTransform.position, transmissionStrength);
		if (!RoleFilterTarget(a, out var _))
		{
			yield break;
		}
		bool flag = a.role == Actor.Roles.Ground || a.role == Actor.Roles.GroundArmor || a.role == Actor.Roles.Ship || ((bool)a.flightInfo && a.flightInfo.isLanded);
		float overrideGroundDist = 1f;
		if (!flag)
		{
			overrideGroundDist = WaterPhysics.GetAltitude(a.position) - VTMapGenerator.fetch.GetHeightmapAltitude(a.position);
		}
		float radarSignalStrength = GetRadarSignalStrength(radarTransform.position, a, flag, overrideGroundDist);
		float sqrMagnitude = (a.position - radarTransform.position).sqrMagnitude;
		float num2 = transmissionStrength * radarSignalStrength / sqrMagnitude;
		if (debugRadar)
		{
			debugString = debugString + " rcs: " + radarSignalStrength + " signal: " + num2;
		}
		if (num2 < 1f / receiverSensitivity)
		{
			if (debugRadar)
			{
				debugString += " below sensitivity. (HeightmapOccludeCheck)";
				Debug.Log(debugString);
			}
		}
		else
		{
			DetectActor(a);
		}
	}

	private RadarOccludeTaskResult HeightmapOccludeTask(Actor a)
	{
		RadarOccludeTaskResult result = new RadarOccludeTaskResult();
		Vector3D aPos = new FixedPoint(a.position).globalPoint;
		float distInterval = 76.8f;
		Vector3D currPos = VTMapManager.WorldToGlobalPoint(radarTransform.position);
		Task.Factory.StartNew(delegate
		{
			while ((aPos - currPos).sqrMagnitude > 10.0)
			{
				currPos = Vector3D.MoveTowards(currPos, aPos, distInterval);
				float num = (float)currPos.y + 5f;
				float heightmapAltitude = VTMapGenerator.fetch.GetHeightmapAltitude(currPos);
				if (num < heightmapAltitude)
				{
					result.occluded = true;
					result.done = true;
					return;
				}
			}
			result.occluded = false;
			result.done = true;
		});
		return result;
	}

	public void ForceDetect(Actor a)
	{
		if ((bool)a)
		{
			if (ADV_RADAR)
			{
				SendRadarDetectEvent(a, myActor, radarSymbol, detectionPersistanceTime, rotationTransform.position, transmissionStrength);
			}
			DetectActor(a);
		}
	}

	private void DetectActor(Actor a)
	{
		if (!a || mpRemote)
		{
			return;
		}
		if ((bool)myActor)
		{
			if (!a.discovered)
			{
				Teams teams = Teams.Allied;
				if (VTOLMPUtils.IsMultiplayer())
				{
					teams = VTOLMPLobbyManager.localPlayerInfo.team;
				}
				if (myActor.team == teams)
				{
					a.DiscoverActor();
				}
			}
			if ((myActor.team == Teams.Allied && !a.detectedByAllied) || (myActor.team == Teams.Enemy && !a.detectedByEnemy))
			{
				Debug.Log(myActor.actorName + " detected " + a.actorName + " via radar.");
			}
			a.DetectActor(myActor.team, myActor);
		}
		int actorID = a.actorID;
		if (removalRoutines.ContainsKey(actorID))
		{
			StopCoroutine(removalRoutines[actorID]);
			removalRoutines[actorID] = StartCoroutine(RemoveAfterTime(a, detectionPersistanceTime));
		}
		else
		{
			detectedUnits.Add(a);
			removalRoutines.Add(actorID, StartCoroutine(RemoveAfterTime(a, detectionPersistanceTime)));
		}
		if (this.OnDetectedActor != null)
		{
			this.OnDetectedActor(a);
		}
	}

	public static float GetRadarSignalStrength(Vector3 radarPosition, Actor target, bool isGroundTarget, float overrideGroundDist = -1f)
	{
		Vector3 viewDir = target.position - radarPosition;
		Vector3 normalized = viewDir.normalized;
		float radarCrossSection = target.GetRadarCrossSection(viewDir);
		float a = 1f;
		float groundDist = 0f;
		int num;
		if (!target.flightInfo || !target.flightInfo.isLanded)
		{
			if (!isGroundTarget)
			{
				num = (DoDopplerFactor(radarPosition, target.position, out groundDist) ? 1 : 0);
				if (num != 0)
				{
					goto IL_005e;
				}
			}
			else
			{
				num = 0;
			}
			goto IL_0095;
		}
		num = 1;
		goto IL_005e;
		IL_0095:
		float t = Vector3.Dot(normalized, Vector3.up);
		a = Mathf.Lerp(a, 1.5f, t);
		radarCrossSection *= a;
		if (num != 0)
		{
			if (overrideGroundDist > 0f)
			{
				groundDist = overrideGroundDist;
			}
			float t2 = Mathf.InverseLerp(25000f, 500f, groundDist);
			t2 = Mathf.Lerp(0.2f, 1f, t2);
			radarCrossSection *= t2;
		}
		return radarCrossSection;
		IL_005e:
		a = Mathf.Abs(Vector3.Dot(Vector3.ClampMagnitude(target.velocity / 100f, 1.5f), normalized));
		a = Mathf.Clamp(a, 0.1f, 1.5f);
		goto IL_0095;
	}

	public static float GetRadarSignalStrength(Vector3 radarPosition, float targetRcs, Vector3 targetPosition, Vector3 targetVelocity, bool isGroundTarget, bool forceDoppler = false)
	{
		Vector3 normalized = (targetPosition - radarPosition).normalized;
		float num = targetRcs;
		float a = 1f;
		float groundDist = 0f;
		int num2;
		if (!forceDoppler)
		{
			if (!isGroundTarget)
			{
				num2 = (DoDopplerFactor(radarPosition, targetPosition, out groundDist) ? 1 : 0);
				if (num2 != 0)
				{
					goto IL_0039;
				}
			}
			else
			{
				num2 = 0;
			}
			goto IL_006b;
		}
		num2 = 1;
		goto IL_0039;
		IL_006b:
		float t = Vector3.Dot(normalized, Vector3.up);
		a = Mathf.Lerp(a, 1.5f, t);
		num *= a;
		if (num2 != 0)
		{
			float t2 = Mathf.InverseLerp(25000f, 500f, groundDist);
			t2 = Mathf.Lerp(0.2f, 1f, t2);
			num *= t2;
		}
		return num;
		IL_0039:
		a = Mathf.Abs(Vector3.Dot(Vector3.ClampMagnitude(targetVelocity / 100f, 1.5f), normalized));
		a = Mathf.Clamp(a, 0.1f, 1.5f);
		goto IL_006b;
	}

	private static bool DoDopplerFactor(Vector3 radarPosition, Vector3 targetPosition, out float groundDist)
	{
		float num = 25000f;
		Vector3 normalized = (targetPosition - radarPosition).normalized;
		Ray ray = new Ray(targetPosition, normalized);
		_ = (targetPosition - radarPosition).magnitude;
		RaycastHit hitInfo;
		if (WaterPhysics.instance.waterPlane.Raycast(ray, out var enter))
		{
			groundDist = enter;
			if (groundDist < num)
			{
				return true;
			}
		}
		else if (Physics.Raycast(ray, out hitInfo, num, 1))
		{
			groundDist = hitInfo.distance;
			return true;
		}
		groundDist = -1f;
		return false;
	}

	private void OnDisable()
	{
		detectedUnits.Clear();
	}

	private IEnumerator RemoveAfterTime(Actor a, float t)
	{
		int id = a.actorID;
		for (float x = 0f; x < t; x += Time.deltaTime)
		{
			yield return null;
		}
		detectedUnits.Remove(a);
		removalRoutines.Remove(id);
	}

	public void RemoteAddActor(Actor a, float t)
	{
		int actorID = a.actorID;
		if (removalRoutines.ContainsKey(actorID))
		{
			StopCoroutine(removalRoutines[actorID]);
			removalRoutines[actorID] = StartCoroutine(RemoveAfterTime(a, detectionPersistanceTime));
		}
		else
		{
			detectedUnits.Add(a);
			removalRoutines.Add(actorID, StartCoroutine(RemoveAfterTime(a, detectionPersistanceTime)));
		}
	}

	public static void SendRadarDetectEvent(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength)
	{
		if (!detectedActor || detectedActor.rwrs == null)
		{
			return;
		}
		for (int i = 0; i < detectedActor.rwrs.Count; i++)
		{
			if ((bool)detectedActor.rwrs[i])
			{
				detectedActor.rwrs[i].Radar_OnDetect(detectedActor, sourceActor, radarSymbol, persistTime, radarPosition, signalStrength);
			}
		}
	}

	public static void SendRadarLockEvent(Actor detectedActor, Actor sourceActor, string radarSymbol, float persistTime, Vector3 radarPosition, float signalStrength)
	{
		if (!detectedActor || detectedActor.rwrs == null)
		{
			return;
		}
		for (int i = 0; i < detectedActor.rwrs.Count; i++)
		{
			if ((bool)detectedActor.rwrs[i])
			{
				detectedActor.rwrs[i].Radar_OnLockPing(detectedActor, sourceActor, radarSymbol, persistTime, radarPosition, signalStrength);
			}
		}
		if (Radar.OnLockPing != null)
		{
			Radar.OnLockPing(detectedActor, sourceActor, radarSymbol, persistTime, radarPosition, signalStrength);
		}
	}

	public static float EstimateDetectionDistance(float rcs, Radar radar)
	{
		return EstimateDetectionDistance(rcs, radar.transmissionStrength, radar.receiverSensitivity);
	}

	public static float EstimateDetectionDistance(float rcs, float transmissionStrength, float receiverSensitivity)
	{
		return Mathf.Sqrt(rcs * transmissionStrength * receiverSensitivity);
	}
}
