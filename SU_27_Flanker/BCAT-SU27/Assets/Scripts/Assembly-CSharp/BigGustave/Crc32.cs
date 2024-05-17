using System.Collections.Generic;

namespace BigGustave {

public static class Crc32
{
	private const uint Polynomial = 3988292384u;

	private static readonly uint[] Lookup;

	static Crc32()
	{
		Lookup = new uint[256];
		for (uint num = 0u; num < 256; num++)
		{
			uint num2 = num;
			for (int i = 0; i < 8; i++)
			{
				num2 = (((num2 & 1) == 0) ? (num2 >> 1) : ((num2 >> 1) ^ 0xEDB88320u));
			}
			Lookup[num] = num2;
		}
	}

	public static uint Calculate(byte[] data)
	{
		uint num = uint.MaxValue;
		for (int i = 0; i < data.Length; i++)
		{
			uint num2 = (num ^ data[i]) & 0xFFu;
			num = (num >> 8) ^ Lookup[num2];
		}
		return num ^ 0xFFFFFFFFu;
	}

	public static uint Calculate(List<byte> data)
	{
		uint num = uint.MaxValue;
		for (int i = 0; i < data.Count; i++)
		{
			uint num2 = (num ^ data[i]) & 0xFFu;
			num = (num >> 8) ^ Lookup[num2];
		}
		return num ^ 0xFFFFFFFFu;
	}

	public static uint Calculate(byte[] data, byte[] data2)
	{
		uint num = uint.MaxValue;
		for (int i = 0; i < data.Length; i++)
		{
			uint num2 = (num ^ data[i]) & 0xFFu;
			num = (num >> 8) ^ Lookup[num2];
		}
		for (int j = 0; j < data2.Length; j++)
		{
			uint num3 = (num ^ data2[j]) & 0xFFu;
			num = (num >> 8) ^ Lookup[num3];
		}
		return num ^ 0xFFFFFFFFu;
	}
}}
