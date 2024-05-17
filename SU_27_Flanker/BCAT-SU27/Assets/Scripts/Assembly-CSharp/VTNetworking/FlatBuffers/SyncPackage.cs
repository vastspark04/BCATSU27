using FlatBuffers;

namespace VTNetworking.FlatBuffers{

public struct SyncPackage : IFlatbufferObject
{
	private Table __p;

	public ByteBuffer ByteBuffer => __p.bb;

	public float Timestamp
	{
		get
		{
			int num = __p.__offset(4);
			if (num == 0)
			{
				return 0f;
			}
			return __p.bb.GetFloat(num + __p.bb_pos);
		}
	}

	public ulong OwnerID
	{
		get
		{
			int num = __p.__offset(6);
			if (num == 0)
			{
				return 0uL;
			}
			return __p.bb.GetUlong(num + __p.bb_pos);
		}
	}

	public int ItemsLength
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

	public int RpcsLength
	{
		get
		{
			int num = __p.__offset(10);
			if (num == 0)
			{
				return 0;
			}
			return __p.__vector_len(num);
		}
	}

	public static SyncPackage GetRootAsSyncPackage(ByteBuffer _bb)
	{
		return GetRootAsSyncPackage(_bb, default(SyncPackage));
	}

	public static SyncPackage GetRootAsSyncPackage(ByteBuffer _bb, SyncPackage obj)
	{
		return obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);
	}

	public void __init(int _i, ByteBuffer _bb)
	{
		__p.bb_pos = _i;
		__p.bb = _bb;
	}

	public SyncPackage __assign(int _i, ByteBuffer _bb)
	{
		__init(_i, _bb);
		return this;
	}

	public SyncItem? Items(int j)
	{
		int num = __p.__offset(8);
		if (num == 0)
		{
			return null;
		}
		return default(SyncItem).__assign(__p.__indirect(__p.__vector(num) + j * 4), __p.bb);
	}

	public Rpc? Rpcs(int j)
	{
		int num = __p.__offset(10);
		if (num == 0)
		{
			return null;
		}
		return default(Rpc).__assign(__p.__indirect(__p.__vector(num) + j * 4), __p.bb);
	}

	public static Offset<SyncPackage> CreateSyncPackage(FlatBufferBuilder builder, float timestamp = 0f, ulong ownerID = 0uL, VectorOffset itemsOffset = default(VectorOffset), VectorOffset rpcsOffset = default(VectorOffset))
	{
		builder.StartObject(4);
		AddOwnerID(builder, ownerID);
		AddRpcs(builder, rpcsOffset);
		AddItems(builder, itemsOffset);
		AddTimestamp(builder, timestamp);
		return EndSyncPackage(builder);
	}

	public static void StartSyncPackage(FlatBufferBuilder builder)
	{
		builder.StartObject(4);
	}

	public static void AddTimestamp(FlatBufferBuilder builder, float timestamp)
	{
		builder.AddFloat(0, timestamp, 0.0);
	}

	public static void AddOwnerID(FlatBufferBuilder builder, ulong ownerID)
	{
		builder.AddUlong(1, ownerID, 0uL);
	}

	public static void AddItems(FlatBufferBuilder builder, VectorOffset itemsOffset)
	{
		builder.AddOffset(2, itemsOffset.Value, 0);
	}

	public static VectorOffset CreateItemsVector(FlatBufferBuilder builder, Offset<SyncItem>[] data)
	{
		builder.StartVector(4, data.Length, 4);
		for (int num = data.Length - 1; num >= 0; num--)
		{
			builder.AddOffset(data[num].Value);
		}
		return builder.EndVector();
	}

	public static VectorOffset CreateItemsVectorBlock(FlatBufferBuilder builder, Offset<SyncItem>[] data)
	{
		builder.StartVector(4, data.Length, 4);
		builder.Add(data);
		return builder.EndVector();
	}

	public static void StartItemsVector(FlatBufferBuilder builder, int numElems)
	{
		builder.StartVector(4, numElems, 4);
	}

	public static void AddRpcs(FlatBufferBuilder builder, VectorOffset rpcsOffset)
	{
		builder.AddOffset(3, rpcsOffset.Value, 0);
	}

	public static VectorOffset CreateRpcsVector(FlatBufferBuilder builder, Offset<Rpc>[] data)
	{
		builder.StartVector(4, data.Length, 4);
		for (int num = data.Length - 1; num >= 0; num--)
		{
			builder.AddOffset(data[num].Value);
		}
		return builder.EndVector();
	}

	public static VectorOffset CreateRpcsVectorBlock(FlatBufferBuilder builder, Offset<Rpc>[] data)
	{
		builder.StartVector(4, data.Length, 4);
		builder.Add(data);
		return builder.EndVector();
	}

	public static void StartRpcsVector(FlatBufferBuilder builder, int numElems)
	{
		builder.StartVector(4, numElems, 4);
	}

	public static Offset<SyncPackage> EndSyncPackage(FlatBufferBuilder builder)
	{
		return new Offset<SyncPackage>(builder.EndObject());
	}

	public static void FinishSyncPackageBuffer(FlatBufferBuilder builder, Offset<SyncPackage> offset)
	{
		builder.Finish(offset.Value);
	}

	public static void FinishSizePrefixedSyncPackageBuffer(FlatBufferBuilder builder, Offset<SyncPackage> offset)
	{
		builder.FinishSizePrefixed(offset.Value);
	}
}

}