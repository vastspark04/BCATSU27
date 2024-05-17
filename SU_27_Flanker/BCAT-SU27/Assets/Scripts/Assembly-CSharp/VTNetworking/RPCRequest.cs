using System;

namespace VTNetworking{

public class RPCRequest
{
	private Type rt;

	internal int requestID;

	public Type returnType => rt;

	public bool isComplete { get; private set; }

	public object Value { get; private set; }

	public RPCRequest(Type returnType)
	{
		rt = returnType;
	}

	internal void OnResponse(object o)
	{
		Value = o;
		isComplete = true;
	}
}

}