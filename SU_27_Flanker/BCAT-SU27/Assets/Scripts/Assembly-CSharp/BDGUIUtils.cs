using UnityEngine;

public static class BDGUIUtils
{
	public static Texture2D pixel;

	private static GUIStyle fontStyle = new GUIStyle();

	public static void DrawTextureOnWorldPos(Camera cam, Vector3 worldPos, Texture texture, Vector2 size, float wobble)
	{
		Vector3 vector = cam.WorldToViewportPoint(worldPos);
		if (!(vector.z < 0f) && vector.x == Mathf.Clamp01(vector.x) && vector.y == Mathf.Clamp01(vector.y))
		{
			float num = vector.x * (float)Screen.width - 0.5f * size.x;
			float num2 = (1f - vector.y) * (float)Screen.height - 0.5f * size.y;
			if (wobble > 0f)
			{
				num += Random.Range((0f - wobble) / 2f, wobble / 2f);
				num2 += Random.Range((0f - wobble) / 2f, wobble / 2f);
			}
			GUI.DrawTexture(new Rect(num, num2, size.x, size.y), texture);
		}
	}

	public static bool WorldToGUIPos(Camera cam, Vector3 worldPos, out Vector2 guiPos)
	{
		Vector3 vector = cam.WorldToViewportPoint(worldPos);
		bool flag = false;
		if (vector.z < 0f)
		{
			flag = true;
		}
		if (vector.x != Mathf.Clamp01(vector.x))
		{
			flag = true;
		}
		if (vector.y != Mathf.Clamp01(vector.y))
		{
			flag = true;
		}
		if (!flag)
		{
			float x = vector.x * (float)Screen.width * cam.rect.width;
			float y = (1f - vector.y * cam.rect.height) * (float)Screen.height;
			guiPos = new Vector2(x, y);
			return true;
		}
		guiPos = Vector2.zero;
		return false;
	}

	public static void DrawWorldspaceWireCube(Camera cam, Vector3 origin, float size, Color color, float thickness)
	{
		float num = size / 2f;
		Vector3 worldPosA = origin + new Vector3(num, num, num);
		Vector3 vector = origin + new Vector3(0f - num, num, num);
		Vector3 vector2 = origin + new Vector3(num, num, 0f - num);
		Vector3 vector3 = origin + new Vector3(0f - num, num, 0f - num);
		DrawLineBetweenWorldPositions(cam, worldPosA, vector, thickness, color);
		DrawLineBetweenWorldPositions(cam, worldPosA, vector2, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector2, vector3, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector, vector3, thickness, color);
		Vector3 vector4 = origin + new Vector3(num, 0f - num, num);
		Vector3 vector5 = origin + new Vector3(0f - num, 0f - num, num);
		Vector3 vector6 = origin + new Vector3(num, 0f - num, 0f - num);
		Vector3 worldPosB = origin + new Vector3(0f - num, 0f - num, 0f - num);
		DrawLineBetweenWorldPositions(cam, worldPosA, vector4, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector, vector5, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector2, vector6, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector3, worldPosB, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector4, vector5, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector4, vector6, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector6, worldPosB, thickness, color);
		DrawLineBetweenWorldPositions(cam, vector5, worldPosB, thickness, color);
	}

	public static void DrawWorldSpaceCircle(Camera cam, Vector3 origin, float radius, Color color, float thickness)
	{
		Vector3 vector = new Vector3(0f, 0f, radius);
		int num = 32;
		float angle = 360f / (float)num;
		for (int i = 0; i < num; i++)
		{
			Vector3 worldPosA = origin + vector;
			vector = Quaternion.AngleAxis(angle, Vector3.up) * vector;
			Vector3 worldPosB = origin + vector;
			DrawLineBetweenWorldPositions(cam, worldPosA, worldPosB, thickness, color);
		}
	}

	public static void DrawLineBetweenWorldPositions(Camera cam, Vector3 worldPosA, Vector3 worldPosB, float width, Color color)
	{
		GUI.matrix = Matrix4x4.identity;
		bool flag = false;
		Plane plane = new Plane(cam.transform.forward, cam.transform.position + cam.transform.forward * 0.05f);
		if (Vector3.Dot(cam.transform.forward, worldPosA - cam.transform.position) < 0f)
		{
			Ray ray = new Ray(worldPosB, worldPosA - worldPosB);
			if (plane.Raycast(ray, out var enter))
			{
				worldPosA = ray.GetPoint(enter);
			}
			flag = true;
		}
		if (Vector3.Dot(cam.transform.forward, worldPosB - cam.transform.position) < 0f)
		{
			if (flag)
			{
				return;
			}
			Ray ray2 = new Ray(worldPosA, worldPosB - worldPosA);
			if (plane.Raycast(ray2, out var enter2))
			{
				worldPosB = ray2.GetPoint(enter2);
			}
		}
		Vector3 vector = cam.WorldToViewportPoint(worldPosA);
		vector.x *= (float)Screen.width * cam.rect.width;
		vector.y = (1f - vector.y * cam.rect.height) * (float)Screen.height;
		Vector3 vector2 = cam.WorldToViewportPoint(worldPosB);
		vector2.x *= (float)Screen.width * cam.rect.width;
		vector2.y = (1f - vector2.y * cam.rect.height) * (float)Screen.height;
		vector.z = (vector2.z = 0f);
		float num = Vector2.Angle(Vector3.up, vector2 - vector);
		if (vector2.x < vector.x)
		{
			num = 0f - num;
		}
		float magnitude = ((Vector2)(vector2 - vector)).magnitude;
		Rect rect = new Rect(vector.x - width / 2f, vector.y - magnitude, width, magnitude);
		GUIUtility.RotateAroundPivot(0f - num + 180f, vector);
		DrawRectangle(rect, color);
		GUI.matrix = Matrix4x4.identity;
	}

