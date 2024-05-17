using UnityEngine;

namespace OC{

public class FloatParameter : ShaderParameter
{
	private float _value;

	public float value
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
				Shader.SetGlobalFloat(base.name, _value);
			}
		}
	}

	public FloatParameter(string name)
	{
		base.name = name;
	}
}
}