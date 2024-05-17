using System;

public class VTRPCAttribute : Attribute
{
	public int index = -1;

	public VTRPCAttribute()
	{
		index = -1;
	}

	public VTRPCAttribute(int idx)
	{
		index = idx;
	}
}
