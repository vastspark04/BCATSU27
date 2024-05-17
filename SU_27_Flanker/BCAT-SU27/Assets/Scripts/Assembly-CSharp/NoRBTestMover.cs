using UnityEngine;

public class NoRBTestMover : MonoBehaviour
{
	private Vector3 centerPos;

	private float angle;

	private Vector3 offset = new Vector3(0f, 0f, 30f);

	private void Start()
	{
		centerPos = base.transform.position + 10f * base.transform.forward + 20f * Vector3.up;
	}

	private void Update()
	{
		base.transform.position = centerPos + offset;
		base.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, offset));
		offset = Quaternion.AngleAxis(60f * Time.deltaTime, Vector3.up) * offset;
	}
}
