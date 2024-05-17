using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class AntennaRenderer : MonoBehaviour
{
	[Serializable]
	public class TubeVertex
	{
		public Vector3 point = Vector3.zero;

		public float radius = 1f;

		public Color color = Color.white;

		public TubeVertex(Vector3 pt, float r, Color c)
		{
			point = pt;
			radius = r;
			color = c;
		}
	}

	[HideInInspector]
	public TubeVertex[] vertices;

	[HideInInspector]
	public Material material;

	[HideInInspector]
	public int crossSegments = 3;

	private Vector3[] crossPoints;

	private int lastCrossSegments;

	[HideInInspector]
	public float flatAtDistance = -1f;

	[HideInInspector]
	public float startWidth = 1f;

	[HideInInspector]
	public float endWidth = 1f;

	private Vector3 lastCameraPosition1;

	private Vector3 lastCameraPosition2;

	[HideInInspector]
	public int movePixelsForRebuild = 6;

	[HideInInspector]
	public float maxRebuildTime = 0.1f;

	private float lastRebuildTime;

	private MeshRenderer renderer;

	private MeshFilter meshFilter;

	private Vector2 prevRes;

	private void Awake()
	{
		if (!renderer)
		{
			renderer = GetComponent<MeshRenderer>();
		}
		if (!meshFilter)
		{
			meshFilter = GetComponent<MeshFilter>();
		}
	}

	private void Reset()
	{
		vertices = new TubeVertex[2]
		{
			new TubeVertex(Vector3.zero, 1f, Color.white),
			new TubeVertex(new Vector3(1f, 0f, 0f), 1f, Color.white)
		};
	}

	private void Start()
	{
		if ((bool)renderer)
		{
			if (Application.isPlaying)
			{
				renderer.material = material;
				meshFilter.mesh = null;
			}
			else
			{
				renderer.sharedMaterial = material;
				meshFilter.sharedMesh = null;
			}
		}
	}

	private void LateUpdate()
	{
		if (vertices == null || vertices.Length <= 1)
		{
			renderer.enabled = false;
			return;
		}
		renderer.enabled = true;
		renderer.material = material;
		bool flag = false;
		if (vertices.Length > 1)
		{
			Vector3 vector = Camera.main.WorldToScreenPoint(vertices[0].point);
			_ = lastCameraPosition1;
			lastCameraPosition1.z = 0f;
			Vector3 vector2 = Camera.main.WorldToScreenPoint(vertices[vertices.Length - 1].point);
			lastCameraPosition2.z = 0f;
			if ((lastCameraPosition1 - vector).magnitude + (lastCameraPosition2 - vector2).magnitude > (float)movePixelsForRebuild || Time.time - lastRebuildTime > maxRebuildTime)
			{
				flag = true;
				lastCameraPosition1 = vector;
				lastCameraPosition2 = vector2;
			}
		}
		if (!flag)
		{
			return;
		}
		if (crossSegments != lastCrossSegments)
		{
			crossPoints = new Vector3[crossSegments];
			float num = (float)Math.PI * 2f / (float)crossSegments;
			for (int i = 0; i < crossSegments; i++)
			{
				crossPoints[i] = new Vector3(Mathf.Cos(num * (float)i), Mathf.Sin(num * (float)i), 0f);
			}
			lastCrossSegments = crossSegments;
		}
		Vector3[] array = new Vector3[vertices.Length * crossSegments];
		Vector2[] array2 = new Vector2[vertices.Length * crossSegments];
		Color[] array3 = new Color[vertices.Length * crossSegments];
		int[] array4 = new int[vertices.Length * crossSegments * 6];
		int[] array5 = new int[crossSegments];
		int[] array6 = new int[crossSegments];
		Quaternion quaternion = Quaternion.identity;
		for (int j = 0; j < vertices.Length; j++)
		{
			if (j < vertices.Length - 1)
			{
				quaternion = Quaternion.FromToRotation(Vector3.forward, vertices[j + 1].point - vertices[j].point);
			}
			vertices[j].radius = Mathf.Lerp(startWidth / 2f, endWidth / 2f, Mathf.Clamp(j - 1, 0f, float.MaxValue) / (float)(vertices.Length - 3));
			if (j == vertices.Length - 1 || j == 0)
			{
				vertices[j].radius = 0f;
			}
			for (int k = 0; k < crossSegments; k++)
			{
				int num2 = j * crossSegments + k;
				array[num2] = vertices[j].point + quaternion * crossPoints[k] * vertices[j].radius;
				array2[num2] = new Vector2((0f + (float)k) / (float)crossSegments, (0f + (float)j) / (float)vertices.Length);
				array3[num2] = vertices[j].color;
				array5[k] = array6[k];
				array6[k] = j * crossSegments + k;
			}
			if (j > 0)
			{
				for (int l = 0; l < crossSegments; l++)
				{
					int num3 = (j * crossSegments + l) * 6;
					array4[num3] = array5[l];
					array4[num3 + 1] = array5[(l + 1) % crossSegments];
					array4[num3 + 2] = array6[l];
					array4[num3 + 3] = array4[num3 + 2];
					array4[num3 + 4] = array4[num3 + 1];
					array4[num3 + 5] = array6[(l + 1) % crossSegments];
				}
			}
		}
		Vector2 vector3 = new Vector2(crossSegments, vertices.Length);
		Mesh mesh = (Application.isPlaying ? meshFilter.mesh : meshFilter.sharedMesh);
		if (!mesh || vector3 != prevRes)
		{
			mesh = new Mesh();
			mesh.name = base.gameObject.name;
			if (!Application.isPlaying)
			{
				meshFilter.sharedMesh = mesh;
			}
			prevRes = vector3;
		}
		mesh.vertices = array;
		mesh.triangles = array4;
		mesh.RecalculateNormals();
		mesh.uv = array2;
		mesh.RecalculateBounds();
	}

	public void SetPoints(Vector3[] points, float radius, Color col, bool worldSpace)
	{
		if (worldSpace)
		{
			Vector3[] array = new Vector3[points.Length];
			for (int i = 0; i < points.Length; i++)
			{
				array[i] = base.transform.InverseTransformPoint(points[i]);
			}
			points = array;
		}
		if (points.Length >= 2)
		{
			vertices = new TubeVertex[points.Length + 2];
			Vector3 vector = (points[0] - points[1]) * 0.01f;
			vertices[0] = new TubeVertex(vector + points[0], 0f, col);
			Vector3 vector2 = (points[points.Length - 1] - points[points.Length - 2]) * 0.01f;
			vertices[vertices.Length - 1] = new TubeVertex(vector2 + points[points.Length - 1], 0f, col);
			for (int j = 0; j < points.Length; j++)
			{
				vertices[j + 1] = new TubeVertex(points[j], radius, col);
			}
		}
	}
}
