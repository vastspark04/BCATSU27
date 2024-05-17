using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class BDTextureTest : MonoBehaviour
{
	private class AsyncMeshData
	{
		public float height;

		public BDTexture bdTex;

		public List<Vector3> verts;

		public List<Vector2> uvs;

		public bool done;

		public object lockObj = new object();

		public float donePercent;
	}

	public MeshRenderer origQuad;

	public MeshRenderer lerpedQuad;

	public MeshRenderer controlQuad;

	public Texture2D testTexture;

	public Texture2D controlTexture;

	public MeshFilter testMesh;

	public float testHeight = 0.25f;

	public int lerpedSize = 1024;

	private BDTexture bdTex;

	public BDTexture.FilterModes filterMode;

	private float meshProgress;

	private void Start()
	{
		Material material = new Material(Shader.Find("Unlit/Texture"));
		material.SetTexture("_MainTex", controlTexture);
		controlQuad.material = material;
		bdTex = ConvertTexture(testTexture);
		bdTex.filterMode = filterMode;
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.T))
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			Apply();
			stopwatch.Stop();
			UnityEngine.Debug.Log("Time: " + stopwatch.ElapsedMilliseconds + " ms");
		}
		if (Input.GetKeyDown(KeyCode.M))
		{
			StartCoroutine(ApplyToMeshRoutine(bdTex));
		}
	}

	private void Apply()
	{
		int width = testTexture.width;
		Material material = new Material(Shader.Find("Unlit/Texture"));
		Material material2 = new Material(material);
		material.SetTexture("_MainTex", testTexture);
		Texture2D texture2D = new Texture2D(lerpedSize, lerpedSize);
		texture2D.filterMode = FilterMode.Point;
		for (int i = 0; i < lerpedSize; i++)
		{
			for (int j = 0; j < lerpedSize; j++)
			{
				float x = (float)i / (float)lerpedSize * (float)width;
				float y = (float)j / (float)lerpedSize * (float)width;
				texture2D.SetPixel(i, j, ConvertColor(bdTex.GetColor(x, y)));
			}
		}
		texture2D.Apply();
		material2.SetTexture("_MainTex", texture2D);
		origQuad.material = material;
		lerpedQuad.material = material2;
	}

	private BDTexture ConvertTexture(Texture2D texture)
	{
		BDTexture bDTexture = new BDTexture(texture.width, texture.height);
		for (int i = 0; i < texture.width; i++)
		{
			for (int j = 0; j < texture.height; j++)
			{
				bDTexture.SetPixel(i, j, ConvertColor(texture.GetPixel(i, j)));
			}
		}
		return bDTexture;
	}

	private BDColor ConvertColor(Color c)
	{
		return new BDColor(c.r, c.g, c.b, c.a);
	}

	private Color ConvertColor(BDColor c)
	{
		return new Color(c.r, c.g, c.b, c.a);
	}

	private IEnumerator ApplyToMeshRoutine(BDTexture bdTex)
	{
		Stopwatch s = new Stopwatch();
		s.Start();
		Mesh i = testMesh.mesh;
		AsyncMeshData md = new AsyncMeshData
		{
			bdTex = bdTex,
			verts = new List<Vector3>(i.vertexCount),
			uvs = new List<Vector2>(i.vertexCount),
			height = testHeight
		};
		yield return null;
		i.GetVertices(md.verts);
		yield return null;
		i.GetUVs(0, md.uvs);
		s.Reset();
		new Thread(ApplyToMesh).Start(md);
		while (!md.done)
		{
			lock (md.lockObj)
			{
				meshProgress = Mathf.Round(md.donePercent * 100f);
			}
			yield return null;
		}
		meshProgress = 100f;
		s.Reset();
		i.SetVertices(md.verts);
		yield return null;
		i.RecalculateNormals();
		yield return null;
		testMesh.mesh = i;
		s.Stop();
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(20f, 20f, 100f, 100f), "Mesh: " + meshProgress + "%");
	}

	private void ApplyToMesh(object mdObj)
	{
		FastNoise fastNoise = new FastNoise();
		float num = 0.0005f;
		float num2 = 8f;
		int num3 = 4;
		AsyncMeshData asyncMeshData = (AsyncMeshData)mdObj;
		int width = asyncMeshData.bdTex.width;
		int count = asyncMeshData.verts.Count;
		for (int i = 0; i < count; i++)
		{
			float num4 = asyncMeshData.uvs[i].x * (float)width;
			float num5 = asyncMeshData.uvs[i].y * (float)width;
			Vector3 value = asyncMeshData.verts[i];
			value.z = asyncMeshData.bdTex.GetColor(num4, num5).r * asyncMeshData.height;
			for (int j = 1; j <= num3; j++)
			{
				value.z += ((float)fastNoise.GetPerlinFractal(num4 * num2 * (float)j, num5 * num2 * (float)j) - 0.5f) * 2f * num / (float)j;
			}
			asyncMeshData.verts[i] = value;
			lock (asyncMeshData.lockObj)
			{
				asyncMeshData.donePercent = (float)i / (float)(count - 1);
			}
		}
		asyncMeshData.done = true;
	}
}
