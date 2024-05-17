using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace BrunetonsImprovedAtmosphere{

public static class CBWrite
{
	private static string[,] writeNames2D = new string[4, 3]
	{
		{ "write2DC1", "_Des2DC1", "_Buffer2DC1" },
		{ "write2DC2", "_Des2DC2", "_Buffer2DC2" },
		{ "write2DC3", "_Des2DC3", "_Buffer2DC3" },
		{ "write2DC4", "_Des2DC4", "_Buffer2DC4" }
	};

	private static string[,] writeNames3D = new string[4, 3]
	{
		{ "write3DC1", "_Des3DC1", "_Buffer3DC1" },
		{ "write3DC2", "_Des3DC2", "_Buffer3DC2" },
		{ "write3DC3", "_Des3DC3", "_Buffer3DC3" },
		{ "write3DC4", "_Des3DC4", "_Buffer3DC4" }
	};

	public static void IntoRenderTexture(RenderTexture tex, int channels, ComputeBuffer buffer, ComputeShader write)
	{
		Check(tex, channels, buffer, write);
		int num = -1;
		int num2 = 1;
		if (tex.dimension == TextureDimension.Tex3D)
		{
			num2 = tex.volumeDepth;
			num = write.FindKernel(writeNames3D[channels - 1, 0]);
			write.SetTexture(num, writeNames3D[channels - 1, 1], tex);
			write.SetBuffer(num, writeNames3D[channels - 1, 2], buffer);
		}
		else
		{
			num = write.FindKernel(writeNames2D[channels - 1, 0]);
			write.SetTexture(num, writeNames2D[channels - 1, 1], tex);
			write.SetBuffer(num, writeNames2D[channels - 1, 2], buffer);
		}
		if (num == -1)
		{
			throw new ArgumentException("Could not find kernel " + writeNames2D[channels - 1, 0]);
		}
		int width = tex.width;
		int height = tex.height;
		write.SetInt("_Width", width);
		write.SetInt("_Height", height);
		write.SetInt("_Depth", num2);
		int num3 = ((width % 8 != 0) ? 1 : 0);
		int num4 = ((height % 8 != 0) ? 1 : 0);
		int num5 = ((num2 % 8 != 0) ? 1 : 0);
		write.Dispatch(num, Mathf.Max(1, width / 8 + num3), Mathf.Max(1, height / 8 + num4), Mathf.Max(1, num2 / 8 + num5));
	}

	public static void IntoRenderTexture(RenderTexture tex, int channels, string Path, ComputeBuffer buffer, ComputeShader write)
	{
		Check(tex, channels, buffer, write);
		int num = -1;
		int num2 = 1;
		if (tex.dimension == TextureDimension.Tex3D)
		{
			num2 = tex.volumeDepth;
			num = write.FindKernel(writeNames3D[channels - 1, 0]);
			write.SetTexture(num, writeNames3D[channels - 1, 1], tex);
			write.SetBuffer(num, writeNames3D[channels - 1, 2], buffer);
		}
		else
		{
			num = write.FindKernel(writeNames2D[channels - 1, 0]);
			write.SetTexture(num, writeNames2D[channels - 1, 1], tex);
			write.SetBuffer(num, writeNames2D[channels - 1, 2], buffer);
		}
		if (num == -1)
		{
			throw new ArgumentException("Could not find kernel " + writeNames2D[channels - 1, 0]);
		}
		int width = tex.width;
		int height = tex.height;
		int num3 = width * height * num2 * channels;
		float[] array = new float[num3];
		LoadRawFile(Path, array, num3);
		buffer.SetData(array);
		write.SetInt("_Width", width);
		write.SetInt("_Height", height);
		write.SetInt("_Depth", num2);
		int num4 = ((width % 8 != 0) ? 1 : 0);
		int num5 = ((height % 8 != 0) ? 1 : 0);
		int num6 = ((num2 % 8 != 0) ? 1 : 0);
		write.Dispatch(num, Mathf.Max(1, width / 8 + num4), Mathf.Max(1, height / 8 + num5), Mathf.Max(1, num2 / 8 + num6));
	}

	private static void LoadRawFile(string Path, float[] map, int size)
	{
		FileInfo fileInfo = new FileInfo(Path);
		if (fileInfo == null)
		{
			throw new ArgumentException("Raw file not found (" + Path + ")");
		}
		FileStream fileStream = fileInfo.OpenRead();
		byte[] array = new byte[fileInfo.Length];
		fileStream.Read(array, 0, (int)fileInfo.Length);
		fileStream.Close();
		if (size > fileInfo.Length / 4)
		{
			throw new ArgumentException("Raw file is not the required size (" + Path + ")");
		}
		int num = 0;
		int num2 = 0;
		while (num < size)
		{
			map[num] = BitConverter.ToSingle(array, num2);
			num++;
			num2 += 4;
		}
	}

	private static void Check(RenderTexture tex, int channels, ComputeBuffer buffer, ComputeShader writeData)
	{
		if (tex == null)
		{
			throw new ArgumentException("RenderTexture is null");
		}
		if (buffer == null)
		{
			throw new ArgumentException("Buffer is null");
		}
		if (writeData == null)
		{
			throw new ArgumentException("Computer shader is null");
		}
		if (channels < 1 || channels > 4)
		{
			throw new ArgumentException("Channels must be 1, 2, 3, or 4");
		}
		if (!tex.enableRandomWrite)
		{
			throw new ArgumentException("You must enable random write on render texture");
		}
		if (!tex.IsCreated())
		{
			throw new ArgumentException("Tex has not been created (Call Create() on tex)");
		}
	}
}}
