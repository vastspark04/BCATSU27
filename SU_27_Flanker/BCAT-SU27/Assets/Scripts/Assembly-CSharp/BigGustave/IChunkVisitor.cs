using System.IO;

namespace BigGustave {

public interface IChunkVisitor
{
	void Visit(Stream stream, ImageHeader header, ChunkHeader chunkHeader, byte[] data, byte[] crc);
}
}