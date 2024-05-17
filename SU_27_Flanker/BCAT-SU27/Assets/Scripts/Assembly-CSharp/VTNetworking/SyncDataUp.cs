using System;
using System.Collections;
using System.Collections.Generic;
using FlatBuffers;
using Steamworks;
using UnityEngine;
using VTNetworking.FlatBuffers;

namespace VTNetworking{

public class SyncDataUp
{
	private class Baseline
	{
		public List<object> values = new List<object>();

		public int currIdx;

		public int deltaMask;

		public int deltaCount;

		public void Reset()
		{
			currIdx = 0;
			deltaMask = 0;
			deltaCount = 0;
		}

		public bool HasChanged(int idx)
		{
			return (deltaMask & (1 << values.Count - 1 - idx)) != 0;
		}

		public void SetFullDeltaMask()
		{
			for (int i = 0; i < values.Count; i++)
			{
				deltaMask |= 1 << i;
			}
			deltaCount = values.Count;
		}
	}

	private Dictionary<Type, Stack> stacks;

	private bool deltaCompress;

	private bool reusable;

	private Dictionary<Type, Baseline> baselines;

	private Stack<float> reuseFStack;

	private Stack<int> reuseIStack;

	public SyncDataUp(bool deltaCompress, bool reusable = false)
	{
		this.deltaCompress = deltaCompress;
		this.reusable = reusable;
		stacks = new Dictionary<Type, Stack>();
		stacks.Add(typeof(float), new Stack());
		stacks.Add(typeof(int), new Stack());
		if (deltaCompress)
		{
			CreateBaselineLists();
		}
	}

	public string DebugStacks()
	{
		return $"float({stacks[typeof(float)].Count}), int({stacks[typeof(int)].Count})";
	}

	public void CopyToLocalBaseline(SyncDataDownDelta.SDDBaseline b)
	{
		foreach (KeyValuePair<Type, Baseline> baseline in baselines)
		{
			List<object> values = baseline.Value.values;
			Type key = baseline.Key;
			for (int i = 0; i < values.Count; i++)
			{
				b.SetValue(key, values[i], i);
			}
		}
	}

	private void CreateBaselineLists()
	{
		baselines = new Dictionary<Type, Baseline>();
		baselines.Add(typeof(float), new Baseline());
		baselines.Add(typeof(int), new Baseline());
	}

	private Baseline GetBaseline<T>()
	{
		return baselines[typeof(T)];
	}

	public void AddObject(object o)
	{
		Type type = o.GetType();
		if (type == typeof(float))
		{
			AddFloat((float)o);
			return;
		}
		if (type == typeof(int))
		{
			AddInt((int)o);
			return;
		}
		if (type == typeof(Vector3))
		{
			AddVector3((Vector3)o);
			return;
		}
		if (type == typeof(Quaternion))
		{
			AddQuaternion((Quaternion)o);
			return;
		}
		if (type == typeof(ulong))
		{
			VTNetUtils.ULongToInts((ulong)o, out var a, out var b);
			AddInt(a);
			AddInt(b);
			return;
		}
		if (type == typeof(SteamId))
		{
			ulong value = ((SteamId)o).Value;
			AddObject(value);
			return;
		}
		throw new NotSupportedException("Tried to add a sync data object with invalid type " + type.Name + "!");
	}

	public void AddFloat(float f)
	{
		Add(f);
	}

	public void AddInt(int i)
	{
		Add(i);
	}

	public void AddVector3(Vector3 v)
	{
		Add(v.x);
		Add(v.y);
		Add(v.z);
	}

	public void AddQuaternion(Quaternion q)
	{
		Vector3 eulerAngles = q.eulerAngles;
		AddVector3(eulerAngles);
	}

	private void Add<T>(T o)
	{
		if (deltaCompress)
		{
			AddDelta(o);
		}
		else
		{
			stacks[typeof(T)].Push(o);
		}
	}

	private void AddDelta<T>(T o)
	{
		Baseline baseline = GetBaseline<T>();
		if (baseline.currIdx == baseline.values.Count)
		{
			baseline.values.Add(o);
			baseline.deltaMask |= 1 << baseline.currIdx;
			baseline.deltaCount++;
		}
		else if (!IsNoChange(o, baseline.values[baseline.currIdx]))
		{
			baseline.deltaMask |= 1 << baseline.currIdx;
			baseline.values[baseline.currIdx] = o;
			baseline.deltaCount++;
		}
		baseline.currIdx++;
		stacks[typeof(T)].Push(o);
	}

