using UnityEngine;

public class AlternatingLight : MonoBehaviour
{
	public Light sharedLight;

	public Transform[] points;

	private int idx;

	private int count;

	private void Start()
	{
		count = points.Length;
	}

	private void LateUpdate()
	{
		sharedLight.transform.position = points[idx].position;
		idx = (idx + 1) % count;
	}
}
