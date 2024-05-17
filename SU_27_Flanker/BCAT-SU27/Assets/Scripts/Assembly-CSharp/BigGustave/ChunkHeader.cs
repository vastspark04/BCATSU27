using System;

namespace BigGustave
{
    public readonly struct ChunkHeader
    {
        public long Position { get; }

        public int Length { get; }

        public string Name { get; }

        public bool IsCritical => char.IsUpper(Name[0]);

        public bool IsPublic => char.IsUpper(Name[1]);

        public bool IsSafeToCopy => char.IsUpper(Name[3]);

        public ChunkHeader(long position, int length, string name)
        {
            if (length < 0)
            {
                throw new ArgumentException($"Length less than zero ({length}) encountered when reading chunk at position {position}.");
            }
            Position = position;
            Length = length;
            Name = name;
        }

        public override string ToString()
        {
            return $"{Name} at {Position} (length: {Length}).";
        }
    }
}
