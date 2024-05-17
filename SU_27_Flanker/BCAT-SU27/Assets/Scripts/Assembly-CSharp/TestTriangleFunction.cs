using UnityEngine;

public class TestTriangleFunction : MonoBehaviour
{
	private void OnDrawGizmos()
	{
		Vector3 vector = base.transform.position;
		Vector3 vector2 = vector;
		for (float num = -3f; num < 3f; num += 0.02f)
		{
			Vector3 vector3 = vector2;
			vector3.x += num;
			vector3.y += Triangle(num);
			Debug.DrawLine(vector, vector3);
			vector = vector3;
		}
	}

	private float Triangle(float t)
	{
		t += 0.5f;
		float num = Mathf.Repeat(t, 1f);
		if (Mathf.FloorToInt(t) % 2 != 0)
		{
			num = 1f - num;
		}
		return (num - 0.5f) * 2f;
	}
}
