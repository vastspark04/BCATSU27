using System;
using UnityEngine;

namespace BrunetonsImprovedAtmosphere{

public class RotateCamera : MonoBehaviour
{
	public float speed = 1f;

	private float viewDistanceMeters = 9f;

	private float viewZenithAngleRadians = 1.47f;

	private float viewAzimuthAngleRadians = -1.47f;

	private Vector3 lastMousePos;

	private void Update()
	{
		HandleMouseWheelEvent();
		HandleMouseDragEvent();
		base.transform.LookAt(Vector3.zero);
	}

	private void HandleMouseWheelEvent()
	{
		float axis = Input.GetAxis("Mouse ScrollWheel");
		if (axis > 0f)
		{
			viewDistanceMeters *= 1.05f;
		}
		else if (axis < 0f)
		{
			viewDistanceMeters /= 1.05f;
		}
	}

	private void HandleMouseDragEvent()
	{
		Vector3 vector = (lastMousePos - Input.mousePosition) * Time.deltaTime * speed;
		if (Input.GetMouseButton(0) && !Input.GetKey(KeyCode.LeftControl))
		{
			viewZenithAngleRadians += 0f - vector.y;
			viewZenithAngleRadians = Mathf.Max(0f, Mathf.Min((float)Math.PI / 2f, viewZenithAngleRadians));
			viewAzimuthAngleRadians += vector.x;
		}
		float y = Mathf.Cos(viewZenithAngleRadians);
		float num = Mathf.Sin(viewZenithAngleRadians);
		float num2 = Mathf.Cos(viewAzimuthAngleRadians);
		float num3 = Mathf.Sin(viewAzimuthAngleRadians);
		Vector3 position = new Vector3(num * num2, y, num * num3) * viewDistanceMeters;
		base.transform.position = position;
		lastMousePos = Input.mousePosition;
	}
}
}