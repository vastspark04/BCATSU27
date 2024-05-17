using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[ExecuteInEditMode]
public class VTText : MonoBehaviour, ISwitchableEmissionText, ISwitchableEmission
{
	public enum AlignmentModes
	{
		Left,
		Center,
		Right
	}

	public enum VerticalAlignmentModes
	{
		Top,
		Middle,
		Bottom
	}

	private class ProtoMesh
	{
		public List<Vector3> vertices = new List<Vector3>();

		public List<Vector3> normals = new List<Vector3>();

		public List<int> triangles = new List<int>();

		public List<Vector2> uvs = new List<Vector2>();

		public Mesh ToMesh()
		{
			Mesh mesh = new Mesh();
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetTriangles(triangles, 0);
			mesh.SetUVs(0, uvs);
			mesh.RecalculateBounds();
			return mesh;
		}

		public void ApplyToMesh(Mesh m)
		{
			m.SetVertices(vertices);
			m.SetNormals(normals);
			m.SetTriangles(triangles, 0);
			m.SetUVs(0, uvs);
			m.RecalculateBounds();
		}

		public void Delete()
		{
			vertices.Clear();
			normals.Clear();
			triangles.Clear();
			uvs.Clear();
			vertices = null;
			normals = null;
			triangles = null;
			uvs = null;
		}
	}

	[TextArea]
	public string text;

	public VTTextFont font;

	public float fontSize = 30f;

	public float lineHeight = 1f;

	public AlignmentModes align;

	public VerticalAlignmentModes vertAlign;

	public Color color = Color.white;

	public Color emission = Color.black;

	public bool useEmission = true;

	[Range(0f, 1f)]
	public float emissionMult = 1f;

	private MeshFilter mf;

	private MeshRenderer mr;

	private Material newTextMaterial;

	private Mesh generatedMesh;

	private string lastFontName = string.Empty;

	private MaterialPropertyBlock props;

	private void Start()
	{
		ApplyText();
	}

	[ContextMenu("Test")]
	public void Test()
	{
		ApplyText();
	}

	private bool EnsureComponents()
	{
		return true;
	}

	public void SetEmission(bool e)
	{
		if (EnsureComponents())
		{
			useEmission = e;
			if (useEmission)
			{
				props.SetColor("_Emission", emission * emissionMult);
			}
			else
			{
				props.SetColor("_Emission", Color.black);
			}
			mr.SetPropertyBlock(props);
		}
	}

	public void SetEmissionMultiplier(float e)
	{
		if (EnsureComponents())
		{
			e = Mathf.Clamp01(e);
			emissionMult = e;
			if (useEmission)
			{
				props.SetColor("_Emission", emission * emissionMult);
			}
			mr.SetPropertyBlock(props);
		}
	}

	public void ApplyText()
	{
		if (!EnsureComponents())
		{
			return;
		}
		string obj = text.ToUpper();
		ProtoMesh protoMesh = new ProtoMesh();
		Vector3 pos = new Vector3(0f, 0f - fontSize, 0f);
		string[] array = obj.Split('\n');
		float num = lineHeight * fontSize * (float)array.Length;
		if (vertAlign == VerticalAlignmentModes.Bottom)
		{
			pos.y += num;
		}
		else if (vertAlign == VerticalAlignmentModes.Middle)
		{
			pos.y += num / 2f;
		}
		for (int i = 0; i < array.Length; i++)
		{
			ApplyLine(protoMesh, array[i], ref pos);
		}
		if (generatedMesh == null || !Application.isPlaying)
		{
			if ((bool)generatedMesh)
			{
				UnityEngine.Object.DestroyImmediate(generatedMesh);
			}
			mf.mesh = (generatedMesh = protoMesh.ToMesh());
		}
		else
		{
			generatedMesh.Clear();
			protoMesh.ApplyToMesh(generatedMesh);
		}
		protoMesh.Delete();
	}

	private void OnDestroy()
	{
		if (generatedMesh != null)
		{
			UnityEngine.Object.DestroyImmediate(generatedMesh);
		}
		if ((bool)newTextMaterial)
		{
			UnityEngine.Object.DestroyImmediate(newTextMaterial);
		}
	}

	private void ApplyLine(ProtoMesh pm, string line, ref Vector3 pos)
	{
		if (align == AlignmentModes.Right || align == AlignmentModes.Center)
		{
			foreach (char c in line)
			{
				if (c == ' ')
				{
					float num = font.spaceWidth * fontSize;
					if (align == AlignmentModes.Center)
					{
						num /= 2f;
					}
					pos.x -= num;
				}
				else if (font.charsDict.ContainsKey(c))
				{
					VTTextFont.VTChar vTChar = font.charsDict[c];
					float num2 = fontSize * vTChar.uvSize.x / vTChar.uvSize.y;
					if (align == AlignmentModes.Center)
					{
						num2 /= 2f;
					}
					pos.x -= num2;
				}
			}
		}
		foreach (char c2 in line)
		{
			if (c2 == ' ')
			{
				pos.x += font.spaceWidth * fontSize;
			}
			else if (font.charsDict.ContainsKey(c2))
			{
				VTTextFont.VTChar vTChar2 = font.charsDict[c2];
				float num3 = fontSize * vTChar2.uvSize.x / vTChar2.uvSize.y;
				AddQuad(pm, pos, fontSize, vTChar2.uvPos, vTChar2.uvSize);
				pos.x += num3;
			}
		}
		pos.x = 0f;
		pos.y -= fontSize * lineHeight;
	}

	private void AddQuad(ProtoMesh mesh, Vector3 position, float fontSize, Vector2 uvPos, Vector2 uvSize)
	{
		int count = mesh.vertices.Count;
		float x = fontSize * uvSize.x / uvSize.y;
		mesh.vertices.Add(position);
		mesh.vertices.Add(position + new Vector3(x, 0f, 0f));
		mesh.vertices.Add(position + new Vector3(x, fontSize, 0f));
		mesh.vertices.Add(position + new Vector3(0f, fontSize, 0f));
		Vector3 vector = position + new Vector3(x, fontSize, 0f) / 2f;
		for (int i = 0; i < 4; i++)
		{
			_ = mesh.vertices[count + i] - vector;
			Vector3 item = -Vector3.forward;
			mesh.normals.Add(item);
		}
		mesh.triangles.Add(3 + count);
		mesh.triangles.Add(2 + count);
		mesh.triangles.Add(count);
		mesh.triangles.Add(2 + count);
		mesh.triangles.Add(1 + count);
		mesh.triangles.Add(count);
		mesh.uvs.Add(uvPos);
		mesh.uvs.Add(uvPos + new Vector2(uvSize.x, 0f));
		mesh.uvs.Add(uvPos + uvSize);
		mesh.uvs.Add(uvPos + new Vector2(0f, uvSize.y));
	}
}
