using System.Collections.Generic;

namespace OC{

public abstract class ShaderParameter
{
	private static List<ShaderParameter> parameters;

	public string name { get; protected set; }

	public ShaderParameter()
	{
		if (parameters == null)
		{
			parameters = new List<ShaderParameter>();
		}
		parameters.Add(this);
	}
}
}
