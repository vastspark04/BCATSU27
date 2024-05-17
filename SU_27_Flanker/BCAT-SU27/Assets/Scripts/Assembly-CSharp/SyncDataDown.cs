using System;
using UnityEngine;
using VTNetworking.FlatBuffers;

public struct SyncDataDown : ISyncDataDown
{
	private SyncItem syncItem;

	private int floatIdx;

	private int intIdx;

	private int numFloats;

	private int numInts;

	public float Ping { get; private set; }

	public float Timestamp { get; private set; }

	public SyncDataDown(SyncItem i, float timestamp, float ping, int vectorIdx = 0, int floatIdx = 0, int intIdx = 0)
	{
		Ping = ping;
		syncItem = i;
		this.floatIdx = floatIdx;
		this.intIdx = intIdx;
		numFloats = i.FloatsLength - floatIdx;
		numInts = i.IntsLength - intIdx;
		Timestamp = timestamp;
	}

	public Vector3 GetNextVector3()
	{
		return new Vector3(GetNextFloat(), GetNextFloat(), GetNextFloat());
	}

	public ulong GetNextULong()
	{
		int nextInt = GetNextInt();
		int nextInt2 = GetNextInt();
		return VTNetUtils.IntsToULong(nextInt, nextInt2);
	}

	public Quaternion GetNextQuaternion()
	{
		return Quaternion.Euler(new Vector3(GetNextFloat(), GetNextFloat(), GetNextFloat()));
	}

	public float GetNextFloat()
	{
		numFloats--;
		return syncItem.Floats(floatIdx++);
	}

	public int GetNextInt()
	{
		numInts--;
		return syncItem.Ints(intIdx++);
	}

	public object GetReturnObject(Type returnType)
	{
		object result = null;
		if (returnType == typeof(Quaternion))
		{
			return GetNextQuaternion();
		}
		if (returnType == typeof(Vector3))
		{
			return GetNextVector3();
		}
		if (returnType == typeof(float))
		{
			return GetNextFloat();
		}
		if (returnType == typeof(int))
		{
			return GetNextInt();
		}
		Debug.Log("Tried to get the RPC return object from a SyncDataDown but it requested an invalid return type: " + returnType);
		return result;
	}
}
