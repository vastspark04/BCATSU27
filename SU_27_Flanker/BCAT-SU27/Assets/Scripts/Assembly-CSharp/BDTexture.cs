using System;
using System.IO;
using BigGustave;
using UnityEngine;

public class BDTexture
{
	public enum FilterModes
	{
		Bilinear,
		Point
	}

	public enum WrapModes
	{
		Clamp,
		Repeat
	}

	public WrapModes wrapMode;

	public FilterModes filterMode;

	private BDColor[,] pixels;

	private int _width;

	private int _height;

	public bool readOnly;

	public int width => _width;

	public int height => _height;

	public BDTexture(Texture2D uTexture)
	{
		pixels = new BDColor[uTexture.width, uTexture.height];
		_width = pixels.GetLength(0);
		_height = pixels.GetLength(1);
		for (int i = 0; i < _width; i++)
		{
			for (int j = 0; j < _height; j++)
			{
				Color pixel = uTexture.GetPixel(i, j);
				pixels[i, j] = new BDColor(pixel.r, pixel.g, pixel.b, pixel.a);
			}
		}
	}

	public BDTexture(params Texture2D[] textures)
	{
		pixels = new BDColor[textures[0].width, textures[0].height];
		_width = pixels.GetLength(0);
		_height = pixels.GetLength(1);
		int num = textures.Length;
		for (int i = 0; i < _width; i++)
		{
			for (int j = 0; j < _height; j++)
			{
				double num2 = 0.0;
				double num3 = 0.0;
				double num4 = 0.0;
				double num5 = 0.0;
				for (int k = 0; k < num; k++)
				{
					Color pixel = textures[k].GetPixel(i, j);
					num2 += (double)pixel.r;
					num3 += (double)pixel.g;
					num4 += (double)pixel.b;
					num5 += (double)pixel.a;
				}
				num2 /= (double)num;
				num3 /= (double)num;
				num4 /= (double)num;
				num5 /= (double)num;
				pixels[i, j] = new BDColor((float)num2, (float)num3, (float)num4, (float)num5);
			}
		}
	}

	public BDTexture(int width, int height)
	{
		pixels = new BDColor[width, height];
		_width = width;
		_height = height;
	}

	public BDTexture(BDColor[,] pixels)
	{
		this.pixels = pixels;
		_width = pixels.GetLength(0);
		_height = pixels.GetLength(1);
	}

	public BDColor GetColor(float x, float y)
	{
		if (filterMode == FilterModes.Point)
		{
			return GetColorPoint(x, y);
		}
		return GetColorBilinear(x, y);
	}

	public BDColor GetColorUV(float x, float y)
	{
		float x2 = x * (float)(_width - 1);
		float y2 = y * (float)(_height - 1);
		return GetColor(x2, y2);
	}

