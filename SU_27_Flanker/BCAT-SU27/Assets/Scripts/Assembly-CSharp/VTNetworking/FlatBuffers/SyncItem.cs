using System;
using FlatBuffers;

namespace VTNetworking.FlatBuffers{

public struct SyncItem : IFlatbufferObject
{
	private Table __p;

	public ByteBuffer ByteBuffer => __p.bb;

	public int Id
	{
		get
		{
			int num = __p.__offset(4);
			if (num == 0)
			{
				return 0;
			}
			return __p.bb.GetInt(num + __p.bb_pos);
		}
	}

	public int FloatsLength
	{
		get
		{
			int num = __p.__offset(6);
			if (num == 0)
			{
				return 0;
			}
			return __p.__vector_len(num);
		}
	}

	public int IntsLength
	{
		get
		{
			int num = __p.__offset(8);
			if (num == 0)
			{
				return 0;
			}
			return __p.__vector_len(num);
		}
	}

	public static SyncItem GetRootAsSyncItem(ByteBuffer _bb)
	{
		return GetRootAsSyncItem(_bb, default(SyncItem));
	}

	public static SyncItem GetRootAsSyncItem(ByteBuffer _bb, SyncItem obj)
	{
		return obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);
	}

	public void __init(int _i, ByteBuffer _bb)
	{
		__p.bb_pos = _i;
		__p.bb = _bb;
	}

	public SyncItem __assign(int _i, ByteBuffer _bb)
	{
		__init(_i, _bb);
		return this;
	}

	public float Floats(int j)
	{
		int num = __p.__offset(6);
		if (num == 0)
		{
			return 0f;
		}
		return __p.bb.GetFloat(__p.__vector(num) + j * 4);
	}

	public ArraySegment<byte>? GetFloatsBytes()
	{
		return __p.__vector_as_arraysegment(6);
	}

	public float[] GetFloatsArray()
	{
		return __p.__vector_as_array<float>(6);
	}

	public int Ints(int j)
	{
		int num = __p.__offset(8);
		if (num == 0)
		{
			return 0;
		}
		return __p.bb.GetInt(__p.__vector(num) + j * 4);
	}

	public ArraySegment<byte>? GetIntsBytes()
	{
		return __p.__vector_as_arraysegment(8);
	}

	public int[] GetIntsArray()
	{
		return __p.__vector_as_array<int>(8);
	}

	public static Offset<SyncItem> CreateSyncItem(FlatBufferBuilder builder, int id = 0, VectorOffset floatsOffset = default(VectorOffset), VectorOffset intsOffset = default(VectorOffset))
	{
		builder.StartObject(3);
		AddInts(builder, intsOffset);
		AddFloats(builder, floatsOffset);
		AddId(builder, id);
		return EndSyncItem(builder);
	}

	public static void StartSyncItem(FlatBufferBuilder builder)
	{
		builder.StartObject(3);
	}

	public static void AddId(FlatBufferBuilder builder, int id)
	{
		builder.AddInt(0, id, 0);
	}

	public static void AddFloats(FlatBufferBuilder builder, VectorOffset floatsOffset)
	{
		builder.AddOffset(1, floatsOffset.Value, 0);
	}

	public static VectorOffset CreateFloatsVector(FlatBufferBuilder builder, float[] data)
	{
		builder.StartVector(4, data.Length, 4);
		for (int num = data.Length - 1; num >= 0; num--)
		{
			builder.AddFloat(data[num]);
		}
		return builder.EndVector();
	}

	public static VectorOffset CreateFloatsVectorBlock(FlatBufferBuilder builder, float[] data)
	{
		builder.StartVector(4, data.Length, 4);
		builder.Add(data);
		return builder.EndVector();
	}

	public static void StartFloatsVector(FlatBufferBuilder builder, int numElems)
	{
		builder.StartVector(4, numElems, 4);
	}

	public static void AddInts(FlatBufferBuilder builder, VectorOffset intsOffset)
	{
		builder.AddOffset(2, intsOffset.Value, 0);
	}

	public static VectorOffset CreateIntsVector(FlatBufferBuilder builder, int[] data)
	{
		builder.StartVector(4, data.Length, 4);
		for (int num = data.Length - 1; num >= 0; num--)
		{
			builder.AddInt(data[num]);
		}
		return builder.EndVector();
	}

	public static VectorOffset CreateIntsVectorBlock(FlatBufferBuilder builder, int[] data)
	{
		builder.StartVector(4, data.Length, 4);
		builder.Add(data);
		return builder.EndVector();
	}

	public static void StartIntsVector(FlatBufferBuilder builder, int numElems)
	{
		builder.StartVector(4, numElems, 4);
	}

	public static Offset<SyncItem> EndSyncItem(FlatBufferBuilder builder)
	{
		return new Offset<SyncItem>(builder.EndObject());
	}
}

}