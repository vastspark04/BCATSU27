using UnityEngine;

public class RadarSweeper : MonoBehaviour
{
	public float range;

	public float speed;

	private Vector3 origEuler;

	private float time;

	private void Start()
	{
		origEuler = base.transform.localEulerAngles;
	}

	private void Update()
	{
		time += Mathf.Min(Time.deltaTime, 0.011f);
		float num = Triangle(time * speed) * range;
		Vector3 localEulerAngles = origEuler;
		localEulerAngles.y += num;
		base.transform.localEulerAngles = localEulerAngles;
	}

	private float Triangle(float t)
	{
		float num = Mathf.Repeat(t / 2f, 2f);
		if (num > 1f)
		{
			num = 1f - (num - 1f);
		}
		return (num - 0.5f) * 2f;
	}
}
