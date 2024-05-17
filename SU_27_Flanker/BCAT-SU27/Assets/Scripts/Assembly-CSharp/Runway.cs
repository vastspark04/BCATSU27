using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Runway : MonoBehaviour
{
	public enum ParallelDesignations
	{
		None,
		Left,
		Center,
		Right
	}

	private Actor parentActor;

	public bool arrestor;

	public bool landing = true;

	public bool takeoff = true;

	public bool shortTakeOff;

	public Transform landingQueueOrbitTf;

	public float landingQueueRadius = 4000f;

	public float landingQueueAltitude = 1600f;

	public FollowPath[] postLandTaxiPaths;

	public float maxMass = 400f;

	private int postLandPathIdx;

	public FollowPath[] takeoffTaxiPaths;

	[HideInInspector]
	public int takeoffRequests;

	public Bounds clearanceBounds;

	public Bounds shortHoldTriggerBounds;

	public OpticalLandingSystem ols;

	[HideInInspector]
	public AirportManager airport;

	public List<Runway> sharedRunways = new List<Runway>();

	public GameObject[] activateOnLandingUsage;

	private Actor lightingObjectsRequester;

	private Coroutine lightingObjectsRoutine;

	public ParallelDesignations parallelDesignation;

	private List<Actor> runwayUsers = new List<Actor>();

	private Dictionary<int, float> yAccum = new Dictionary<int, float>();

	private static List<Actor> runwayClearActorBuffer = new List<Actor>();

	public float GetFinalQueueAltitude()
	{
		return WaterPhysics.GetAltitude(base.transform.position) + landingQueueAltitude;
	}

	private void Start()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene += OnExitScene;
		}
	}

	private void OnDestroy()
	{
		if ((bool)FlightSceneManager.instance)
		{
			FlightSceneManager.instance.OnExitScene -= OnExitScene;
		}
	}

	private void OnExitScene()
	{
		ForceHideLightObjects();
	}

	public void ShowLandingLightObjects(Actor requester)
	{
		if (activateOnLandingUsage == null)
		{
			return;
		}
		lightingObjectsRequester = requester;
		activateOnLandingUsage.SetActive(active: true);
		if (lightingObjectsRoutine != null)
		{
			StopCoroutine(lightingObjectsRoutine);
		}
		lightingObjectsRoutine = StartCoroutine(LandingObjectsRoutine());
		foreach (Runway sharedRunway in sharedRunways)
		{
			sharedRunway.ForceHideLightObjects();
		}
	}

	public void ShowTakeoffLightObjects(Actor requester)
	{
		if (activateOnLandingUsage == null)
		{
			return;
		}
		lightingObjectsRequester = requester;
		activateOnLandingUsage.SetActive(active: true);
		if (lightingObjectsRoutine != null)
		{
			StopCoroutine(lightingObjectsRoutine);
		}
		lightingObjectsRoutine = StartCoroutine(TakeOffObjsRoutine());
		foreach (Runway sharedRunway in sharedRunways)
		{
			sharedRunway.ForceHideLightObjects();
		}
	}

	public void HideLightObjects(Actor requester)
	{
		if (activateOnLandingUsage != null && (requester == lightingObjectsRequester || lightingObjectsRequester == null))
		{
			lightingObjectsRequester = null;
			activateOnLandingUsage.SetActive(active: false);
		}
	}

	private void ForceHideLightObjects()
	{
		if (activateOnLandingUsage != null)
		{
			lightingObjectsRequester = null;
			activateOnLandingUsage.SetActive(active: false);
		}
	}

	private IEnumerator LandingObjectsRoutine()
	{
		while (lightingObjectsRequester != null)
		{
			if (!lightingObjectsRequester.alive || ((bool)lightingObjectsRequester.flightInfo && lightingObjectsRequester.flightInfo.isLanded && lightingObjectsRequester.flightInfo.surfaceSpeed < 5f))
			{
				HideLightObjects(lightingObjectsRequester);
				break;
			}
			yield return null;
		}
		lightingObjectsRoutine = null;
	}

	private IEnumerator TakeOffObjsRoutine()
	{
		while (lightingObjectsRequester != null)
		{
			if (!lightingObjectsRequester.alive || ((bool)lightingObjectsRequester.flightInfo && !lightingObjectsRequester.flightInfo.isLanded && lightingObjectsRequester.flightInfo.radarAltitude < 50f))
			{
				HideLightObjects(lightingObjectsRequester);
				break;
			}
			yield return null;
		}
		lightingObjectsRoutine = null;
	}

	public bool IsRunwayUsageAuthorized(Actor a)
	{
		ClearDeadUsers();
		bool flag = runwayUsers == null || runwayUsers.Count == 0 || runwayUsers[0] == a;
		if (flag)
		{
			foreach (Runway sharedRunway in sharedRunways)
			{
				if (flag && sharedRunway.runwayUsers != null)
				{
					sharedRunway.ClearDeadUsers();
					if (sharedRunway.runwayUsers.Count > 0)
					{
						flag = flag && sharedRunway.runwayUsers[0] == a;
					}
				}
			}
			return flag;
		}
		return flag;
	}

	private void ClearDeadUsers()
	{
		if (runwayUsers.Count > 0 && !IsValidRunwayUser(runwayUsers[0]))
		{
			runwayUsers.RemoveAll((Actor x) => !IsValidRunwayUser(x));
		}
	}

	private bool IsValidRunwayUser(Actor a)
	{
		if (!a)
		{
			return false;
		}
		if (!a.alive)
		{
			Debug.Log(base.gameObject.name + ": Removing user " + a.gameObject.name + " beacuse dead.");
			return false;
		}
		AIPilot component = a.GetComponent<AIPilot>();
		if ((bool)component && (component.commandState != AIPilot.CommandStates.Override || component.commandState != AIPilot.CommandStates.Override) && (!component.targetRunway || !component.targetRunway.clearanceBounds.Contains(component.targetRunway.transform.InverseTransformPoint(a.position))))
		{
			Debug.LogFormat("{0}: Removing user {1} beacuse commandState == {2} and user is not on the runway.", base.gameObject.name, a.DebugName(), component.commandState);
			return false;
		}
		return true;
	}

	public Actor GetAuthorizedUser()
	{
		ClearDeadUsers();
		if (runwayUsers != null && runwayUsers.Count > 0)
		{
			return runwayUsers[0];
		}
		foreach (Runway sharedRunway in sharedRunways)
		{
			if (sharedRunway.runwayUsers != null && sharedRunway.runwayUsers.Count > 0)
			{
				return sharedRunway.runwayUsers[0];
			}
		}
		return null;
	}

	public int UsageQueueCount()
	{
		return runwayUsers.Count;
	}

	public void RegisterUsageRequest(Actor a)
	{
		if (!runwayUsers.Contains(a))
		{
			runwayUsers.Add(a);
		}
		foreach (Runway sharedRunway in sharedRunways)
		{
			if (!sharedRunway.runwayUsers.Contains(a))
			{
				sharedRunway.runwayUsers.Add(a);
			}
		}
	}

	private bool IsCurrentlyUsingRunway(Actor a)
	{
		if (!shortHoldTriggerBounds.Contains(base.transform.InverseTransformPoint(a.position)))
		{
			if (runwayUsers.Count > 0 && runwayUsers[0] == a)
			{
				return !a.flightInfo.isLanded;
			}
			return false;
		}
		return true;
	}

	public void RegisterUsageRequestHighPriority(Actor a)
	{
		ClearDeadUsers();
		if (runwayUsers.Contains(a))
		{
			if (runwayUsers[0] == a)
			{
				return;
			}
			int num = runwayUsers.IndexOf(a);
			for (int i = 0; i < runwayUsers.Count; i++)
			{
				if (i != num && !IsCurrentlyUsingRunway(runwayUsers[i]))
				{
					Actor value = runwayUsers[i];
					runwayUsers[i] = a;
					runwayUsers[num] = value;
					break;
				}
			}
			{
				foreach (Runway sharedRunway in sharedRunways)
				{
					sharedRunway.runwayUsers = runwayUsers.Copy();
				}
				return;
			}
		}
		bool flag = false;
		if (runwayUsers.Count >= 2)
		{
			for (int j = 0; j < runwayUsers.Count; j++)
			{
				if (IsCurrentlyUsingRunway(runwayUsers[j]))
				{
					continue;
				}
				runwayUsers.Insert(j, a);
				flag = true;
				foreach (Runway sharedRunway2 in sharedRunways)
				{
					sharedRunway2.runwayUsers.Insert(j, a);
				}
				break;
			}
		}
		if (flag)
		{
			return;
		}
		runwayUsers.Add(a);
		foreach (Runway sharedRunway3 in sharedRunways)
		{
			sharedRunway3.runwayUsers.Add(a);
		}
	}

	public void UnregisterUsageRequest(Actor a)
	{
		runwayUsers.Remove(a);
		foreach (Runway sharedRunway in sharedRunways)
		{
			sharedRunway.runwayUsers.Remove(a);
		}
	}

	public void ClearAll()
	{
		if (runwayUsers != null)
		{
			runwayUsers.Clear();
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position, base.transform.rotation, base.transform.lossyScale);
		Gizmos.color = new Color(0.6f, 0.4f, 0.1f, 0.35f);
		Gizmos.DrawCube(clearanceBounds.center, clearanceBounds.size);
		Gizmos.DrawWireCube(clearanceBounds.center, clearanceBounds.size);
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(shortHoldTriggerBounds.center, shortHoldTriggerBounds.size);
		Gizmos.matrix = Matrix4x4.identity;
	}

	public FollowPath GetNextPostLandPath()
	{
		FollowPath result = postLandTaxiPaths[postLandPathIdx];
		postLandPathIdx = (postLandPathIdx + 1) % postLandTaxiPaths.Length;
		return result;
	}

	private void OnEnable()
	{
		if (!landingQueueOrbitTf)
		{
			landingQueueOrbitTf = base.transform;
		}
	}

	private void Awake()
	{
		parentActor = GetComponentInParent<Actor>();
		if (activateOnLandingUsage != null)
		{
			activateOnLandingUsage.SetActive(active: false);
		}
	}

	public Vector3 GetGuidedLandingPoint(int planeID, Vector3 currentPosition, Vector3 currentVelocity, float glideSlope)
	{
		if (!yAccum.ContainsKey(planeID))
		{
			yAccum.Add(planeID, 0f);
		}
		Vector3 vector = base.transform.InverseTransformPoint(currentPosition);
		vector.y -= 3f;
		Vector3 vector2 = base.transform.InverseTransformVector(currentVelocity);
		float x = 0f - vector.x - 0.5f * vector2.x;
		Vector3 vector3 = Quaternion.AngleAxis(glideSlope, Vector3.right) * Vector3.forward;
		Vector3 vector4 = Vector3.Cross(vector3, Vector3.right);
		Vector3 rhs = Vector3.Project(vector2, vector4);
		Vector3 rhs2 = Vector3.Project(vector, vector4);
		float num = rhs.magnitude * Mathf.Sign(Vector3.Dot(vector4, rhs));
		float num2 = rhs2.magnitude * Mathf.Sign(Vector3.Dot(vector4, rhs2));
		yAccum[planeID] = Mathf.Clamp(yAccum[planeID] + 2f * num2 * Time.deltaTime, -5f, 1f);
		num2 += 3f * yAccum[planeID];
		float num3 = -3f * num2 - 0.75f * num;
		Vector3 position = Vector3.Project(vector, vector3) + vector3 * 1000f;
		position.x = x;
		position.y += num3;
		return base.transform.TransformPoint(position);
	}

	public Vector3 GetGuidedLandingPoint(Vector3 currentPosition, Vector3 currentVelocity, float glideSlope, PID landingHorizPID, PID landingVertPID)
	{
		Vector3 vector = base.transform.InverseTransformPoint(currentPosition);
		vector.y -= 3f;
		float magnitude = vector.magnitude;
		if ((bool)parentActor && magnitude < 5000f && Vector3.Dot(parentActor.velocity.normalized, currentVelocity.normalized) > 0.8f)
		{
			float magnitude2 = (parentActor.velocity - currentVelocity).magnitude;
			float num = magnitude / magnitude2;
			float num2 = Mathf.Lerp(2f, 0f, magnitude / 5000f);
			vector -= num2 * num * base.transform.InverseTransformVector(parentActor.velocity);
		}
		float x = landingHorizPID.Evaluate(vector.x, 0f);
		Vector3 vector2 = Quaternion.AngleAxis(glideSlope, Vector3.right) * Vector3.forward;
		Vector3 vector3 = Vector3.Cross(vector2, Vector3.right);
		Vector3 rhs = Vector3.Project(vector, vector3);
		float current = rhs.magnitude * Mathf.Sign(Vector3.Dot(vector3, rhs));
		float num3 = landingVertPID.Evaluate(current, 0f);
		Vector3 position = Vector3.Project(vector, vector2) + vector2 * 1000f;
		position.x = x;
		position.y += num3;
		return base.transform.TransformPoint(position);
	}

	public FollowPath GetTakeoffTaxiPath(Transform pilotTransform)
	{
		Vector3 position = pilotTransform.position;
		FollowPath result = null;
		float num = float.MaxValue;
		for (int i = 0; i < takeoffTaxiPaths.Length; i++)
		{
			FollowPath followPath = takeoffTaxiPaths[i];
			Vector3 worldPoint = followPath.GetWorldPoint(followPath.GetClosestTime(position));
			if (Vector3.Dot(pilotTransform.forward, worldPoint - position) > 0f || (position - worldPoint).sqrMagnitude < 64f)
			{
				float sqrMagnitude = (worldPoint - position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = followPath;
				}
			}
		}
		return result;
	}

	public ConfigNode SaveToQsNode(string nodeName)
	{
		ConfigNode configNode = new ConfigNode(nodeName);
		foreach (Actor runwayUser in runwayUsers)
		{
			configNode.AddNode(QuicksaveManager.SaveActorIdentifierToNode(runwayUser, "runwayUser"));
		}
		return configNode;
	}

	public void LoadFromQsNode(ConfigNode node)
	{
		runwayUsers.Clear();
		foreach (ConfigNode node2 in node.GetNodes("runwayUser"))
		{
			Actor actor = QuicksaveManager.RetrieveActorFromNode(node2);
			if ((bool)actor)
			{
				runwayUsers.Add(actor);
			}
		}
	}

	public bool IsRunwayClear(Actor ignoreActor)
	{
		Actor.GetActorsInRadius(base.transform.TransformPoint(clearanceBounds.center), clearanceBounds.extents.z * 1.2f, Teams.Allied, TeamOptions.BothTeams, runwayClearActorBuffer);
		for (int i = 0; i < runwayClearActorBuffer.Count; i++)
		{
			Actor actor = runwayClearActorBuffer[i];
			if ((bool)actor && actor.finalCombatRole == Actor.Roles.Air && actor != ignoreActor)
			{
				Vector3 point = base.transform.InverseTransformPoint(actor.position + actor.velocity);
				if (clearanceBounds.Contains(point))
				{
					return false;
				}
			}
		}
		if ((bool)airport)
		{
			for (int j = 0; j < airport.landingPads.Length; j++)
			{
				Transform transform = airport.landingPads[j];
				if (clearanceBounds.Contains(base.transform.InverseTransformPoint(transform.position)))
				{
					AirportManager.ParkingSpace parkingSpaceFromLandingPadIdx = airport.GetParkingSpaceFromLandingPadIdx(j);
					if ((bool)parkingSpaceFromLandingPadIdx.occupiedBy && (bool)parkingSpaceFromLandingPadIdx.occupiedBy.flightInfo && !parkingSpaceFromLandingPadIdx.occupiedBy.flightInfo.isLanded)
					{
						return false;
					}
				}
			}
		}
		return true;
	}
}
