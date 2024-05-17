using UnityEngine;

namespace OC{

public class TextureParameter : ShaderParameter
{
	private Texture _value;

	public Texture value
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
				Shader.SetGlobalTexture(base.name, _value);
			}
		}
	}

	public TextureParameter(string name)
	{
		base.name = name;
	}
}
}