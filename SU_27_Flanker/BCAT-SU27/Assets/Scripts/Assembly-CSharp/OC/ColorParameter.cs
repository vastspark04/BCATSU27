using UnityEngine;

namespace OC{

public class ColorParameter : ShaderParameter
{
	private Color _value;

	public Color value
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
				Shader.SetGlobalColor(base.name, _value);
			}
		}
	}

	public ColorParameter(string name)
	{
		base.name = name;
	}
}
}