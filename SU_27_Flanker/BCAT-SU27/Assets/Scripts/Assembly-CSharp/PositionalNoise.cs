using UnityEngine;

public class PositionalNoise : MonoBehaviour
{
	public Vector3 speed;

	public Vector3 magnitude;

	public Vector3 offset;

	public float lerpRate = 15f;

	private Vector3 origPos;

	private Vector3 seed;

	private void Start()
	{
		origPos = base.transform.localPosition;
		seed = Random.insideUnitSphere * 100f;
	}

	private void Update()
	{
		float time = Time.time;
		float x = magnitude.x * VectorUtils.FullRangePerlinNoise(offset.x + time * speed.x, seed.x);
		float y = magnitude.y * VectorUtils.FullRangePerlinNoise(offset.y + time * speed.y, seed.y);
		float z = magnitude.z * VectorUtils.FullRangePerlinNoise(offset.z + time * speed.z, seed.z);
		base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, origPos + new Vector3(x, y, z), lerpRate * Time.deltaTime);
	}
}
