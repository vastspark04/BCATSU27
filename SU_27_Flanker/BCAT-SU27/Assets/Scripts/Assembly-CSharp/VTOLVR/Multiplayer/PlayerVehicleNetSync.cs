using Steamworks;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

public class PlayerVehicleNetSync : VTNetSync
{
	public FlightInfo flightInfo;

	public bool sendFlaps = true;

	public SpriteRenderer debugSprite;

	public GameObject[] disableOnRemote;

	public GameObject[] enableOnRemote;

	public GameObject[] destroyOnLocal;

	public GameObject[] destroyOnRemote;

	public VehiclePart[] vehicleParts;

	public FlightControlComponent[] syncedControlOutputs;

	private Vector3 myPyr;

	private float myBrakes;

	private float myThrottle;

	private float myFlaps;

	private Vector3 rawPyr;

	public Radar detectionRadar;

	public LockingRadar lockingRadar;

	public static bool usePingCompensation = true;

	private RaySpringDamper sampleSusp;

	private PlayerInfo player;

	private bool hasInitialized;

	public static float minInterpThresh = 0.1f;

	public static float maxInterpThresh = 9f;

	public static float interpSpeedDiv = 200f;

	private bool interpolatingPos;

	public bool useFixedUpdateSync = true;

	private bool wasLanded;

	private Transform lastPlatformRoot;

	private MovingPlatform lastMP;

	private Vector3 lastLocalPos;

	private CarrierCatapult catapult;

	private float currCorrectionDist = 50f;

	private float targetCorrectionDist = 50f;

	private Vector3 syncedPos;

	private Vector3 syncedVel;

	private Vector3 syncedAccel;

	private Quaternion syncedRot;

	private Vector3 syncedPyr;

	private float syncedFlaps;

	private float syncedBrakes;

	private float syncedThrottle;

	private Collider[] colliders;

	private bool remoteKilled;

	private Rigidbody rb => flightInfo.rb;

	public void SetPitchYawRoll(Vector3 pyr)
	{
		if (base.isMine)
		{
			myPyr = pyr;
		}
	}

	public void SetBrakes(float brakes)
	{
		if (base.isMine)
		{
			myBrakes = brakes;
		}
	}

	public void SetThrottle(float t)
	{
		if (base.isMine)
		{
			myThrottle = t;
		}
	}

	public void SetFlaps(float f)
	{
		if (base.isMine)
		{
			myFlaps = f;
		}
	}

	public void SetRawInputPYR(Vector3 pitchYawRoll)
	{
		rawPyr = pitchYawRoll;
	}

	protected override void Awake()
	{
		if (base.isOffline)
		{
			disableOnRemote.SetActive(active: true);
		}
		base.Awake();
		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		colliders = GetComponentsInChildren<Collider>(includeInactive: true);
		if ((bool)debugSprite)
		{
			debugSprite.gameObject.SetActive(value: false);
		}
	}