	private void AddDelta(Type t, object o)
	{
		Baseline baseline = baselines[t];
		if (baseline.currIdx == baseline.values.Count)
		{
			baseline.values.Add(o);
			baseline.deltaMask |= 1 << baseline.currIdx;
			baseline.deltaCount++;
		}
		else if (!IsNoChange(o, baseline.values[baseline.currIdx]))
		{
			baseline.deltaMask |= 1 << baseline.currIdx;
			baseline.values[baseline.currIdx] = o;
			baseline.deltaCount++;
		}
		baseline.currIdx++;
		stacks[t].Push(o);
	}

	private bool IsNoChange(object a, object b)
	{
		if (a is float)
		{
			return (float)a == (float)b;
		}
		if (a is int)
		{
			return (int)a == (int)b;
		}
		return false;
	}

	private Stack GetStack<T>()
	{
		return stacks[typeof(T)];
	}

	public bool HasDelta()
	{
		foreach (Baseline value in baselines.Values)
		{
			if (value.deltaMask != 0)
			{
				return true;
			}
		}
		return false;
	}

	public Offset<SyncItem> CreateSyncItem(FlatBufferBuilder fbb, int id, bool forceAll = false, bool retrieve = false)
	{
		if (!deltaCompress)
		{
			retrieve = false;
		}
		else if (reusable)
		{
			reusable = false;
			retrieve = true;
		}
		Stack stack = GetStack<float>();
		int count = stack.Count;
		Baseline baseline = null;
		if (deltaCompress)
		{
			baseline = GetBaseline<float>();
			if (forceAll)
			{
				baseline.SetFullDeltaMask();
				if (retrieve)
				{
					count = baseline.values.Count;
				}
			}
		}
		if (reusable && reuseFStack == null)
		{
			reuseFStack = new Stack<float>();
			reuseIStack = new Stack<int>();
		}
		SyncItem.StartFloatsVector(fbb, deltaCompress ? baseline.deltaCount : count);
		for (int i = 0; i < count; i++)
		{
			float num;
			if (retrieve)
			{
				num = (float)baseline.values[i];
			}
			else
			{
				num = (float)stack.Pop();
				if (reusable)
				{
					reuseFStack.Push(num);
				}
			}
			if (forceAll || !deltaCompress || baseline.HasChanged(i))
			{
				fbb.AddFloat(num);
			}
		}
		VectorOffset floatsOffset = fbb.EndVector();
		Stack stack2 = GetStack<int>();
		int count2 = stack2.Count;
		Baseline baseline2 = null;
		if (deltaCompress)
		{
			baseline2 = GetBaseline<int>();
			if (forceAll)
			{
				baseline2.SetFullDeltaMask();
				if (retrieve)
				{
					count2 = baseline2.values.Count;
				}
			}
		}
		SyncItem.StartIntsVector(fbb, deltaCompress ? (baseline2.deltaCount + 2) : count2);
		for (int j = 0; j < count2; j++)
		{
			int num2 = ((!retrieve) ? ((int)stack2.Pop()) : ((int)baseline2.values[j]));
			if (forceAll || !deltaCompress || baseline2.HasChanged(j))
			{
				fbb.AddInt(num2);
				if (reusable)
				{
					reuseIStack.Push(num2);
				}
			}
		}
		if (deltaCompress)
		{
			fbb.AddInt(baseline2.deltaMask);
			fbb.AddInt(baseline.deltaMask);
		}
		VectorOffset intsOffset = fbb.EndVector();
		if (reusable)
		{
			while (reuseFStack.Count > 0)
			{
				stack.Push(reuseFStack.Pop());
			}
			while (reuseIStack.Count > 0)
			{
				stack2.Push(reuseIStack.Pop());
			}
		}
		SyncItem.StartSyncItem(fbb);
		SyncItem.AddId(fbb, id);
		SyncItem.AddFloats(fbb, floatsOffset);
		SyncItem.AddInts(fbb, intsOffset);
		Offset<SyncItem> result = SyncItem.EndSyncItem(fbb);
		if (deltaCompress)
		{
			ResetBaselines();
		}
		return result;
	}

	public void ClearUnused()
	{
		foreach (Stack value in stacks.Values)
		{
			value.Clear();
		}
		ResetBaselines();
	}

	private void ResetBaselines()
	{
		GetBaseline<float>().Reset();
		GetBaseline<int>().Reset();
	}
}

}