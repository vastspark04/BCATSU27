using System.Collections.Generic;
using UnityEngine;

public class TurretManager : MonoBehaviour
{
	private struct TurretOrder
	{
		public ModuleTurret turret;

		public Vector3 target;

		public bool pitch;

		public bool yaw;

		public int pitchPriority;

		public int yawPriority;

		public TurretOrder(ModuleTurret turret, Vector3 target, bool pitch, bool yaw, int pitchPriority, int yawPriority)
		{
			this.turret = turret;
			this.target = target;
			this.pitch = pitch;
			this.yaw = yaw;
			this.pitchPriority = pitchPriority;
			this.yawPriority = yawPriority;
		}

		public TurretOrder(TurretOrder other, bool pitch)
		{
			turret = other.turret;
			target = other.target;
			this.pitch = pitch;
			yaw = other.yaw;
			pitchPriority = other.pitchPriority;
			yawPriority = other.yawPriority;
		}

		public TurretOrder(TurretOrder other, bool yaw, int dummy)
		{
			turret = other.turret;
			target = other.target;
			pitch = other.pitch;
			this.yaw = yaw;
			pitchPriority = other.pitchPriority;
			yawPriority = other.yawPriority;
		}
	}

	private List<TurretOrder> orderQueue = new List<TurretOrder>();

	private void FixedUpdate()
	{
		int count = orderQueue.Count;
		for (int i = 0; i < count; i++)
		{
			TurretOrder turretOrder = orderQueue[i];
			turretOrder.turret.AimToTarget(turretOrder.target, turretOrder.pitch, turretOrder.yaw, useManager: false);
		}
		orderQueue.Clear();
	}

	public void AimToTarget(ModuleTurret turret, Vector3 target, int pitchPriority, int yawPriority)
	{
		TurretOrder item = new TurretOrder(turret, target, pitch: true, yaw: true, pitchPriority, yawPriority);
		for (int i = 0; i < orderQueue.Count; i++)
		{
			if (item.pitchPriority > orderQueue[i].pitchPriority)
			{
				orderQueue[i] = new TurretOrder(orderQueue[i], pitch: false);
			}
			else if (item.pitchPriority < orderQueue[i].pitchPriority)
			{
				item.pitch = false;
			}
			if (item.yawPriority > orderQueue[i].yawPriority)
			{
				orderQueue[i] = new TurretOrder(orderQueue[i], yaw: false, 0);
			}
			else if (item.yawPriority < orderQueue[i].yawPriority)
			{
				item.yaw = false;
			}
		}
		orderQueue.Add(item);
	}
}