	public void Initialize()
	{
		if (hasInitialized)
		{
			return;
		}
		hasInitialized = true;
		Actor component = GetComponent<Actor>();
		if (base.isMine || !VTOLMPUtils.IsMultiplayer())
		{
			if (VTOLMPUtils.IsMultiplayer())
			{
				Debug.Log("Initializing PlayerVehicleNetSync for " + base.netEntity.owner.Name);
			}
			FlightSceneManager.instance.playerActor = component;
			if ((bool)debugSprite)
			{
				debugSprite.gameObject.SetActive(value: false);
			}
			GetComponent<FloatingOriginShifter>().enabled = true;
			GetComponent<FlightWarnings>().enabled = true;
			disableOnRemote.SetActive(active: true);
			enableOnRemote.SetActive(active: false);
			GameObject[] array = destroyOnLocal;
			for (int i = 0; i < array.Length; i++)
			{
				Object.Destroy(array[i]);
			}
			if (VTOLMPUtils.IsMultiplayer())
			{
				Refresh(0uL);
				VTNetworkManager.instance.OnNewClientConnected += Instance_OnNewClientConnected;
			}
			sampleSusp = GetComponent<WheelsController>().suspensions[1];
		}
		else
		{
			rb.isKinematic = true;
			GetComponent<FloatingOriginShifter>().enabled = false;
			GetComponent<FlightWarnings>().enabled = false;
			GetComponent<FlightAssist>().enabled = false;
			GetComponent<VehicleMaster>().enabled = false;
			GetComponent<VTOLCollisionEffects>().enabled = false;
			GetComponent<VehicleInputManager>().enabled = false;
			GetComponent<PlayerVehicleSetup>().enabled = false;
			GetComponent<ShipController>().enabled = false;
			GetComponent<PlayerFlightLogger>().enabled = false;
			GetComponent<VisualTargetFinder>().enabled = false;
			GetComponent<MissileDetector>().enabled = false;
			GetComponent<CollisionDetector>().enabled = false;
			GetComponent<MassUpdater>().enabled = false;
			Tailhook componentInChildren = GetComponentInChildren<Tailhook>();
			if ((bool)componentInChildren)
			{
				componentInChildren.SetToRemote();
			}
			if ((bool)lockingRadar)
			{
				lockingRadar.enabled = false;
			}
			if ((bool)detectionRadar)
			{
				detectionRadar.radarEnabled = false;
			}
			disableOnRemote.SetActive(active: false);
			enableOnRemote.SetActive(active: true);
			flightInfo.PauseGCalculations();
			WheelsController component2 = GetComponent<WheelsController>();
			component2.remoteAutoSteer = true;
			RaySpringDamper[] suspensions = component2.suspensions;
			for (int i = 0; i < suspensions.Length; i++)
			{
				suspensions[i].raycastWhileKinematic = true;
			}
			component.EnableIcon();
			AtmosphericAudio[] componentsInChildren = GetComponentsInChildren<AtmosphericAudio>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].onlyOnFlybyCam = false;
			}
			AtmosphericAudioSource[] componentsInChildren2 = GetComponentsInChildren<AtmosphericAudioSource>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].specCamOnly = false;
			}
			WaterBuoyancy[] componentsInChildren3 = GetComponentsInChildren<WaterBuoyancy>(includeInactive: true);
			for (int i = 0; i < componentsInChildren3.Length; i++)
			{
				componentsInChildren3[i].health = null;
			}
			FloatingOrigin.instance.OnOriginShift += Instance_OnOriginShift;
			GameObject[] array = destroyOnRemote;
			for (int i = 0; i < array.Length; i++)
			{
				Object.Destroy(array[i]);
			}
			VTOLAutoPilot component3 = GetComponent<VTOLAutoPilot>();
			ModuleEngine[] engines = component3.engines;
			foreach (ModuleEngine moduleEngine in engines)
			{
				moduleEngine.doWarnings = false;
				if (component3.disableAutoAB)
				{
					moduleEngine.autoAB = true;
				}
			}
			if (component3.disableAutoAB)
			{
				component3.disableAutoAB = false;
			}
			component3.enabled = false;
		}
		if (VTOLMPUtils.IsMultiplayer())
		{
			SetupVehiclePartEvents();
			player = VTOLMPLobbyManager.GetPlayer(base.netEntity.ownerID);
			component.actorName = player.pilotName;
			ShipSurviveObjective component4 = GetComponent<ShipSurviveObjective>();
			if ((bool)component4)
			{
				Object.Destroy(component4);
			}
			Collider[] componentsInChildrenImplementing = base.gameObject.GetComponentsInChildrenImplementing<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildrenImplementing)
			{
				if (collider.gameObject.layer == 8)
				{
					collider.gameObject.layer = 13;
				}
			}
			Debug.Log("Spawned new player vehicle for " + base.netEntity.owner.Name);
		}
		else
		{
			base.enabled = false;
		}
	}

	private void Instance_OnNewClientConnected(SteamId obj)
	{
		Refresh(obj);
	}

	private void Start()
	{
		if (!VTOLMPUtils.IsMultiplayer())
		{
			Initialize();
		}
	}

	private void OnDestroy()
	{
		if (VTNetworkManager.hasInstance)
		{
			VTNetworkManager.instance.OnNewClientConnected -= Instance_OnNewClientConnected;
		}
		if ((bool)VTOLMPSceneManager.instance && player != null)
		{
			VTOLMPSceneManager.instance.ReportPlayerUnspawnedVehicle(player);
		}
		FloatingOrigin.instance.OnOriginShift -= Instance_OnOriginShift;
	}

	public void SetToKilledLocalState()
	{
		GetComponent<FloatingOriginShifter>().enabled = false;
		GetComponent<FlightWarnings>().enabled = false;
		GetComponent<FlightAssist>().enabled = false;
		disableOnRemote.SetActive(active: false);
		enableOnRemote.SetActive(active: true);
		flightInfo.PauseGCalculations();
		AtmosphericAudio[] componentsInChildren = GetComponentsInChildren<AtmosphericAudio>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].onlyOnFlybyCam = false;
		}
		AtmosphericAudioSource[] componentsInChildren2 = GetComponentsInChildren<AtmosphericAudioSource>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].specCamOnly = false;
		}
		GetComponentInChildren<BlackoutEffect>(includeInactive: true).enabled = false;
	}

	private void Instance_OnOriginShift(Vector3 offset)
	{
		if (!base.isMine)
		{
			syncedPos += offset;
			rb.velocity = syncedVel;
		}
	}

	private float GetInterpThreshold()
	{
		return Mathf.Lerp(minInterpThresh, maxInterpThresh, flightInfo.airspeed / interpSpeedDiv);
	}

	private void FixedUpdate()
	{
		if (!base.isMine && useFixedUpdateSync)
		{
			if (remoteKilled)
			{
				rb.isKinematic = false;
			}
			else
			{
				SyncPhysics(Time.fixedDeltaTime, rb.position, rb.rotation, out var pos, out var rot);
				rb.MovePosition(pos);
				rb.MoveRotation(rot);
				rb.velocity = syncedVel;
			}
		}
		if (!base.isMine)
		{
			return;
		}
		if (sampleSusp.isTouching)
		{
			if (wasLanded)
			{
				if ((bool)lastPlatformRoot)
				{
					float num = (lastPlatformRoot.InverseTransformPoint(rb.position) - lastLocalPos).magnitude / Time.fixedDeltaTime;
					float magnitude = sampleSusp.surfaceVelocity.magnitude;
					if (num > Mathf.Max(0.5f, magnitude * 10f))
					{
						Debug.Log(base.gameObject.name + " PVNS is correcting unexpected shift on moving platform! " + $"Detected Speed: {num}, Expected {magnitude}");
						Vector3 vector3 = (rb.position = (base.transform.position = lastPlatformRoot.TransformPoint(lastLocalPos) + rb.velocity * Time.fixedDeltaTime));
					}
					lastLocalPos = lastPlatformRoot.InverseTransformPoint(rb.position);
				}
			}
			else
			{
				lastMP = sampleSusp.touchingPlatform;
				if ((bool)lastMP)
				{
					lastPlatformRoot = lastMP.transform.root;
					lastLocalPos = lastPlatformRoot.InverseTransformPoint(rb.position);
				}
				wasLanded = true;
			}
		}
		else
		{
			lastPlatformRoot = null;
			wasLanded = false;
		}
	}

	private void Update()
	{
		if (!base.isMine && !float.IsNaN(syncedVel.x))
		{
			rb.velocity = syncedVel;
		}
	}

	private void SyncPhysics(float deltaTime, Vector3 currPos, Quaternion currRot, out Vector3 pos, out Quaternion rot)
	{
		float interpThreshold = GetInterpThreshold();
		Vector3 vector = currPos + syncedVel * deltaTime + 0.5f * deltaTime * deltaTime * syncedAccel;
		syncedVel += syncedAccel * deltaTime;
		syncedPos += 0.5f * deltaTime * deltaTime * syncedAccel + syncedVel * deltaTime;
		flightInfo.OverrideRecordedAcceleration(syncedAccel);
		if (!float.IsNaN(syncedVel.x))
		{
			rb.velocity = syncedVel;
		}
		else
		{
			Debug.LogError("syncedVel is NaN", base.gameObject);
			syncedVel = rb.velocity;
		}
		float magnitude = (syncedPos - vector).magnitude;
		float num = Mathf.Lerp(8f, 3f, syncedVel.sqrMagnitude / 6400f);
		Vector3 vector2 = Vector3.MoveTowards(vector, syncedPos, Mathf.Max(magnitude * num, magnitude * 3f) * deltaTime);
		if (!catapult)
		{
			currCorrectionDist = Mathf.MoveTowards(currCorrectionDist, targetCorrectionDist, 100f * deltaTime);
		}
		Color color;
		if ((bool)catapult)
		{
			Vector3 vector3 = base.transform.position - catapult.catapultTransform.position;
			syncedPos = (pos = catapult.catapultPosition + vector3);
			syncedVel = rb.velocity;
			color = Color.blue;
			currCorrectionDist = 500f;
		}
		else if ((magnitude > 1f && flightInfo.airspeed < 4f) || magnitude > currCorrectionDist)
		{
			Debug.LogFormat("Resetting sync tf! Airspeed: {0}, dist: {1}", flightInfo.airspeed, magnitude);
			Vector3 vector5 = (pos = (rb.position = syncedPos));
			color = Color.yellow;
		}
		else if (interpolatingPos)
		{
			pos = vector2;
			if (magnitude < interpThreshold * 0.33f)
			{
				interpolatingPos = false;
			}
			color = Color.red;
		}
		else
		{
			pos = vector;
			if (magnitude > interpThreshold)
			{
				interpolatingPos = true;
			}
			color = Color.green;
		}
		if ((bool)debugSprite && debugSprite.gameObject.activeSelf)
		{
			debugSprite.transform.position = syncedPos;
			debugSprite.color = color;
		}
		rot = Quaternion.Lerp(currRot, syncedRot, 10f * deltaTime);
	}

	private void LateUpdate()
	{
		if (base.isMine)
		{
			return;
		}
		if (Input.GetKeyDown(KeyCode.O) && (bool)debugSprite)
		{
			debugSprite.gameObject.SetActive(!debugSprite.gameObject.activeSelf);
		}
		float t = 10f * Time.deltaTime;
		myPyr = Vector3.Lerp(myPyr, syncedPyr, t);
		myBrakes = Mathf.Lerp(myBrakes, syncedBrakes, t);
		myThrottle = Mathf.Lerp(myThrottle, syncedThrottle, t);
		if (sendFlaps)
		{
			myFlaps = Mathf.Lerp(myFlaps, syncedFlaps, t);
		}
		for (int i = 0; i < syncedControlOutputs.Length; i++)
		{
			FlightControlComponent flightControlComponent = syncedControlOutputs[i];
			flightControlComponent.SetPitchYawRoll(myPyr);
			flightControlComponent.SetBrakes(myBrakes);
			flightControlComponent.SetThrottle(myThrottle);
			if (sendFlaps)
			{
				flightControlComponent.SetFlaps(myFlaps);
			}
		}
		if (!useFixedUpdateSync)
		{
			if (remoteKilled)
			{
				rb.isKinematic = false;
				return;
			}
			SyncPhysics(Time.deltaTime, base.transform.position, base.transform.rotation, out var pos, out var rot);
			base.transform.position = pos;
			base.transform.rotation = rot;
		}
	}

	public override void UploadData(SyncDataUp d)
	{
		base.UploadData(d);
		FloatingOrigin.instance.GetCubeShiftVector(out var x, out var y, out var z);
		Vector3 v = new Vector3(x, y, z);
		d.AddVector3(v);
		d.AddVector3(base.transform.position);
		d.AddVector3(rb.velocity);
		d.AddVector3(flightInfo.acceleration);
		d.AddQuaternion(rb.rotation);
		d.AddVector3(myPyr);
		d.AddFloat(myBrakes);
		d.AddFloat(myThrottle);
		if (sendFlaps)
		{
			d.AddFloat(myFlaps);
		}
		int num = 0;
		num |= (flightInfo.isLanded ? 1 : 0);
		d.AddInt(num);
	}

	protected override void OnNetInitialized()
	{
		base.OnNetInitialized();
		if (!base.isMine)
		{
			base.gameObject.SetActive(value: true);
		}
		Initialize();
		syncedPos = base.transform.position;
		syncedRot = base.transform.rotation;
		if (!base.netEntity.isMine)
		{
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			ModuleEngine[] componentsInChildren = GetComponentsInChildren<ModuleEngine>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].SetPowerImmediate(1);
			}
			PlayerInfo playerInfo = VTOLMPLobbyManager.GetPlayer(base.netEntity.ownerID);
			if (playerInfo != null)
			{
				GetComponent<Actor>().SetTeam(playerInfo.team);
			}
		}
	}

	public override void DownloadData(ISyncDataDown d)
	{
		base.DownloadData(d);
		Vector3 nextVector = d.GetNextVector3();
		syncedPos = FloatingOrigin.GlobalToWorldPoint(d.GetNextVector3(), Mathf.RoundToInt(nextVector.x), Mathf.RoundToInt(nextVector.y), Mathf.RoundToInt(nextVector.z));
		Vector3 nextVector2 = d.GetNextVector3();
		Vector3 nextVector3 = d.GetNextVector3();
		Quaternion nextQuaternion = d.GetNextQuaternion();
		if (float.IsNaN(nextVector2.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedVel)");
		}
		else
		{
			syncedVel = nextVector2;
		}
		if (float.IsNaN(nextVector3.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedAccel)");
		}
		else
		{
			syncedAccel = nextVector3;
		}
		if (float.IsNaN(nextQuaternion.x))
		{
			Debug.LogError("Received a sync message with NaN values (syncedRot)");
		}
		else
		{
			syncedRot = nextQuaternion;
		}
		syncedPyr = d.GetNextVector3();
		syncedBrakes = d.GetNextFloat();
		syncedThrottle = d.GetNextFloat();
		if (sendFlaps)
		{
			syncedFlaps = d.GetNextFloat();
		}
		if (usePingCompensation)
		{
			if (float.IsNaN(VTNetworkManager.networkTime))
			{
				Debug.LogError("networkTime is NaN");
			}
			if (float.IsNaN(d.Timestamp))
			{
				Debug.LogError("d.Timestamp is NaN");
			}
			float num = Mathf.Max(0f, VTNetworkManager.networkTime - d.Timestamp);
			syncedPos += 0.5f * syncedAccel * num * num + syncedVel * num;
			syncedVel += syncedAccel * num;
		}
		int nextInt = d.GetNextInt();
		flightInfo.RemoteSetIsLanded((nextInt & 1) == 1);
	}

	public void NetSetInvincible(bool invincible)
	{
		if (base.netEntity.isMine)
		{
			GetComponent<Health>().invincible = invincible;
			if (invincible)
			{
				SendRPC("RPC_SetInvincible");
			}
			else
			{
				SendRPC("RPC_SetNotInvincible");
			}
		}
	}

	[VTRPC]
	public void RPC_SetInvincible()
	{
		GetComponent<Health>().invincible = true;
	}

	[VTRPC]
	public void RPC_SetNotInvincible()
	{
		GetComponent<Health>().invincible = false;
	}

	public void SendNetDeathMessage()
	{
		SendRPC("RPC_Kill");
	}

	[VTRPC]
	private void RPC_Kill()
	{
		remoteKilled = true;
		rb.isKinematic = false;
		rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
		GetComponent<Health>().Kill();
		Collider[] array = colliders;
		foreach (Collider collider in array)
		{
			if ((bool)collider)
			{
				if (collider.gameObject.layer == 0)
				{
					collider.gameObject.layer = 9;
				}
				else if (collider.gameObject.layer == 8)
				{
					collider.gameObject.layer = 13;
				}
				else if (collider.gameObject.layer == 10)
				{
					collider.enabled = false;
				}
			}
		}
	}

	private void SetupVehiclePartEvents()
	{
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			int partIdx = i;
			if (base.isMine)
			{
				vehicleParts[i].OnPartDetach.AddListener(delegate
				{
					SendDetachRPC(partIdx, 0uL);
				});
				vehicleParts[i].health.OnDeath.AddListener(delegate
				{
					SendKillPartRPC(partIdx, 0uL);
				});
				vehicleParts[i].OnRepair.AddListener(delegate
				{
					SendPartRepairRPC(partIdx);
				});
			}
			else
			{
				vehicleParts[i].detachOnDeath = false;
			}
		}
	}

	private void SendPartRepairRPC(int partIdx)
	{
		SendRPC("RPC_PartRepair", partIdx);
	}

	[VTRPC]
	private void RPC_PartRepair(int idx)
	{
		vehicleParts[idx].Repair();
	}

	public void SendKillPartRPC(int partIdx, ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_PartKill", partIdx);
	}

	[VTRPC]
	private void RPC_PartKill(int idx)
	{
		vehicleParts[idx].RemoteKill(null);
	}

	public void SendDetachRPC(int partIdx, ulong target = 0uL)
	{
		SendDirectedRPC(target, "RPC_PartDetach", partIdx);
	}

	[VTRPC]
	private void RPC_PartDetach(int idx)
	{
		vehicleParts[idx].RemoteDetachPart();
	}

	private void Refresh(ulong target = 0uL)
	{
		if (!base.isMine)
		{
			return;
		}
		for (int i = 0; i < vehicleParts.Length; i++)
		{
			if (vehicleParts[i].health.normalizedHealth == 0f)
			{
				SendKillPartRPC(i, target);
			}
			if (vehicleParts[i].hasDetached)
			{
				SendDetachRPC(i, target);
			}
		}
	}
}

}