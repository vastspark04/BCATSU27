using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer{

	public class TurretSync : VTNetSyncRPCOnly
	{
		public ModuleTurret turret;

		public bool multiUserVehicle;

		public float sendInterval = 0.2f;

		public float remoteLerpRate = 7f;

		private MultiUserVehicleSync muvs;

		private bool hasListened;

		private bool aimDirty;

		private Vector3 rDir = Vector3.forward;

		private float timeDirRecvd;

	}

}