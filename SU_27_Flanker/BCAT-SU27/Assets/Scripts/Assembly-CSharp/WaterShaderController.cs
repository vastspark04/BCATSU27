using UnityEngine;

[ExecuteInEditMode]
public class WaterShaderController : MonoBehaviour
{
	public float _WaveY;

	public float _WaveW;

	public float _Wave2Y;

	public float _Wave2W;

	public float _Wave3Y;

	public float _Wave3W;

	private int sins0ID;

	private int sins1ID;

	private void Awake()
	{
		sins0ID = Shader.PropertyToID("_WaterSins0");
		sins1ID = Shader.PropertyToID("_WaterSins1");
	}

	private void Update()
	{
		float time = Time.time;
		float x = Mathf.Sin(time * _WaveY);
		float y = Mathf.Sin(time * _WaveW);
		float z = Mathf.Sin(time * _Wave2Y);
		float w = Mathf.Sin(time * _Wave2W);
		float x2 = Mathf.Sin(time * _Wave3Y);
		float y2 = Mathf.Sin(time * _Wave3W);
		float z2 = Mathf.Sin(time / 2f);
		Vector4 value = new Vector4(x, y, z, w);
		Vector4 value2 = new Vector4(x2, y2, z2, 1f);
		Shader.SetGlobalVector(sins0ID, value);
		Shader.SetGlobalVector(sins1ID, value2);
	}
}
