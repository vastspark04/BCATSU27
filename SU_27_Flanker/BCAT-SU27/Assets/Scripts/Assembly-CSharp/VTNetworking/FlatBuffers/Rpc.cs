using FlatBuffers;

namespace VTNetworking.FlatBuffers{

public struct Rpc : IFlatbufferObject
{
	private Table __p;

	public ByteBuffer ByteBuffer => __p.bb;

	public int FuncId
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

	public SyncItem? Params
	{
		get
		{
			int num = __p.__offset(6);
			if (num == 0)
			{
				return null;
			}
			return default(SyncItem).__assign(__p.__indirect(num + __p.bb_pos), __p.bb);
		}
	}

	public static Rpc GetRootAsRpc(ByteBuffer _bb)
	{
		return GetRootAsRpc(_bb, default(Rpc));
	}

	public static Rpc GetRootAsRpc(ByteBuffer _bb, Rpc obj)
	{
		return obj.__assign(_bb.GetInt(_bb.Position) + _bb.Position, _bb);
	}

	public void __init(int _i, ByteBuffer _bb)
	{
		__p.bb_pos = _i;
		__p.bb = _bb;
	}

	public Rpc __assign(int _i, ByteBuffer _bb)
	{
		__init(_i, _bb);
		return this;
	}

	public static Offset<Rpc> CreateRpc(FlatBufferBuilder builder, int funcId = 0, Offset<SyncItem> paramsOffset = default(Offset<SyncItem>))
	{
		builder.StartObject(2);
		AddParams(builder, paramsOffset);
		AddFuncId(builder, funcId);
		return EndRpc(builder);
	}

	public static void StartRpc(FlatBufferBuilder builder)
	{
		builder.StartObject(2);
	}

	public static void AddFuncId(FlatBufferBuilder builder, int funcId)
	{
		builder.AddInt(0, funcId, 0);
	}

	public static void AddParams(FlatBufferBuilder builder, Offset<SyncItem> paramsOffset)
	{
		builder.AddOffset(1, paramsOffset.Value, 0);
	}

	public static Offset<Rpc> EndRpc(FlatBufferBuilder builder)
	{
		return new Offset<Rpc>(builder.EndObject());
	}
}

}