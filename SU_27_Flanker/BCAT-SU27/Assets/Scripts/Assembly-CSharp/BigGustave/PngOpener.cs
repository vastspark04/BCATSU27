using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace BigGustave {

internal static class PngOpener
{
	public static Png Open(Stream stream, IChunkVisitor chunkVisitor = null)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanRead)
		{
			throw new ArgumentException("The provided stream of type " + stream.GetType().FullName + " was not readable.");
		}
		HeaderValidationResult headerValidationResult = HasValidHeader(stream);
		if (!headerValidationResult.IsValid)
		{
			throw new ArgumentException($"The provided stream did not start with the PNG header. Got {headerValidationResult}.");
		}
		byte[] array = new byte[4];
		ImageHeader header = ReadImageHeader(stream, array);
		bool flag = false;
		Palette palette = null;
		using MemoryStream memoryStream2 = new MemoryStream();
		using (MemoryStream memoryStream = new MemoryStream())
		{
			ChunkHeader chunkHeader;
			while (TryReadChunkHeader(stream, out chunkHeader))
			{
				if (flag)
				{
					throw new InvalidOperationException($"Found another chunk {chunkHeader} after already reading the IEND chunk.");
				}
				byte[] array2 = new byte[chunkHeader.Length];
				int num = stream.Read(array2, 0, array2.Length);
				if (num != array2.Length)
				{
					throw new InvalidOperationException($"Did not read {chunkHeader.Length} bytes for the {chunkHeader} header, only found: {num}.");
				}
				if (chunkHeader.IsCritical)
				{
					switch (chunkHeader.Name)
					{
					case "PLTE":
						if (chunkHeader.Length % 3 != 0)
						{
							throw new InvalidOperationException($"Palette data must be multiple of 3, got {chunkHeader.Length}.");
						}
						palette = new Palette(array2);
						break;
					case "IDAT":
						memoryStream.Write(array2, 0, array2.Length);
						break;
					case "IEND":
						flag = true;
						break;
					default:
						throw new NotSupportedException($"Encountered critical header {chunkHeader} which was not recognised.");
					}
				}
				num = stream.Read(array, 0, array.Length);
				if (num != 4)
				{
					throw new InvalidOperationException($"Did not read 4 bytes for the CRC, only found: {num}.");
				}
				int num2 = (int)Crc32.Calculate(Encoding.ASCII.GetBytes(chunkHeader.Name), array2);
				int num3 = (array[0] << 24) + (array[1] << 16) + (array[2] << 8) + array[3];
				if (num2 != num3)
				{
					throw new InvalidOperationException($"CRC calculated {num2} did not match file {num3} for chunk: {chunkHeader.Name}.");
				}
				chunkVisitor?.Visit(stream, header, chunkHeader, array2, array);
			}
			memoryStream.Flush();
			memoryStream.Seek(2L, SeekOrigin.Begin);
			using DeflateStream deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
			deflateStream.CopyTo(memoryStream2);
			deflateStream.Close();
		}
		byte[] decompressedData = memoryStream2.ToArray();
		(byte bytesPerPixel, byte samplesPerPixel) bytesAndSamplesPerPixel = Decoder.GetBytesAndSamplesPerPixel(header);
		byte item = bytesAndSamplesPerPixel.bytesPerPixel;
		byte item2 = bytesAndSamplesPerPixel.samplesPerPixel;
		decompressedData = Decoder.Decode(decompressedData, header, item, item2);
		return new Png(header, new RawPngData(decompressedData, item, header.Width, header.InterlaceMethod, palette, header.ColorType));
	}

	private static HeaderValidationResult HasValidHeader(Stream stream)
	{
		return new HeaderValidationResult(stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte(), stream.ReadByte());
	}

	private static bool TryReadChunkHeader(Stream stream, out ChunkHeader chunkHeader)
	{
		chunkHeader = default(ChunkHeader);
		long position = stream.Position;
		if (!StreamHelper.TryReadHeaderBytes(stream, out var bytes))
		{
			return false;
		}
		int length = StreamHelper.ReadBigEndianInt32(bytes, 0);
		string @string = Encoding.ASCII.GetString(bytes, 4, 4);
		chunkHeader = new ChunkHeader(position, length, @string);
		return true;
	}

	private static ImageHeader ReadImageHeader(Stream stream, byte[] crc)
	{
		if (!TryReadChunkHeader(stream, out var chunkHeader))
		{
			throw new ArgumentException("The provided stream did not contain a single chunk.");
		}
		if (chunkHeader.Name != "IHDR")
		{
			throw new ArgumentException($"The first chunk was not the IHDR chunk: {chunkHeader}.");
		}
		if (chunkHeader.Length != 13)
		{
			throw new ArgumentException($"The first chunk did not have a length of 13 bytes: {chunkHeader}.");
		}
		byte[] array = new byte[13];
		int num = stream.Read(array, 0, array.Length);
		if (num != 13)
		{
			throw new InvalidOperationException($"Did not read 13 bytes for the IHDR, only found: {num}.");
		}
		num = stream.Read(crc, 0, crc.Length);
		if (num != 4)
		{
			throw new InvalidOperationException($"Did not read 4 bytes for the CRC, only found: {num}.");
		}
		int width = StreamHelper.ReadBigEndianInt32(array, 0);
		int height = StreamHelper.ReadBigEndianInt32(array, 4);
		byte bitDepth = array[8];
		byte colorType = array[9];
		byte compressionMethod = array[10];
		byte filterMethod = array[11];
		byte interlaceMethod = array[12];
		return new ImageHeader(width, height, bitDepth, (ColorType)colorType, (CompressionMethod)compressionMethod, (FilterMethod)filterMethod, (InterlaceMethod)interlaceMethod);
	}
}}
