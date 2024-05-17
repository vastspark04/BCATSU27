using System;
using UnityEngine;

[Serializable]
public class PerlinNoise
{
	private const int SIZE = 256;

	private int[] m_perm = new int[512];

	public Texture2D m_permTable1D;

	public Texture2D m_permTable2D;

	public Texture2D m_gradient2D;

	public Texture2D m_gradient3D;

	public Texture2D m_gradient4D;

	private static float[] GRADIENT2 = new float[16]
	{
		0f, 1f, 1f, 1f, 1f, 0f, 1f, -1f, 0f, -1f,
		-1f, -1f, -1f, 0f, -1f, 1f
	};

	private static float[] GRADIENT3 = new float[48]
	{
		1f, 1f, 0f, -1f, 1f, 0f, 1f, -1f, 0f, -1f,
		-1f, 0f, 1f, 0f, 1f, -1f, 0f, 1f, 1f, 0f,
		-1f, -1f, 0f, -1f, 0f, 1f, 1f, 0f, -1f, 1f,
		0f, 1f, -1f, 0f, -1f, -1f, 1f, 1f, 0f, 0f,
		-1f, 1f, -1f, 1f, 0f, 0f, -1f, -1f
	};

	private static float[] GRADIENT4 = new float[128]
	{
		0f, -1f, -1f, -1f, 0f, -1f, -1f, 1f, 0f, -1f,
		1f, -1f, 0f, -1f, 1f, 1f, 0f, 1f, -1f, -1f,
		0f, 1f, -1f, 1f, 0f, 1f, 1f, -1f, 0f, 1f,
		1f, 1f, -1f, -1f, 0f, -1f, -1f, 1f, 0f, -1f,
		1f, -1f, 0f, -1f, 1f, 1f, 0f, -1f, -1f, -1f,
		0f, 1f, -1f, 1f, 0f, 1f, 1f, -1f, 0f, 1f,
		1f, 1f, 0f, 1f, -1f, 0f, -1f, -1f, 1f, 0f,
		-1f, -1f, -1f, 0f, -1f, 1f, 1f, 0f, -1f, 1f,
		-1f, 0f, 1f, -1f, 1f, 0f, 1f, -1f, -1f, 0f,
		1f, 1f, 1f, 0f, 1f, 1f, 0f, -1f, -1f, 0f,
		0f, -1f, -1f, 0f, 0f, -1f, 1f, 0f, 0f, -1f,
		1f, 0f, 0f, 1f, -1f, 0f, 0f, 1f, -1f, 0f,
		0f, 1f, 1f, 0f, 0f, 1f, 1f, 0f
	};

	public Texture2D GetPermutationTable1D()
	{
		return m_permTable1D;
	}

	public Texture2D GetPermutationTable2D()
	{
		return m_permTable2D;
	}

	public Texture2D GetGradient2D()
	{
		return m_gradient2D;
	}

	public Texture2D GetGradient3D()
	{
		return m_gradient3D;
	}

	public Texture2D GetGradient4D()
	{
		return m_gradient4D;
	}

	public PerlinNoise(int seed)
	{
		UnityEngine.Random.InitState(seed);
		int i;
		for (i = 0; i < 256; i++)
		{
			m_perm[i] = i;
		}
		while (--i != 0)
		{
			int num = m_perm[i];
			int num2 = UnityEngine.Random.Range(0, 256);
			m_perm[i] = m_perm[num2];
			m_perm[num2] = num;
		}
		for (i = 0; i < 256; i++)
		{
			m_perm[256 + i] = m_perm[i];
		}
	}

	public void LoadResourcesFor2DNoise()
	{
		LoadPermTable1D();
		LoadGradient2D();
	}

	public void LoadResourcesFor3DNoise()
	{
		LoadPermTable2D();
		LoadGradient3D();
	}

	public void LoadResourcesFor4DNoise()
	{
		LoadPermTable1D();
		LoadPermTable2D();
		LoadGradient4D();
	}

