using System;
using System.Collections;
using UnityEngine;

public class RefuelPlane : MonoBehaviour
{
	public Actor actor;

	public ModuleTurret boomTurret;

	public float maxRange;

	public float minRange;

	public float maxExtension;

	public Transform connectorTransform;

	public float connectorMoveSpeed = 6f;

	public float readyDelayTime = 3f;

	private bool refueling;

	private bool readyToAim = true;

	private RefuelPort _tPort;

	private bool mpRemote;

	private bool yawTfIsChild;

	private RefuelPort refuelReservation;

	public Transform refuelPositionTransform;

	public AIPilot aiPilot;

	public RefuelGuideLights guideLights;

	private bool hasReset;

	private bool remoteReady;

	private bool localReady;

	public RefuelPort targetRefuelPort
	{
		get
		{
			return _tPort;
		}
		private set
		{
			if (_tPort != value)
			{
				_tPort = value;
				this.OnSetRefuelPort?.Invoke(_tPort);
			}
		}
	}

	public bool hasTargetRefuelPort => targetRefuelPort != null;

	public event Action<RefuelPort> OnSetRefuelPort;

	public event Action<bool> OnAIPilotReady;

	public void SetToRemote()
	{
		mpRemote = true;
	}

	public void RemoteSetTargetPort(RefuelPort p)
	{
		targetRefuelPort = p;
		if ((bool)p)
		{
			if ((bool)guideLights)
			{
				guideLights.BeginGuiding(targetRefuelPort.transform);
			}
			return;
		}
		boomTurret.ReturnTurretOneshot();
		if ((bool)guideLights)
		{
			guideLights.EndGuiding();
		}
	}

	public bool RequestRefuelReservation(RefuelPort port)
	{
		if (mpRemote)
		{
			return false;
		}
		if (refuelReservation == null)
		{
			refuelReservation = port;
			return true;
		}
		if (refuelReservation == port && (!targetRefuelPort || targetRefuelPort == port))
		{
			return true;
		}
		if (refuelReservation.fuelTank.fuelFraction < 0.0001f || !refuelReservation.actor.alive)
		{
			refuelReservation = null;
		}
		return false;
	}

	public void CancelReservation(RefuelPort port)
	{
		if (port == refuelReservation)
		{
			refuelReservation = null;
		}
	}

