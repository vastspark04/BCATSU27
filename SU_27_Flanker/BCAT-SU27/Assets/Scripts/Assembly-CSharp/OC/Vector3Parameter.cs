using UnityEngine;

namespace OC{

public class Vector3Parameter : ShaderParameter
{
	private Vector3 _value;

	public Vector3 value
	{
		get
		{
			return _value;
		}
		set
		{
			if (_value != value)
			{
				_value = value;
				Shader.SetGlobalVector(base.name, _value);
			}
		}
	}

	public Vector3Parameter(string name)
	{
		base.name = name;
	}
}
}