using System;

namespace BigGustave{

internal static class Decoder
{
	public static (byte bytesPerPixel, byte samplesPerPixel) GetBytesAndSamplesPerPixel(ImageHeader header)
	{
		int num = (header.BitDepth + 7) / 8;
		byte b = SamplesPerPixel(header);
		return ((byte)(b * num), b);
	}

	public static byte[] Decode(byte[] decompressedData, ImageHeader header, byte bytesPerPixel, byte samplesPerPixel)
	{
		switch (header.InterlaceMethod)
		{
		case InterlaceMethod.None:
		{
			int num4 = BytesPerScanline(header, samplesPerPixel);
			int num5 = 1;
			for (int m = 0; m < header.Height; m++)
			{
				FilterType type2 = (FilterType)decompressedData[num5 - 1];
				int previousRowStartByteAbsolute2 = m + num4 * (m - 1);
				int num6 = num5 + num4;
				for (int n = num5; n < num6; n++)
				{
					ReverseFilter(decompressedData, type2, previousRowStartByteAbsolute2, num5, n, n - num5, bytesPerPixel);
				}
				num5 += num4 + 1;
			}
			return decompressedData;
		}
		case InterlaceMethod.Adam7:
		{
			int num = header.Width * bytesPerPixel;
			byte[] array = new byte[header.Height * num];
			int num2 = 0;
			int previousRowStartByteAbsolute = -1;
			for (int i = 0; i < 7; i++)
			{
				int numberOfScanlinesInPass = Adam7.GetNumberOfScanlinesInPass(header, i);
				int pixelsPerScanlineInPass = Adam7.GetPixelsPerScanlineInPass(header, i);
				if (numberOfScanlinesInPass <= 0 || pixelsPerScanlineInPass <= 0)
				{
					continue;
				}
				for (int j = 0; j < numberOfScanlinesInPass; j++)
				{
					FilterType type = (FilterType)decompressedData[num2++];
					int num3 = num2;
					for (int k = 0; k < pixelsPerScanlineInPass; k++)
					{
						(int, int) pixelIndexForScanlineInPass = Adam7.GetPixelIndexForScanlineInPass(header, i, j, k);
						for (int l = 0; l < bytesPerPixel; l++)
						{
							int rowByteIndex = k * bytesPerPixel + l;
							ReverseFilter(decompressedData, type, previousRowStartByteAbsolute, num3, num2, rowByteIndex, bytesPerPixel);
							num2++;
						}
						int destinationIndex = num * pixelIndexForScanlineInPass.Item2 + pixelIndexForScanlineInPass.Item1 * bytesPerPixel;
						Array.ConstrainedCopy(decompressedData, num3 + k * bytesPerPixel, array, destinationIndex, bytesPerPixel);
					}
					previousRowStartByteAbsolute = num3;
				}
			}
			return array;
		}
		default:
			throw new ArgumentOutOfRangeException($"Invalid interlace method: {header.InterlaceMethod}.");
		}
	}

	private static byte SamplesPerPixel(ImageHeader header)
	{
		return header.ColorType switch
		{
			ColorType.None => 1, 
			ColorType.PaletteUsed => 1, 
			ColorType.ColorUsed => 3, 
			ColorType.AlphaChannelUsed => 2, 
			ColorType.ColorUsed | ColorType.AlphaChannelUsed => 4, 
			_ => 0, 
		};
	}

	private static int BytesPerScanline(ImageHeader header, byte samplesPerPixel)
	{
		int width = header.Width;
		switch (header.BitDepth)
		{
		case 1:
			return (width + 7) / 8;
		case 2:
			return (width + 3) / 4;
		case 4:
			return (width + 1) / 2;
		case 8:
		case 16:
			return width * samplesPerPixel * ((int)header.BitDepth / 8);
		default:
			return 0;
		}
	}

	private static void ReverseFilter(byte[] data, FilterType type, int previousRowStartByteAbsolute, int rowStartByteAbsolute, int byteAbsolute, int rowByteIndex, int bytesPerPixel)
	{
		switch (type)
		{
		case FilterType.Up:
		{
			int num2 = previousRowStartByteAbsolute + rowByteIndex;
			if (num2 >= 0)
			{
				data[byteAbsolute] += data[num2];
			}
			break;
		}
		case FilterType.Sub:
		{
			int num = rowByteIndex - bytesPerPixel;
			if (num >= 0)
			{
				data[byteAbsolute] += data[rowStartByteAbsolute + num];
			}
			break;
		}
		case FilterType.None:
			break;
		case FilterType.Average:
			data[byteAbsolute] += (byte)((GetLeftByteValue() + GetAboveByteValue()) / 2);
			break;
		case FilterType.Paeth:
		{
			byte a = GetLeftByteValue();
			byte b = GetAboveByteValue();
			byte c = GetAboveLeftByteValue();
			data[byteAbsolute] += GetPaethValue(a, b, c);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("type", type, null);
		}
		byte GetAboveByteValue()
		{
			int num4 = previousRowStartByteAbsolute + rowByteIndex;
			if (num4 < 0)
			{
				return 0;
			}
			return data[num4];
		}
		byte GetAboveLeftByteValue()
		{
			int num3 = previousRowStartByteAbsolute + rowByteIndex - bytesPerPixel;
			if (num3 >= previousRowStartByteAbsolute && previousRowStartByteAbsolute >= 0)
			{
				return data[num3];
			}
			return 0;
		}
		byte GetLeftByteValue()
		{
			int num5 = rowByteIndex - bytesPerPixel;
			if (num5 < 0)
			{
				return 0;
			}
			return data[rowStartByteAbsolute + num5];
		}
	}

	private static byte GetPaethValue(byte a, byte b, byte c)
	{
		int num = a + b - c;
		int num2 = Math.Abs(num - a);
		int num3 = Math.Abs(num - b);
		int num4 = Math.Abs(num - c);
		if (num2 <= num3 && num2 <= num4)
		{
			return a;
		}
		if (num3 > num4)
		{
			return c;
		}
		return b;
	}
}
}