	public BDColor GetColorPoint(float x, float y)
	{
		return GetPixel(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
	}

	public BDColor GetColorBilinear(float x, float y)
	{
		int num = FloorToInt(x);
		int x2 = num + 1;
		float t = x - (float)num;
		int num2 = FloorToInt(y);
		int y2 = num2 + 1;
		float t2 = y - (float)num2;
		BDColor pixel = GetPixel(num, num2);
		BDColor pixel2 = GetPixel(x2, num2);
		BDColor a = BDColor.Lerp(pixel, pixel2, t);
		BDColor pixel3 = GetPixel(num, y2);
		BDColor pixel4 = GetPixel(x2, y2);
		BDColor b = BDColor.Lerp(pixel3, pixel4, t);
		return BDColor.Lerp(a, b, t2);
	}

	public BDColor GetPixel(IntVector2 coord)
	{
		return GetPixel(coord.x, coord.y);
	}

	public BDColor GetPixel(int x, int y)
	{
		switch (wrapMode)
		{
		case WrapModes.Clamp:
			x = Mathf.Clamp(x, 0, _width - 1);
			y = Mathf.Clamp(y, 0, _height - 1);
			break;
		case WrapModes.Repeat:
			x = (x % _width + _width) % _width;
			y = (y % _height + _height) % _height;
			break;
		}
		BDColor result = default(BDColor);
		try
		{
			result = pixels[x, y];
			return result;
		}
		catch (IndexOutOfRangeException ex)
		{
			Debug.LogError(ex?.ToString() + "\npixels size: " + pixels.GetLength(0) + ", " + pixels.GetLength(1) + "\nattempted get: " + x + ", " + y);
			return result;
		}
	}

	public void SetPixel(int x, int y, BDColor c)
	{
		if (readOnly)
		{
			Debug.LogError("Tried to set pixel of a read-only BDTexture");
		}
		else
		{
			pixels[x, y] = c;
		}
	}

	public void SetPixels(BDColor[,] pixels)
	{
		int length = pixels.GetLength(0);
		int length2 = pixels.GetLength(1);
		if (this.pixels.GetLength(0) != length || this.pixels.GetLength(1) != length2)
		{
			this.pixels = new BDColor[length, length2];
		}
		for (int i = 0; i < length; i++)
		{
			for (int j = 0; j < length2; j++)
			{
				this.pixels[i, j] = pixels[i, j];
			}
		}
		_width = length;
		_height = length2;
	}

	private int FloorToInt(float f)
	{
		return (int)f;
	}

	private float MoveTowards(float a, float b, float maxDist)
	{
		if (maxDist > b - a)
		{
			return b;
		}
		return a + maxDist;
	}

	private float Clamp01(float f)
	{
		if (f < 0f)
		{
			return 0f;
		}
		if (f > 1f)
		{
			return 1f;
		}
		return f;
	}

	public Texture2D ToTexture2D(TextureFormat format, bool mipmaps, bool linear)
	{
		Texture2D texture2D = new Texture2D(width, height, format, mipmaps, linear);
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				texture2D.SetPixel(i, j, GetPixel(i, j).ToColor());
			}
		}
		texture2D.Apply();
		return texture2D;
	}

	public void ApplyToTexture(Texture2D tex)
	{
		if (tex.width != width || tex.height != height)
		{
			Debug.LogError("Can not apply BDTexture to Texture2D. Non-matching dimensions. " + tex.name);
			return;
		}
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				tex.SetPixel(i, j, GetPixel(i, j).ToColor());
			}
		}
		tex.Apply();
	}

	public void SaveToPNGThread(string path)
	{
		PngBuilder pngBuilder = PngBuilder.Create(width, height, hasAlphaChannel: true);
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				BDColor pixel = GetPixel(i, j);
				Pixel pixel2 = new Pixel(GetByte(pixel.r), GetByte(pixel.g), GetByte(pixel.b), GetByte(pixel.a), isGrayscale: false);
				pngBuilder.SetPixel(pixel2, i, j);
			}
		}
		byte[] bytes = pngBuilder.Save();
		File.WriteAllBytes(path, bytes);
	}

	private byte GetByte(float c)
	{
		return (byte)Mathf.Clamp(Mathf.RoundToInt(c * 255f), 0, 255);
	}

	public void SaveToPNG(string path, bool linear)
	{
		try
		{
			byte[] bytes = ToTexture2D(TextureFormat.RGBA32, mipmaps: false, linear).EncodeToPNG();
			File.WriteAllBytes(path, bytes);
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	public void SaveToMultiPNG(string dir, string filePrefix, bool linear, int count)
	{
		Texture2D[] array = new Texture2D[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear);
		}
		for (int j = 0; j < width; j++)
		{
			for (int k = 0; k < height; k++)
			{
				BDColor pixel = GetPixel(j, k);
				double dC = (double)pixel.r * (double)count;
				double dC2 = (double)pixel.g * (double)count;
				double dC3 = (double)pixel.b * (double)count;
				double dC4 = (double)pixel.a * (double)count;
				for (int l = 0; l < count; l++)
				{
					Color color = default(Color);
					color.r = ApplySplitColor(ref dC);
					color.g = ApplySplitColor(ref dC2);
					color.b = ApplySplitColor(ref dC3);
					color.a = ApplySplitColor(ref dC4);
					array[l].SetPixel(j, k, color);
				}
			}
		}
		for (int m = 0; m < count; m++)
		{
			array[m].Apply();
			byte[] bytes = array[m].EncodeToPNG();
			File.WriteAllBytes(Path.Combine(dir, filePrefix + m + ".png"), bytes);
		}
	}

	private float ApplySplitColor(ref double dC)
	{
		if (dC >= 1.0)
		{
			dC -= 1.0;
			return 1f;
		}
		float result = (float)dC;
		dC = 0.0;
		return result;
	}

	public void SaveToBDM(string path)
	{
	}
}
