using UnityEngine;

public class SimpleOrbitCamera : MonoBehaviour
{
	public float speed;

	public Transform target;

	private void Update()
	{
		Vector3 vector = target.position - base.transform.position;
		vector = Quaternion.AngleAxis(speed * Time.deltaTime, Vector3.up) * vector;
		base.transform.position = target.position - vector;
		base.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
	}
}
