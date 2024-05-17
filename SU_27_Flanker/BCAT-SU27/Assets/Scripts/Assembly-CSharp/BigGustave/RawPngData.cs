using System;

namespace BigGustave {

internal class RawPngData
{
	private readonly byte[] data;

	private readonly int bytesPerPixel;

	private readonly int width;

	private readonly Palette palette;

	private readonly ColorType colorType;

	private readonly int rowOffset;

	public RawPngData(byte[] data, int bytesPerPixel, int width, InterlaceMethod interlaceMethod, Palette palette, ColorType colorType)
	{
		if (width < 0)
		{
			throw new ArgumentOutOfRangeException($"Width must be greater than or equal to 0, got {width}.");
		}
		this.data = data ?? throw new ArgumentNullException("data");
		this.bytesPerPixel = bytesPerPixel;
		this.width = width;
		this.palette = palette;
		this.colorType = colorType;
		rowOffset = ((interlaceMethod != InterlaceMethod.Adam7) ? 1 : 0);
	}

	public Pixel GetPixel(int x, int y)
	{
		int num = rowOffset + rowOffset * y + bytesPerPixel * width * y + bytesPerPixel * x;
		byte b = data[num];
		if (palette != null)
		{
			return palette.GetPixel(b);
		}
		switch (bytesPerPixel)
		{
		case 1:
			return new Pixel(b, b, b, byte.MaxValue, isGrayscale: true);
		case 2:
			if (colorType == ColorType.None)
			{
				byte second3 = data[num + 1];
				byte num3 = ToSingleByte(b, second3);
				return new Pixel(num3, num3, num3, byte.MaxValue, isGrayscale: true);
			}
			return new Pixel(b, b, b, data[num + 1], isGrayscale: true);
		case 3:
			return new Pixel(b, data[num + 1], data[num + 2], byte.MaxValue, isGrayscale: false);
		case 4:
			if (colorType == ColorType.AlphaChannelUsed)
			{
				byte second = data[num + 1];
				byte first = data[num + 2];
				byte second2 = data[num + 3];
				byte num2 = ToSingleByte(b, second);
				byte a = ToSingleByte(first, second2);
				return new Pixel(num2, num2, num2, a, isGrayscale: true);
			}
			return new Pixel(b, data[num + 1], data[num + 2], data[num + 3], isGrayscale: false);
		case 6:
			return new Pixel(b, data[num + 2], data[num + 4], byte.MaxValue, isGrayscale: false);
		case 8:
			return new Pixel(b, data[num + 2], data[num + 4], data[num + 6], isGrayscale: false);
		default:
			throw new InvalidOperationException($"Unreconized number of bytes per pixel: {bytesPerPixel}.");
		}
	}

	private static byte ToSingleByte(byte first, byte second)
	{
		int num = (first << 8) + second;
		return (byte)Math.Round((double)(255 * num) / 65535.0);
	}
}}
