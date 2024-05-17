using UnityEngine;

namespace BrunetonsImprovedAtmosphere{

public class RotateLight : MonoBehaviour
{
	public float speed = 5f;

	private Vector3 lastMousePos;

	private void Update()
	{
		Vector3 vector = (lastMousePos - Input.mousePosition) * Time.deltaTime * speed;
		if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))
		{
			base.transform.Rotate(new Vector3(0f - vector.y, 0f - vector.x, 0f));
		}
		lastMousePos = Input.mousePosition;
	}
}}
