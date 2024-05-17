using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrunetonsImprovedAtmosphere{

public static class CBRead
{
	private static string[,] readNames2D = new string[4, 3]
	{
		{ "read2DC1", "_Tex2D", "_Buffer2DC1" },
		{ "read2DC2", "_Tex2D", "_Buffer2DC2" },
		{ "read2DC3", "_Tex2D", "_Buffer2DC3" },
		{ "read2DC4", "_Tex2D", "_Buffer2DC4" }
	};

	private static string[,] readNames3D = new string[4, 3]
	{
		{ "read3DC1", "_Tex3D", "_Buffer3DC1" },
		{ "read3DC2", "_Tex3D", "_Buffer3DC2" },
		{ "read3DC3", "_Tex3D", "_Buffer3DC3" },
		{ "read3DC4", "_Tex3D", "_Buffer3DC4" }
	};

	public static void FromRenderTexture(RenderTexture tex, int channels, ComputeBuffer buffer, ComputeShader read)
	{
		Check(tex, channels, buffer, read);
		int num = -1;
		int num2 = 1;
		if (tex.dimension == TextureDimension.Tex3D)
		{
			num2 = tex.volumeDepth;
			num = read.FindKernel(readNames3D[channels - 1, 0]);
			read.SetTexture(num, readNames3D[channels - 1, 1], tex);
			read.SetBuffer(num, readNames3D[channels - 1, 2], buffer);
		}
		else
		{
			num = read.FindKernel(readNames2D[channels - 1, 0]);
			read.SetTexture(num, readNames2D[channels - 1, 1], tex);
			read.SetBuffer(num, readNames2D[channels - 1, 2], buffer);
		}
		if (num == -1)
		{
			throw new ArgumentException("Could not find kernel " + readNames2D[channels - 1, 0]);
		}
		int width = tex.width;
		int height = tex.height;
		read.SetInt("_Width", width);
		read.SetInt("_Height", height);
		read.SetInt("_Depth", num2);
		int num3 = ((width % 8 != 0) ? 1 : 0);
		int num4 = ((height % 8 != 0) ? 1 : 0);
		int num5 = ((num2 % 8 != 0) ? 1 : 0);
		read.Dispatch(num, Mathf.Max(1, width / 8 + num3), Mathf.Max(1, height / 8 + num4), Mathf.Max(1, num2 / 8 + num5));
	}

	public static void SingleFromRenderTexture(RenderTexture tex, float x, float y, float z, ComputeBuffer buffer, ComputeShader read, bool useBilinear)
	{
		Check(tex, 0, buffer, read);
		int num = -1;
		int num2 = 1;
		if (tex.dimension == TextureDimension.Tex3D)
		{
			num = ((!useBilinear) ? read.FindKernel("readSingle3D") : read.FindKernel("readSingleBilinear3D"));
			num2 = tex.volumeDepth;
			read.SetTexture(num, "_Tex3D", tex);
			read.SetBuffer(num, "_BufferSingle3D", buffer);
		}
		else
		{
			num = ((!useBilinear) ? read.FindKernel("readSingle2D") : read.FindKernel("readSingleBilinear2D"));
			read.SetTexture(num, "_Tex2D", tex);
			read.SetBuffer(num, "_BufferSingle2D", buffer);
		}
		if (num == -1)
		{
			throw new ArgumentException("Could not find kernel readSingle for " + tex.dimension);
		}
		int width = tex.width;
		int height = tex.height;
		read.SetInt("_IdxX", (int)x);
		read.SetInt("_IdxY", (int)y);
		read.SetInt("_IdxZ", (int)z);
		read.SetVector("_UV", new Vector4(x / (float)(width - 1), y / (float)(height - 1), z / (float)(num2 - 1), 0f));
		read.Dispatch(num, 1, 1, 1);
	}

	private static void Check(RenderTexture tex, int channels, ComputeBuffer buffer, ComputeShader read)
	{
		if (tex == null)
		{
			throw new ArgumentException("RenderTexture is null");
		}
		if (buffer == null)
		{
			throw new ArgumentException("Buffer is null");
		}
		if (read == null)
		{
			throw new ArgumentException("Computer shader is null");
		}
		if (channels < 1 || channels > 4)
		{
			throw new ArgumentException("Channels must be 1, 2, 3, or 4");
		}
		if (!tex.IsCreated())
		{
			throw new ArgumentException("Tex has not been created (Call Create() on tex)");
		}
	}
}}
