using System;
using UnityEngine;

public static class CloudNoiseGen
{
	[Serializable]
	public enum Mode
	{
		LoadAvailableElseGenerate,
		LoadAvailableElseAbort,
		ForceGenerate
	}

	[Serializable]
	public enum NoiseMode
	{
		Mix,
		PerlinOnly,
		WorleyOnly
	}

	[Serializable]
	public struct NoiseSettings
	{
		[Range(1f, 8f)]
		public int octaves;

		[Range(1f, 16f)]
		public int periods;

		[Range(0f, 2f)]
		public float brightness;

		[Range(0f, 8f)]
		public float contrast;

		public Vector4 GetParams()
		{
			return new Vector4(octaves, periods, brightness, contrast);
		}
	}

	public static NoiseSettings perlin;

	public static NoiseSettings worley;

	public static float previewSliceZ;

	private static Material _generatorMat;

	private static Material generatorMat
	{
		get
		{
			if (!_generatorMat)
			{
				_generatorMat = new Material(Shader.Find("Hidden/CloudNoiseGen"));
			}
			return _generatorMat;
		}
		set
		{
			_generatorMat = value;
		}
	}

	private static void UpdateGenerator()
	{
		generatorMat.SetVector("_PerlinParams", perlin.GetParams());
		generatorMat.SetVector("_WorleyParams", worley.GetParams());
	}

	public static bool LoadNoise(ref Texture3D noiseTexture, string folderName, int resolution)
	{
		if (1 == 0)
		{
			return false;
		}
		UnityEngine.Object[] array = Resources.LoadAll("CloudNoiseGen/" + folderName + "/", typeof(Texture2D));
		if (resolution != array.Length)
		{
			return false;
		}
		UnityEngine.Object[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Texture2D texture2D = (Texture2D)array2[i];
			if (resolution != texture2D.width || resolution != texture2D.height)
			{
				return false;
			}
		}
		Color[] array3 = new Color[resolution * resolution * resolution];
		noiseTexture = new Texture3D(resolution, resolution, resolution, TextureFormat.ARGB32, mipChain: true);
		for (int j = 0; j < array3.Length; j++)
		{
			array3[j] = new Color(1f, 1f, 1f, 1f);
		}
		RenderTexture renderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		renderTexture.filterMode = FilterMode.Point;
		renderTexture.Create();
		int num = 0;
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			Color[] pixels = ((Texture2D)array2[i]).GetPixels();
			foreach (Color color in pixels)
			{
				array3[num++] = color;
			}
		}
		renderTexture.DiscardContents();
		renderTexture = null;
		noiseTexture.SetPixels(array3);
		noiseTexture.Apply();
		return true;
	}

	public static void GetSlice(ref RenderTexture rt, float z, NoiseMode noiseMode = NoiseMode.Mix)
	{
		RenderTexture active = RenderTexture.active;
		UpdateGenerator();
		generatorMat.SetFloat("_Slice", z);
		generatorMat.SetInt("_Mode", (int)noiseMode);
		Graphics.Blit(null, rt, generatorMat);
		RenderTexture.active = active;
	}

	public static bool InitializeNoise(ref Texture3D noiseTexture, string folderName, int resolution, Mode mode = Mode.LoadAvailableElseGenerate)
	{
		noiseTexture = null;
		mode = Mode.LoadAvailableElseAbort;
		bool result = false;
		if (mode != Mode.ForceGenerate)
		{
			result = LoadNoise(ref noiseTexture, folderName, resolution);
		}
		return result;
	}
}
