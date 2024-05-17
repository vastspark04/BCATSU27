using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class VTLineDrawer : MonoBehaviour
{
	public struct QueuedLine
	{
		public Vector3 a;

		public Vector3 b;

		public Color color;

		public QueuedLine(Vector3 a, Vector3 b, Color color)
		{
			this.a = a;
			this.b = b;
			this.color = color;
		}
	}

	private static Material glLineMat;

	public Queue<QueuedLine> lineQueue = new Queue<QueuedLine>();

	public void DrawLine(Vector3 a, Vector3 b, Color color)
	{
		lineQueue.Enqueue(new QueuedLine(a, b, color));
	}

	public void DrawCircle(Vector3 center, float radius, Color color)
	{
		int num = 24;
		float angle = 360 / num;
		Vector3 vector = radius * Vector3.forward;
		for (int i = 0; i < num; i++)
		{
			Vector3 a = center + vector;
			vector = Quaternion.AngleAxis(angle, Vector3.up) * vector;
			Vector3 b = center + vector;
			DrawLine(a, b, color);
		}
	}

	public void DrawCircle(Vector3 center, float radius, Color color, Vector3 axis)
	{
		int num = 24;
		float angle = 360 / num;
		Vector3 vector = Vector3.right;
		if (vector == axis)
		{
			vector = Vector3.forward;
		}
		Vector3 vector2 = radius * Vector3.Cross(axis, vector).normalized;
		for (int i = 0; i < num; i++)
		{
			Vector3 a = center + vector2;
			vector2 = Quaternion.AngleAxis(angle, axis) * vector2;
			Vector3 b = center + vector2;
			DrawLine(a, b, color);
		}
	}

	public void DrawWireSphere(Vector3 center, float radius, Color color)
	{
		DrawCircle(center, radius, color);
		DrawCircle(center, radius, color, Vector3.forward);
		DrawCircle(center, radius, color, Vector3.right);
	}

	private void Awake()
	{
		if (glLineMat == null)
		{
			glLineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
			glLineMat.hideFlags = HideFlags.HideAndDontSave;
			glLineMat.SetInt("_ZWrite", 0);
		}
	}

	private void OnPostRender()
	{
		GLDrawLines();
	}

	private void GLDrawLines()
	{
		if (lineQueue.Count > 0)
		{
			GL.PushMatrix();
			glLineMat.SetPass(0);
			GL.Begin(1);
			while (lineQueue.Count > 0)
			{
				QueuedLine queuedLine = lineQueue.Dequeue();
				GL.Color(queuedLine.color);
				GL.Vertex(queuedLine.a);
				GL.Vertex(queuedLine.b);
			}
			GL.End();
			GL.PopMatrix();
		}
	}
}
