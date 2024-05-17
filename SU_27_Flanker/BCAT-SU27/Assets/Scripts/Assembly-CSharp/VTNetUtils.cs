using System;
using System.Collections;
using System.Text;
using UnityEngine;
using VTNetworking;

public static class VTNetUtils
{
	private class UnityExceptionReporter : MonoBehaviour
	{
		public void Run(string msg)
		{
			StartCoroutine(DestroyAfterDelay());
			throw new Exception(msg);
		}

		private IEnumerator DestroyAfterDelay()
		{
			yield return null;
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private enum ActorIdentifierTypes
	{
		Player,
		AIUnit,
		Missile
	}

	private static StringBuilder sb = new StringBuilder();

	public static void SendExceptionReport(string msg)
	{
		Debug.LogError("Sending exception report: " + msg);
		new GameObject("Exception Reporter").AddComponent<UnityExceptionReporter>().Run(msg);
	}

	public static void IntToBytes(int val, out byte a, out byte b, out byte c, out byte d)
	{
		int num = 255;
		a = (byte)(val & num);
		b = (byte)((val >> 8) & num);
		c = (byte)((val >> 16) & num);
		d = (byte)((val >> 24) & num);
	}

	public static int BytesToInt(byte a, byte b, byte c, byte d)
	{
		return 0 | a | (b << 8) | (c << 16) | (d << 24);
	}

	public static void ULongToBytes(ulong val, out byte a, out byte b, out byte c, out byte d, out byte e, out byte f, out byte g, out byte h)
	{
		ulong num = 255uL;
		a = (byte)(val & num);
		b = (byte)((val >> 8) & num);
		c = (byte)((val >> 16) & num);
		d = (byte)((val >> 24) & num);
		e = (byte)((val >> 32) & num);
		f = (byte)((val >> 40) & num);
		g = (byte)((val >> 48) & num);
		h = (byte)((val >> 56) & num);
	}

	public static ulong BytesToULong(byte a, byte b, byte c, byte d, byte e, byte f, byte g, byte h)
	{
		return 0uL | (ulong)a | ((ulong)b << 8) | ((ulong)c << 16) | ((ulong)d << 24) | ((ulong)e << 32) | ((ulong)f << 40) | ((ulong)g << 48) | ((ulong)h << 56);
	}

	public static ulong IntsToULong(int a, int b)
	{
		return 0uL | (ulong)(uint)a | (ulong)((long)b << 32);
	}

	public static void ULongToInts(ulong u, out int a, out int b)
	{
		ulong num = 4294967295uL;
		uint num2 = (uint)(u & num);
		uint num3 = (uint)((u >> 32) & num);
		a = (int)num2;
		b = (int)num3;
	}

	public static void WorldPosToNetPos(Vector3 worldPos, ref byte[] byteArr, int offset)
	{
		FloatingOrigin.instance.GetCubeShiftVector(out var x, out var y, out var z);
		IntToBytes(x, out byteArr[offset], out byteArr[offset + 1], out byteArr[offset + 2], out byteArr[offset + 3]);
		IntToBytes(y, out byteArr[offset + 4], out byteArr[offset + 5], out byteArr[offset + 6], out byteArr[offset + 7]);
		IntToBytes(z, out byteArr[offset + 8], out byteArr[offset + 9], out byteArr[offset + 10], out byteArr[offset + 11]);
		Vector3ToBytes(worldPos, ref byteArr, offset + 12);
	}

	public static Vector3 NetPosToWorldPos(ref byte[] byteArr, int offset)
	{
		int gridX = BytesToInt(byteArr[offset], byteArr[offset + 1], byteArr[offset + 2], byteArr[offset + 3]);
		int gridY = BytesToInt(byteArr[offset + 4], byteArr[offset + 5], byteArr[offset + 6], byteArr[offset + 7]);
		int gridZ = BytesToInt(byteArr[offset + 8], byteArr[offset + 9], byteArr[offset + 10], byteArr[offset + 11]);
		return FloatingOrigin.GlobalToWorldPoint(BytesToVector3(ref byteArr, offset + 12), gridX, gridY, gridZ);
	}

	public static void Vector3ToBytes(Vector3 v, ref byte[] byteArr, int offset)
	{
		byte[] bytes = BitConverter.GetBytes(v.x);
		byte[] bytes2 = BitConverter.GetBytes(v.y);
		byte[] bytes3 = BitConverter.GetBytes(v.z);
		byteArr[offset] = bytes[0];
		byteArr[offset + 1] = bytes[1];
		byteArr[offset + 2] = bytes[2];
		byteArr[offset + 3] = bytes[3];
		byteArr[offset + 4] = bytes2[0];
		byteArr[offset + 5] = bytes2[1];
		byteArr[offset + 6] = bytes2[2];
		byteArr[offset + 7] = bytes2[3];
		byteArr[offset + 8] = bytes3[0];
		byteArr[offset + 9] = bytes3[1];
		byteArr[offset + 10] = bytes3[2];
		byteArr[offset + 11] = bytes3[3];
	}

	public static Vector3 BytesToVector3(ref byte[] byteArr, int offset)
	{
		float x = BitConverter.ToSingle(byteArr, offset);
		float y = BitConverter.ToSingle(byteArr, offset + 4);
		float z = BitConverter.ToSingle(byteArr, offset + 8);
		return new Vector3(x, y, z);
	}

	public static int GetActorIdentifier(Actor a)
	{
		if (a == null)
		{
			return -1;
		}
		int num = 3;
		int num2 = 32767;
		int num3 = 8191;
		int num4 = 0;
		int num6;
		int num5;
		if (a.role == Actor.Roles.Missile)
		{
			num5 = 2;
			num6 = a.GetComponent<VTNetEntity>().entityID;
		}
		else if ((bool)a.unitSpawn)
		{
			if (a.unitSpawn is MultiplayerSpawn)
			{
				num5 = 0;
				num6 = a.unitSpawn.unitID;
			}
			else
			{
				num5 = 1;
				num6 = a.unitSpawn.unitID;
			}
		}
		else
		{
			UnitSpawn parentActorSpawn = QuicksaveManager.GetParentActorSpawn(a);
			if (!parentActorSpawn)
			{
				Debug.LogError("Tried to get an actor identifier but it was not handled: " + UIUtils.GetHierarchyString(a.gameObject));
				num5 = -1;
				return -1;
			}
			num5 = 1;
			num6 = parentActorSpawn.unitID;
			num4 = ((AIUnitSpawn)parentActorSpawn).subUnits.IndexOf(a) + 1;
		}
		return ((num5 & num) << 29) | ((num6 & num2) << 13) | (num4 & num3);
	}

	public static Actor GetActorFromIdentifier(int identifier)
	{
		if (identifier == -1)
		{
			return null;
		}
		int num = 1610612736;
		int num2 = 268427264;
		int num3 = 8191;
		ActorIdentifierTypes actorIdentifierTypes = (ActorIdentifierTypes)((identifier & num) >> 29);
		int num4 = (identifier & num2) >> 13;
		int num5 = (identifier & num3) - 1;
		switch (actorIdentifierTypes)
		{
		case ActorIdentifierTypes.Player:
		{
			UnitSpawn spawnedUnit2 = VTScenario.current.units.GetUnit(num4).spawnedUnit;
			if ((bool)spawnedUnit2)
			{
				return spawnedUnit2.actor;
			}
			return null;
		}
		case ActorIdentifierTypes.AIUnit:
		{
			UnitSpawner unit = VTScenario.current.units.GetUnit(num4);
			if ((bool)unit)
			{
				UnitSpawn spawnedUnit = unit.spawnedUnit;
				if ((bool)spawnedUnit)
				{
					if (num5 < 0)
					{
						return spawnedUnit.actor;
					}
					return ((AIUnitSpawn)spawnedUnit).subUnits[num5];
				}
				return null;
			}
			return null;
		}
		case ActorIdentifierTypes.Missile:
		{
			VTNetEntity entity = VTNetworkManager.instance.GetEntity(num4);
			if ((bool)entity)
			{
				return entity.GetComponent<Actor>();
			}
			return null;
		}
		default:
			Debug.LogError("Unhandled actor identified type: " + actorIdentifierTypes);
			return null;
		}
	}

	public static int Encode3CharString(string s)
	{
		int num = 0;
		for (int i = 0; i < 3; i++)
		{
			int num2 = s[i];
			num |= num2 << i * 10;
		}
		return num;
	}

	public static string Decode3CharString(int encoded)
	{
		sb.Clear();
		for (int i = 0; i < 3; i++)
		{
			sb.Append((char)((encoded & (1023 << i * 10)) >> i * 10));
		}
		return sb.ToString();
	}
}
