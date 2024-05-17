using UnityEngine;

public class CloudGenerator : MonoBehaviour
{
	public int cloudCount;

	public float extent;

	public float heightRange;

	public float height;

	public Cloud cloud;

	private void Start()
	{
	}

	private void OnEnable()
	{
		SpawnClouds(base.transform.position);
	}

	private void SpawnClouds(Vector3 center)
	{
		for (int i = 0; i < cloudCount; i++)
		{
			Vector3 position = new Vector3(Random.Range(0f - extent, extent), height + Random.Range(0f - heightRange, heightRange), Random.Range(0f - extent, extent));
			position += center;
			cloud.SpawnCloud2(position);
		}
	}
}
