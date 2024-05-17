using UnityEngine;

namespace OC{

public class Vector4Parameter : ShaderParameter
{
	private Vector4 _value;

	public Vector4 value
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

	public Vector4Parameter(string name)
	{
		base.name = name;
	}
}}
