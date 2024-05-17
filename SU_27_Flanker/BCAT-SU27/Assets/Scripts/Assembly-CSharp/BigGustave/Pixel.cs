namespace BigGustave {

public readonly struct Pixel
{
	public byte R { get; }

	public byte G { get; }

	public byte B { get; }

	public byte A { get; }

	public bool IsGrayscale { get; }

	public Pixel(byte r, byte g, byte b, byte a, bool isGrayscale)
	{
		R = r;
		G = g;
		B = b;
		A = a;
		IsGrayscale = isGrayscale;
	}

	public Pixel(byte r, byte g, byte b)
	{
		R = r;
		G = g;
		B = b;
		A = byte.MaxValue;
		IsGrayscale = false;
	}

	public Pixel(byte grayscale)
	{
		R = grayscale;
		G = grayscale;
		B = grayscale;
		A = byte.MaxValue;
		IsGrayscale = true;
	}

	public override bool Equals(object obj)
	{
		if (obj is Pixel pixel)
		{
			if (IsGrayscale == pixel.IsGrayscale && A == pixel.A && R == pixel.R && G == pixel.G)
			{
				return B == pixel.B;
			}
			return false;
		}
		return false;
	}

	public bool Equals(Pixel other)
	{
		if (R == other.R && G == other.G && B == other.B && A == other.A)
		{
			return IsGrayscale == other.IsGrayscale;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((R.GetHashCode() * 397) ^ G.GetHashCode()) * 397) ^ B.GetHashCode()) * 397) ^ A.GetHashCode()) * 397) ^ IsGrayscale.GetHashCode();
	}

	public override string ToString()
	{
		return $"({R}, {G}, {B}, {A})";
	}
}}
