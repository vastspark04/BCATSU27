using System;

namespace BigGustave
{
[Flags]
public enum ColorType : byte
{
	None = 0,
	PaletteUsed = 1,
	ColorUsed = 2,
	AlphaChannelUsed = 4
}
}