	private void Awake()
	{
		if (!aiPilot)
		{
			aiPilot = GetComponent<AIPilot>();
		}
		if (!boomTurret)
		{
			return;
		}
		Transform[] componentsInChildren = boomTurret.pitchTransform.GetComponentsInChildren<Transform>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] == boomTurret.yawTransform)
			{
				yawTfIsChild = true;
			}
		}
	}

	private void OnEnable()
	{
		StartCoroutine(LookForTargetRoutine());
	}

	private void LateUpdate()
	{
		if (refueling)
		{
			return;
		}
		if ((bool)targetRefuelPort && IsAIPilotReady())
		{
			bool flag = IsInEngageRange();
			hasReset = false;
			if (readyToAim && flag)
			{
				Vector3 position = targetRefuelPort.transform.position;
				if (IsInTransferStartRange())
				{
					boomTurret.AimToTarget(position);
					boomTurret.AimToTarget(position);
					float z = connectorTransform.parent.InverseTransformPoint(position).z;
					z = Mathf.Clamp(z, 0f, maxExtension);
					connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, new Vector3(0f, 0f, z), connectorMoveSpeed * Time.deltaTime);
					if ((connectorTransform.position - position).sqrMagnitude < 0.02f)
					{
						StartCoroutine(RefuelRoutine());
					}
				}
				else
				{
					boomTurret.AimToTarget(refuelPositionTransform.position);
					float z2 = Vector3.Dot(connectorTransform.forward, refuelPositionTransform.position - connectorTransform.parent.position);
					connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, new Vector3(0f, 0f, z2), connectorMoveSpeed * Time.deltaTime);
				}
			}
			else
			{
				boomTurret.ReturnTurret();
				connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, Vector3.zero, connectorMoveSpeed * Time.deltaTime);
			}
		}
		else if (!hasReset)
		{
			boomTurret.ReturnTurret();
			if (connectorTransform.localPosition.z > 0.02f)
			{
				connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, Vector3.zero, connectorMoveSpeed * Time.deltaTime);
				return;
			}
			connectorTransform.localPosition = Vector3.zero;
			hasReset = true;
		}
	}

	private void OnDrawGizmosSelected()
	{
		if ((bool)boomTurret)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(boomTurret.yawTransform.position, minRange);
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(boomTurret.yawTransform.position, maxRange);
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(boomTurret.yawTransform.position + boomTurret.yawTransform.forward * minRange, boomTurret.yawTransform.position + boomTurret.yawTransform.forward * maxExtension);
		}
	}

	public void RemoteSetAIReady(bool r)
	{
		remoteReady = r;
	}

	public bool IsAIPilotReady()
	{
		if (mpRemote)
		{
			return remoteReady;
		}
		bool flag = actor.alive && (aiPilot.commandState == AIPilot.CommandStates.AirRefuel || aiPilot.commandState == AIPilot.CommandStates.FollowLeader || aiPilot.commandState == AIPilot.CommandStates.Navigation || aiPilot.commandState == AIPilot.CommandStates.Orbit);
		if (localReady != flag)
		{
			localReady = flag;
			this.OnAIPilotReady?.Invoke(flag);
		}
		return flag;
	}

	private bool IsInTransferRange()
	{
		if (!IsAIPilotReady())
		{
			return false;
		}
		if (!targetRefuelPort.open)
		{
			return false;
		}
		Vector3 position = targetRefuelPort.transform.position;
		float sqrMagnitude = (position - boomTurret.yawTransform.position).sqrMagnitude;
		Transform parent = boomTurret.yawTransform.parent;
		if (yawTfIsChild)
		{
			parent = boomTurret.pitchTransform.parent;
		}
		float num = Vector3.Angle(parent.forward, Vector3.ProjectOnPlane(position - boomTurret.yawTransform.position, parent.up));
		float num2 = Vector3.Angle(parent.forward, Vector3.ProjectOnPlane(position - boomTurret.yawTransform.position, parent.right));
		num2 = Mathf.Sign(Vector3.Dot(position - boomTurret.yawTransform.position, parent.up)) * num2;
		float sqrMagnitude2 = (connectorTransform.position - position).sqrMagnitude;
		if ((position - connectorTransform.parent.position).sqrMagnitude < maxExtension * maxExtension && sqrMagnitude > minRange * minRange && num < boomTurret.yawRange / 2f && num2 > boomTurret.minPitch)
		{
			return sqrMagnitude2 < 4f;
		}
		return false;
	}

	private bool IsInTransferStartRange()
	{
		float magnitude = (boomTurret.pitchTransform.position - connectorTransform.parent.position).magnitude;
		return (boomTurret.pitchTransform.position - targetRefuelPort.transform.position).magnitude - magnitude < maxExtension;
	}

	private bool IsInEngageRange()
	{
		if (!targetRefuelPort)
		{
			return false;
		}
		if (targetRefuelPort.isRemote)
		{
			if (!targetRefuelPort.remoteNeedsFuel)
			{
				return false;
			}
		}
		else if (targetRefuelPort.fuelTank.fuelFraction > 0.95f)
		{
			return false;
		}
		Vector3 position = targetRefuelPort.transform.position;
		float sqrMagnitude = (position - boomTurret.yawTransform.position).sqrMagnitude;
		Transform parent = boomTurret.yawTransform.parent;
		if (yawTfIsChild)
		{
			parent = boomTurret.pitchTransform.parent;
		}
		if (Vector3.Dot(position - boomTurret.yawTransform.position, parent.up) < 0f && sqrMagnitude < maxRange * maxRange)
		{
			return sqrMagnitude > minRange * minRange;
		}
		return false;
	}

	private IEnumerator RefuelRoutine()
	{
		refueling = true;
		targetRefuelPort.StartRefuel(this);
		bool wasSmooth = boomTurret.smoothRotation;
		boomTurret.smoothRotation = false;
		int failFrames = 0;
		while (true)
		{
			failFrames = ((!IsInTransferRange()) ? (failFrames + 1) : 0);
			if (failFrames >= 2)
			{
				break;
			}
			if (targetRefuelPort.Refuel())
			{
				refueling = false;
				targetRefuelPort.EndRefuel();
				StartCoroutine(ReadyDelayRoutine());
				if ((bool)guideLights)
				{
					guideLights.EndGuiding();
				}
				boomTurret.smoothRotation = wasSmooth;
				yield break;
			}
			Vector3 position = targetRefuelPort.transform.position;
			boomTurret.AimToTarget(position);
			boomTurret.AimToTarget(position);
			float z = connectorTransform.parent.InverseTransformPoint(position).z;
			connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, new Vector3(0f, 0f, z), connectorMoveSpeed * Time.deltaTime);
			yield return null;
		}
		refueling = false;
		targetRefuelPort.FailRefuel();
		boomTurret.smoothRotation = wasSmooth;
		StartCoroutine(ReadyDelayRoutine());
	}

	private IEnumerator ReadyDelayRoutine()
	{
		readyToAim = false;
		boomTurret.ReturnTurretOneshot();
		while (connectorTransform.localPosition.sqrMagnitude > 0.05f)
		{
			connectorTransform.localPosition = Vector3.MoveTowards(connectorTransform.localPosition, Vector3.zero, connectorMoveSpeed * Time.deltaTime);
			yield return null;
		}
		yield return new WaitForSeconds(readyDelayTime);
		readyToAim = true;
	}

	private IEnumerator LookForTargetRoutine()
	{
		yield return null;
		while (!TargetManager.instance)
		{
			yield return null;
		}
		while (base.enabled && !mpRemote)
		{
			if (refueling || !IsAIPilotReady())
			{
				yield return null;
				continue;
			}
			RefuelPort refuelPort = null;
			if ((bool)refuelReservation && refuelReservation.open)
			{
				refuelPort = refuelReservation;
			}
			float num = maxRange * maxRange;
			foreach (RefuelPort refuelPort2 in TargetManager.instance.refuelPorts)
			{
				if ((!refuelReservation || refuelPort2.isPlayer) && refuelPort2.open)
				{
					float sqrMagnitude = (refuelPort2.transform.position - connectorTransform.position).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						refuelPort = refuelPort2;
					}
				}
			}
			if ((bool)targetRefuelPort && !refuelPort)
			{
				boomTurret.ReturnTurretOneshot();
				if ((bool)guideLights)
				{
					guideLights.EndGuiding();
				}
			}
			targetRefuelPort = refuelPort;
			if ((bool)targetRefuelPort && (bool)guideLights)
			{
				guideLights.BeginGuiding(targetRefuelPort.transform);
			}
			yield return new WaitForSeconds(2f);
		}
	}
}
