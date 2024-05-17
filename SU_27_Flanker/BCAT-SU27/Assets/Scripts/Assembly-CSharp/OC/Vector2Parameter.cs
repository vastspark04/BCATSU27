using UnityEngine;

namespace OC{

public class Vector2Parameter : ShaderParameter
{
	private Vector2 _value;

	public Vector2 value
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

	public Vector2Parameter(string name)
	{
		base.name = name;
	}
}}