	public static void DrawRectangleAroundWorldPoint(Camera cam, Vector3 worldPos, float size, Color color)
	{
		if (!(Vector3.Dot(cam.transform.forward, worldPos - cam.transform.position) < 0f))
		{
			Vector3 vector = cam.WorldToViewportPoint(worldPos);
			vector.x *= (float)Screen.width * cam.rect.width;
			vector.y = (1f - vector.y * cam.rect.height) * (float)Screen.height;
			DrawRectangle(new Rect(vector.x - size / 2f, vector.y - size / 2f, size, size), color);
		}
	}

	public static void DrawTextAtWorldPoint(string text, Camera cam, Vector3 worldPos, int fontSize, Color color)
	{
		if (!(Vector3.Dot(cam.transform.forward, worldPos - cam.transform.position) < 0f))
		{
			Vector3 vector = cam.WorldToViewportPoint(worldPos);
			vector.x *= (float)Screen.width * cam.rect.width;
			vector.y = (1f - vector.y * cam.rect.height) * (float)Screen.height;
			Rect position = new Rect(vector.x, vector.y, 1024f, 1024f);
			fontStyle.fontSize = fontSize;
			fontStyle.alignment = TextAnchor.UpperLeft;
			fontStyle.normal.textColor = color;
			GUI.Label(position, text, fontStyle);
		}
	}

	public static void DrawRectangle(Rect rect, Color color)
	{
		if (pixel == null)
		{
			pixel = new Texture2D(1, 1);
		}
		Color color2 = GUI.color;
		GUI.color = color;
		GUI.DrawTexture(rect, pixel);
		GUI.color = color2;
	}

	public static void DrawDebugSphere(Vector3 pos, float radius, Color color)
	{
		DrawDebugSphere(pos, radius, color, Vector3.up);
	}

	public static void DrawDebugSphere(Vector3 pos, float radius, Color color, Vector3 axis)
	{
		Vector3 vector = Vector3.up;
		axis.Normalize();
		if (vector == axis.normalized)
		{
			vector = Vector3.right;
		}
		Vector3 normalized = Vector3.Cross(axis, vector).normalized;
		vector = Vector3.Cross(axis, normalized).normalized;
		int num = 24;
		float num2 = 360f / (float)num;
		for (int i = 0; i < num; i++)
		{
			Vector3 vector2 = vector * radius;
			Vector3 vector3 = Quaternion.AngleAxis(num2 * (float)i, axis) * vector2;
			Vector3 vector4 = Quaternion.AngleAxis(num2 * (float)(i + 1), axis) * vector2;
			Debug.DrawLine(pos + vector3, pos + vector4, color);
		}
		for (int j = 0; j < num; j++)
		{
			Vector3 vector5 = axis * radius;
			Vector3 vector6 = Quaternion.AngleAxis(num2 * (float)j, vector) * vector5;
			Vector3 vector7 = Quaternion.AngleAxis(num2 * (float)(j + 1), vector) * vector5;
			Debug.DrawLine(pos + vector6, pos + vector7, color);
		}
		for (int k = 0; k < num; k++)
		{
			Vector3 vector8 = vector * radius;
			Vector3 vector9 = Quaternion.AngleAxis(num2 * (float)k, normalized) * vector8;
			Vector3 vector10 = Quaternion.AngleAxis(num2 * (float)(k + 1), normalized) * vector8;
			Debug.DrawLine(pos + vector9, pos + vector10, color);
		}
	}

	public static void DrawDebugCapsule(Vector3 pt1, Vector3 pt2, float radius, Color color)
	{
		Vector3 vector = pt2 - pt1;
		DrawDebugSphere(pt1, radius, color, vector);
		DrawDebugSphere(pt2, radius, color, vector);
		Vector3 vector2 = Vector3.up;
		if (vector2 == vector.normalized)
		{
			vector2 = Vector3.right;
		}
		Vector3 vector3 = Vector3.Cross(vector, vector2).normalized * radius;
		vector2 = Vector3.Cross(vector, vector3).normalized * radius;
		Debug.DrawLine(pt1 + vector3, pt2 + vector3, color);
		Debug.DrawLine(pt1 - vector3, pt2 - vector3, color);
		Debug.DrawLine(pt1 + vector2, pt2 + vector2, color);
		Debug.DrawLine(pt1 - vector2, pt2 - vector2, color);
	}
}
