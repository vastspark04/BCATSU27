using System;
using System.Collections.Generic;
using UnityEngine;
using VTNetworking.FlatBuffers;

namespace VTNetworking{

public struct SyncDataDownDelta : ISyncDataDown
{
	public class SDDBaseline
	{
		private Dictionary<Type, object[]> baselines = new Dictionary<Type, object[]>(3);

		public T GetLastValue<T>(int idx)
		{
			return (T)GetBaselineArray<T>(idx)[idx];
		}

		public void SetValue<T>(T val, int idx)
		{
			GetBaselineArray<T>(idx)[idx] = val;
		}

		public void SetValue(Type t, object val, int idx)
		{
			GetBaselineArray(t, idx)[idx] = val;
		}

		private object[] GetBaselineArray<T>(int idx)
		{
			Type typeFromHandle = typeof(T);
			if (baselines.TryGetValue(typeFromHandle, out var value))
			{
				if (value.Length <= idx)
				{
					object[] array = new object[idx + 1];
					int i;
					for (i = 0; i < value.Length; i++)
					{
						array[i] = value[i];
					}
					for (; i < array.Length; i++)
					{
						array[i] = default(T);
					}
					value = array;
					baselines[typeFromHandle] = array;
				}
				return value;
			}
			object[] array2 = new object[idx + 1];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = default(T);
			}
			baselines.Add(typeFromHandle, array2);
			return array2;
		}

		private object[] GetBaselineArray(Type type, int idx)
		{
			if (baselines.TryGetValue(type, out var value))
			{
				if (value.Length <= idx)
				{
					object[] array = new object[idx + 1];
					int i;
					for (i = 0; i < value.Length; i++)
					{
						array[i] = value[i];
					}
					for (; i < array.Length; i++)
					{
						if (type.IsValueType)
						{
							array[i] = Activator.CreateInstance(type);
						}
						else
						{
							array[i] = null;
						}
					}
					value = array;
					baselines[type] = array;
				}
				return value;
			}
			object[] array2 = new object[idx + 1];
			for (int j = 0; j < array2.Length; j++)
			{
				if (type.IsValueType)
				{
					array2[j] = Activator.CreateInstance(type);
				}
				else
				{
					array2[j] = null;
				}
			}
			baselines.Add(type, array2);
			return array2;
		}

		public void UploadData(SyncDataUp data)
		{
			foreach (KeyValuePair<Type, object[]> baseline in baselines)
			{
				Type key = baseline.Key;
				object[] value = baseline.Value;
				for (int i = 0; i < value.Length; i++)
				{
					if (key == typeof(float))
					{
						data.AddFloat((float)value[i]);
					}
					else if (key == typeof(int))
					{
						data.AddInt((int)value[i]);
					}
				}
			}
		}
	}

	private SyncItem syncItem;

	private SDDBaseline baseline;

	private int fDeltaMask;

	private int iDeltaMask;

	private int floatIdx;

	private int floatDeltaIdx;

	private int intIdx;

	private int intDeltaIdx;

	private int floatCount;

	private int intCount;

	public float Ping { get; private set; }

	public float Timestamp { get; private set; }

	public SyncDataDownDelta(SyncItem i, float timestamp, float ping, SDDBaseline baseline)
	{
		this.baseline = baseline;
		Ping = ping;
		syncItem = i;
		floatIdx = (floatDeltaIdx = 0);
		intIdx = (intDeltaIdx = 0);
		floatCount = i.FloatsLength;
		intCount = i.IntsLength;
		fDeltaMask = i.Ints(0);
		iDeltaMask = i.Ints(1);
		intDeltaIdx = 2;
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
		if ((fDeltaMask & (1 << floatIdx)) == 0)
		{
			return baseline.GetLastValue<float>(floatIdx++);
		}
		float num = syncItem.Floats(floatDeltaIdx++);
		baseline.SetValue(num, floatIdx++);
		return num;
	}

	public int GetNextInt()
	{
		if ((iDeltaMask & (1 << intIdx)) == 0)
		{
			return baseline.GetLastValue<int>(intIdx++);
		}
		int num = syncItem.Ints(intDeltaIdx++);
		baseline.SetValue(num, intIdx++);
		return num;
	}
}

}