	private void LoadPermTable1D()
	{
		if (!m_permTable1D)
		{
			m_permTable1D = new Texture2D(256, 1, TextureFormat.Alpha8, mipChain: false, linear: true);
			m_permTable1D.hideFlags = HideFlags.DontSave;
			m_permTable1D.filterMode = FilterMode.Point;
			m_permTable1D.wrapMode = TextureWrapMode.Repeat;
			for (int i = 0; i < 256; i++)
			{
				m_permTable1D.SetPixel(i, 1, new Color(0f, 0f, 0f, (float)m_perm[i] / 255f));
			}
			m_permTable1D.Apply();
		}
	}

	private void LoadPermTable2D()
	{
		if ((bool)m_permTable2D)
		{
			return;
		}
		m_permTable2D = new Texture2D(256, 256, TextureFormat.ARGB32, mipChain: false, linear: true);
		m_permTable2D.hideFlags = HideFlags.DontSave;
		m_permTable2D.filterMode = FilterMode.Point;
		m_permTable2D.wrapMode = TextureWrapMode.Repeat;
		for (int i = 0; i < 256; i++)
		{
			for (int j = 0; j < 256; j++)
			{
				int num = m_perm[i] + j;
				int num2 = m_perm[num];
				int num3 = m_perm[num + 1];
				int num4 = m_perm[i + 1] + j;
				int num5 = m_perm[num4];
				int num6 = m_perm[num4 + 1];
				m_permTable2D.SetPixel(i, j, new Color((float)num2 / 255f, (float)num3 / 255f, (float)num5 / 255f, (float)num6 / 255f));
			}
		}
		m_permTable2D.Apply();
	}

	private void LoadGradient2D()
	{
		if (!m_gradient2D)
		{
			m_gradient2D = new Texture2D(8, 1, TextureFormat.RGB24, mipChain: false, linear: true);
			m_gradient2D.hideFlags = HideFlags.DontSave;
			m_gradient2D.filterMode = FilterMode.Point;
			m_gradient2D.wrapMode = TextureWrapMode.Repeat;
			for (int i = 0; i < 8; i++)
			{
				float r = (GRADIENT2[i * 2] + 1f) * 0.5f;
				float g = (GRADIENT2[i * 2 + 1] + 1f) * 0.5f;
				m_gradient2D.SetPixel(i, 0, new Color(r, g, 0f, 1f));
			}
			m_gradient2D.Apply();
		}
	}

	private void LoadGradient3D()
	{
		if (!m_gradient3D)
		{
			m_gradient3D = new Texture2D(256, 1, TextureFormat.RGB24, mipChain: false, linear: true);
			m_gradient3D.hideFlags = HideFlags.DontSave;
			m_gradient3D.filterMode = FilterMode.Point;
			m_gradient3D.wrapMode = TextureWrapMode.Repeat;
			for (int i = 0; i < 256; i++)
			{
				int num = m_perm[i] % 16;
				float r = (GRADIENT3[num * 3] + 1f) * 0.5f;
				float g = (GRADIENT3[num * 3 + 1] + 1f) * 0.5f;
				float b = (GRADIENT3[num * 3 + 2] + 1f) * 0.5f;
				m_gradient3D.SetPixel(i, 0, new Color(r, g, b, 1f));
			}
			m_gradient3D.Apply();
		}
	}

	private void LoadGradient4D()
	{
		if (!m_gradient4D)
		{
			m_gradient4D = new Texture2D(256, 1, TextureFormat.ARGB32, mipChain: false, linear: true);
			m_gradient4D.hideFlags = HideFlags.DontSave;
			m_gradient4D.filterMode = FilterMode.Point;
			m_gradient4D.wrapMode = TextureWrapMode.Repeat;
			for (int i = 0; i < 256; i++)
			{
				int num = m_perm[i] % 32;
				float r = (GRADIENT4[num * 4] + 1f) * 0.5f;
				float g = (GRADIENT4[num * 4 + 1] + 1f) * 0.5f;
				float b = (GRADIENT4[num * 4 + 2] + 1f) * 0.5f;
				float a = (GRADIENT4[num * 4 + 3] + 1f) * 0.5f;
				m_gradient4D.SetPixel(i, 0, new Color(r, g, b, a));
			}
			m_gradient4D.Apply();
		}
	}
}
