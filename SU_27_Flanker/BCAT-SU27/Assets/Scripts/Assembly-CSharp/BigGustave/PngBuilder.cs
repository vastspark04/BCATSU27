using System.IO;
using System.IO.Compression;
using System.Text;

namespace BigGustave {

public class PngBuilder
{
	private const byte Deflate32KbWindow = 120;

	private const byte ChecksumBits = 1;

	private readonly byte[] rawData;

	private readonly bool hasAlphaChannel;

	private readonly int width;

	private readonly int height;

	private readonly int bytesPerPixel;

	public static PngBuilder Create(int width, int height, bool hasAlphaChannel)
	{
		int num = (hasAlphaChannel ? 4 : 3);
		return new PngBuilder(new byte[height * width * num + height], hasAlphaChannel, width, height, num);
	}

	private PngBuilder(byte[] rawData, bool hasAlphaChannel, int width, int height, int bytesPerPixel)
	{
		this.rawData = rawData;
		this.hasAlphaChannel = hasAlphaChannel;
		this.width = width;
		this.height = height;
		this.bytesPerPixel = bytesPerPixel;
	}

	public PngBuilder SetPixel(byte r, byte g, byte b, int x, int y)
	{
		return SetPixel(new Pixel(r, g, b), x, y);
	}

	public PngBuilder SetPixel(Pixel pixel, int x, int y)
	{
		int num = y * (width * bytesPerPixel + 1) + 1 + x * bytesPerPixel;
		rawData[num++] = pixel.R;
		rawData[num++] = pixel.G;
		rawData[num++] = pixel.B;
		if (hasAlphaChannel)
		{
			rawData[num] = pixel.A;
		}
		return this;
	}

	public byte[] Save()
	{
		using MemoryStream memoryStream = new MemoryStream();
		Save(memoryStream);
		return memoryStream.ToArray();
	}

	public void Save(Stream outputStream)
	{
		outputStream.Write(HeaderValidationResult.ExpectedHeader, 0, HeaderValidationResult.ExpectedHeader.Length);
		PngStreamWriteHelper pngStreamWriteHelper = new PngStreamWriteHelper(outputStream);
		pngStreamWriteHelper.WriteChunkLength(13);
		pngStreamWriteHelper.WriteChunkHeader(ImageHeader.HeaderBytes);
		StreamHelper.WriteBigEndianInt32(pngStreamWriteHelper, width);
		StreamHelper.WriteBigEndianInt32(pngStreamWriteHelper, height);
		pngStreamWriteHelper.WriteByte(8);
		ColorType colorType = ColorType.ColorUsed;
		if (hasAlphaChannel)
		{
			colorType |= ColorType.AlphaChannelUsed;
		}
		pngStreamWriteHelper.WriteByte((byte)colorType);
		pngStreamWriteHelper.WriteByte(0);
		pngStreamWriteHelper.WriteByte(0);
		pngStreamWriteHelper.WriteByte(0);
		pngStreamWriteHelper.WriteCrc();
		byte[] array = Compress(rawData);
		pngStreamWriteHelper.WriteChunkLength(array.Length);
		pngStreamWriteHelper.WriteChunkHeader(Encoding.ASCII.GetBytes("IDAT"));
		pngStreamWriteHelper.Write(array, 0, array.Length);
		pngStreamWriteHelper.WriteCrc();
		pngStreamWriteHelper.WriteChunkLength(0);
		pngStreamWriteHelper.WriteChunkHeader(Encoding.ASCII.GetBytes("IEND"));
		pngStreamWriteHelper.WriteCrc();
	}

	private static byte[] Compress(byte[] data)
	{
		using MemoryStream memoryStream = new MemoryStream();
		using DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true);
		deflateStream.Write(data, 0, data.Length);
		deflateStream.Close();
		memoryStream.Seek(0L, SeekOrigin.Begin);
		byte[] array = new byte[2 + memoryStream.Length + 4];
		array[0] = 120;
		array[1] = 1;
		int num = 0;
		int num2;
		while ((num2 = memoryStream.ReadByte()) != -1)
		{
			array[2 + num] = (byte)num2;
			num++;
		}
		int num3 = Adler32Checksum.Calculate(data);
		long num4 = 2 + memoryStream.Length;
		array[num4++] = (byte)(num3 >> 24);
		array[num4++] = (byte)(num3 >> 16);
		array[num4++] = (byte)(num3 >> 8);
		array[num4] = (byte)num3;
		return array;
	}
}}
