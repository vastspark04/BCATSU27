using System.Collections;
using UnityEngine;
using VTNetworking;

namespace VTOLVR.Multiplayer
{
	public class GunSync : VTNetSyncRPCOnly
	{
		public Gun gun;

		[Tooltip("Used for getting the name of the weapon for kill credits")]
		public HPEquippable equip;

		public string customKillCreditName;

		private MultiUserVehicleSync muvs;

		private bool useMuvs;

		private bool listenedGunMuvs;

		private bool awoke;

		private bool listenedGun;

		private bool destroyed;

		private float timeReceivedBullet;

		private float interFrameT;

		private bool sentFiring;

		private const int RPC_F_T = 0;

		private const int RPC_F_F = 1;

		private int remoteFireTfIdx;

		private const int RPC_B = 2;

		private bool resettingInterframe;

		private WaitForEndOfFrame eofWait = new WaitForEndOfFrame();

		private bool checkingTimeout;

		private const int RPC_SET_AMMO = 3;

		private Actor actor => gun.actor;

	}
}
