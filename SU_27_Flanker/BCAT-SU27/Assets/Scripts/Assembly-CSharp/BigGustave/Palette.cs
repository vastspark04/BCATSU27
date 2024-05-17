namespace BigGustave {

internal class Palette
{
	public byte[] Data { get; }

	public Palette(byte[] data)
	{
		Data = data;
	}

	public Pixel GetPixel(int index)
	{
		int num = index * 3;
		return new Pixel(Data[num], Data[num + 1], Data[num + 2], byte.MaxValue, isGrayscale: false);
	}
}
}