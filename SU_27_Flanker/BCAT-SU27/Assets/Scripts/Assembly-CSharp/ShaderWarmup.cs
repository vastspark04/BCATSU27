using UnityEngine;

public class ShaderWarmup : MonoBehaviour
{
	private void Awake()
	{
		Shader.WarmupAllShaders();
	}
}
