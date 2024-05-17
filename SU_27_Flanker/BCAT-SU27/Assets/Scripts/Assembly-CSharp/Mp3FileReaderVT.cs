using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

public class Mp3FileReaderVT : WaveStream
{
	public delegate IMp3FrameDecompressor FrameDecompressorBuilder(WaveFormat mp3Format);

	public class AudioFileReader : WaveStream, ISampleProvider
	{
		private WaveStream readerStream;

		private readonly SampleChannel sampleChannel;

		private readonly int destBytesPerSample;

		private readonly int sourceBytesPerSample;

		private readonly long length;

		private readonly object lockObject;

		private readonly int lengthSamples;

		public string FileName { get; private set; }

		public override WaveFormat WaveFormat => sampleChannel.WaveFormat;

		public override long Length => length;

		public int LengthSamples => lengthSamples;

		public override long Position
		{
			get
			{
				return SourceToDest(readerStream.Position);
			}
			set
			{
				lock (lockObject)
				{
					readerStream.Position = DestToSource(value);
				}
			}
		}

		public float Volume
		{
			get
			{
				return sampleChannel.Volume;
			}
			set
			{
				sampleChannel.Volume = value;
			}
		}

		public AudioFileReader(string fileName)
		{
			lockObject = new object();
			FileName = fileName;
			CreateReaderStream(fileName);
			sourceBytesPerSample = readerStream.WaveFormat.BitsPerSample / 8 * readerStream.WaveFormat.Channels;
			sampleChannel = new SampleChannel(readerStream, forceStereo: false);
			destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
			length = SourceToDest(readerStream.Length);
			lengthSamples = (int)(readerStream.Length / sourceBytesPerSample);
		}

		private void CreateReaderStream(string fileName)
		{
			if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
			{
				readerStream = new WaveFileReader(fileName);
				if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
				{
					readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
					readerStream = new BlockAlignReductionStream(readerStream);
				}
			}
			else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
			{
				readerStream = new Mp3FileReaderVT(fileName);
			}
			else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
			{
				readerStream = new AiffFileReader(fileName);
			}
			else
			{
				readerStream = new MediaFoundationReader(fileName);
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			WaveBuffer waveBuffer = new WaveBuffer(buffer);
			int count2 = count / 4;
			return Read(waveBuffer.FloatBuffer, offset / 4, count2) * 4;
		}

		public int Read(float[] buffer, int offset, int count)
		{
			lock (lockObject)
			{
				return sampleChannel.Read(buffer, offset, count);
			}
		}

		private long SourceToDest(long sourceBytes)
		{
			return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
		}

		private long DestToSource(long destBytes)
		{
			return sourceBytesPerSample * (destBytes / destBytesPerSample);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && readerStream != null)
			{
				readerStream.Dispose();
				readerStream = null;
			}
			base.Dispose(disposing);
		}
	}

	private const bool validateSampleChange = false;

	private readonly WaveFormat waveFormat;

	private Stream mp3Stream;

	private readonly long mp3DataLength;

	private readonly long dataStartPosition;

	private readonly XingHeader xingHeader;

	private readonly bool ownInputStream;

	private List<Mp3Index> tableOfContents;

	private int tocIndex;

	private long totalSamples;

	private readonly int bytesPerSample;

	private readonly int bytesPerDecodedFrame;

	private IMp3FrameDecompressor decompressor;

	private readonly byte[] decompressBuffer;

	private int decompressBufferOffset;

	private int decompressLeftovers;

	private bool repositionedFlag;

	private long position;

	private readonly object repositionLock = new object();

	public Mp3WaveFormat Mp3WaveFormat { get; private set; }

	public Id3v2Tag Id3v2Tag { get; private set; }

	public byte[] Id3v1Tag { get; private set; }

	public override long Length => totalSamples * bytesPerSample;

	public override WaveFormat WaveFormat => waveFormat;

	public override long Position
	{
		get
		{
			return position;
		}
		set
		{
			lock (repositionLock)
			{
				value = Math.Max(Math.Min(value, Length), 0L);
				long num = value / bytesPerSample;
				Mp3Index mp3Index = null;
				for (int i = 0; i < tableOfContents.Count; i++)
				{
					if (tableOfContents[i].SamplePosition + tableOfContents[i].SampleCount > num)
					{
						mp3Index = tableOfContents[i];
						tocIndex = i;
						break;
					}
				}
				decompressBufferOffset = 0;
				decompressLeftovers = 0;
				repositionedFlag = true;
				if (mp3Index != null)
				{
					mp3Stream.Position = mp3Index.FilePosition;
					long num2 = num - mp3Index.SamplePosition;
					if (num2 > 0)
					{
						decompressBufferOffset = (int)num2 * bytesPerSample;
					}
				}
				else
				{
					mp3Stream.Position = mp3DataLength + dataStartPosition;
				}
				position = value;
			}
		}
	}

	public XingHeader XingHeader => xingHeader;

	public Mp3FileReaderVT(string mp3FileName)
		: this(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, ownInputStream: true)
	{
	}

	public Mp3FileReaderVT(string mp3FileName, FrameDecompressorBuilder frameDecompressorBuilder)
		: this(File.OpenRead(mp3FileName), frameDecompressorBuilder, ownInputStream: true)
	{
	}

	public Mp3FileReaderVT(Stream inputStream)
		: this(inputStream, CreateAcmFrameDecompressor, ownInputStream: false)
	{
	}

	public Mp3FileReaderVT(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder)
		: this(inputStream, frameDecompressorBuilder, ownInputStream: false)
	{
	}

	private Mp3FileReaderVT(Stream inputStream, FrameDecompressorBuilder frameDecompressorBuilder, bool ownInputStream)
	{
		if (inputStream == null)
		{
			throw new ArgumentNullException("inputStream");
		}
		if (frameDecompressorBuilder == null)
		{
			throw new ArgumentNullException("frameDecompressorBuilder");
		}
		this.ownInputStream = ownInputStream;
		try
		{
			mp3Stream = inputStream;
			Id3v2Tag = Id3v2Tag.ReadTag(mp3Stream);
			dataStartPosition = mp3Stream.Position;
			Mp3Frame mp3Frame = Mp3Frame.LoadFromStream(mp3Stream);
			if (mp3Frame == null)
			{
				throw new InvalidDataException("Invalid MP3 file - no MP3 Frames Detected");
			}
			double num = mp3Frame.BitRate;
			xingHeader = XingHeader.LoadXingHeader(mp3Frame);
			if (xingHeader != null)
			{
				dataStartPosition = mp3Stream.Position;
			}
			Mp3Frame mp3Frame2 = Mp3Frame.LoadFromStream(mp3Stream);
			if (mp3Frame2 != null && (mp3Frame2.SampleRate != mp3Frame.SampleRate || mp3Frame2.ChannelMode != mp3Frame.ChannelMode))
			{
				dataStartPosition = mp3Frame2.FileOffset;
				mp3Frame = mp3Frame2;
			}
			mp3DataLength = mp3Stream.Length - dataStartPosition;
			mp3Stream.Position = mp3Stream.Length - 128;
			byte[] array = new byte[128];
			mp3Stream.Read(array, 0, 128);
			if (array[0] == 84 && array[1] == 65 && array[2] == 71)
			{
				Id3v1Tag = array;
				mp3DataLength -= 128L;
			}
			mp3Stream.Position = dataStartPosition;
			Mp3WaveFormat = new Mp3WaveFormat(mp3Frame.SampleRate, (mp3Frame.ChannelMode == ChannelMode.Mono) ? 1 : 2, mp3Frame.FrameLength, (int)num);
			CreateTableOfContents();
			tocIndex = 0;
			num = (double)mp3DataLength * 8.0 / TotalSeconds();
			mp3Stream.Position = dataStartPosition;
			Mp3WaveFormat = new Mp3WaveFormat(mp3Frame.SampleRate, (mp3Frame.ChannelMode == ChannelMode.Mono) ? 1 : 2, mp3Frame.FrameLength, (int)num);
			decompressor = frameDecompressorBuilder(Mp3WaveFormat);
			waveFormat = decompressor.OutputFormat;
			bytesPerSample = decompressor.OutputFormat.BitsPerSample / 8 * decompressor.OutputFormat.Channels;
			bytesPerDecodedFrame = 1152 * bytesPerSample;
			decompressBuffer = new byte[bytesPerDecodedFrame * 2];
		}
		catch (Exception)
		{
			if (ownInputStream)
			{
				inputStream.Dispose();
			}
			throw;
		}
	}

	public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format)
	{
		return new AcmMp3FrameDecompressor(mp3Format);
	}

	private void CreateTableOfContents()
	{
		try
		{
			tableOfContents = new List<Mp3Index>((int)(mp3DataLength / 400));
			Mp3Frame mp3Frame;
			do
			{
				Mp3Index mp3Index = new Mp3Index();
				mp3Index.FilePosition = mp3Stream.Position;
				mp3Index.SamplePosition = totalSamples;
				mp3Frame = ReadNextFrame(readData: false);
				if (mp3Frame != null)
				{
					ValidateFrameFormat(mp3Frame);
					totalSamples += mp3Frame.SampleCount;
					mp3Index.SampleCount = mp3Frame.SampleCount;
					mp3Index.ByteCount = (int)(mp3Stream.Position - mp3Index.FilePosition);
					tableOfContents.Add(mp3Index);
				}
			}
			while (mp3Frame != null);
		}
		catch (EndOfStreamException)
		{
		}
	}

	private void ValidateFrameFormat(Mp3Frame frame)
	{
		if (((frame.ChannelMode == ChannelMode.Mono) ? 1 : 2) != Mp3WaveFormat.Channels)
		{
			throw new InvalidOperationException($"Got a frame with channel mode {frame.ChannelMode}, in an MP3 with {Mp3WaveFormat.Channels} channels. Mp3FileReader does not support changes to channel count.");
		}
	}

	private double TotalSeconds()
	{
		return (double)totalSamples / (double)Mp3WaveFormat.SampleRate;
	}

	public Mp3Frame ReadNextFrame()
	{
		Mp3Frame mp3Frame = ReadNextFrame(readData: true);
		if (mp3Frame != null)
		{
			position += mp3Frame.SampleCount * bytesPerSample;
		}
		return mp3Frame;
	}

	private Mp3Frame ReadNextFrame(bool readData)
	{
		Mp3Frame mp3Frame = null;
		try
		{
			mp3Frame = Mp3Frame.LoadFromStream(mp3Stream, readData);
			if (mp3Frame != null)
			{
				tocIndex++;
				return mp3Frame;
			}
			return mp3Frame;
		}
		catch (EndOfStreamException)
		{
			return mp3Frame;
		}
	}

	public override int Read(byte[] sampleBuffer, int offset, int numBytes)
	{
		int num = 0;
		lock (repositionLock)
		{
			if (decompressLeftovers != 0)
			{
				int num2 = Math.Min(decompressLeftovers, numBytes);
				Array.Copy(decompressBuffer, decompressBufferOffset, sampleBuffer, offset, num2);
				decompressLeftovers -= num2;
				if (decompressLeftovers == 0)
				{
					decompressBufferOffset = 0;
				}
				else
				{
					decompressBufferOffset += num2;
				}
				num += num2;
				offset += num2;
			}
			int num3 = tocIndex;
			if (repositionedFlag)
			{
				decompressor.Reset();
				tocIndex = Math.Max(0, tocIndex - 3);
				mp3Stream.Position = tableOfContents[tocIndex].FilePosition;
				repositionedFlag = false;
			}
			while (num < numBytes)
			{
				Mp3Frame mp3Frame = ReadNextFrame(readData: true);
				if (mp3Frame == null)
				{
					break;
				}
				int num4 = decompressor.DecompressFrame(mp3Frame, decompressBuffer, 0);
				if (tocIndex > num3 && num4 != 0)
				{
					if (tocIndex == num3 + 1 && num4 == bytesPerDecodedFrame * 2)
					{
						Array.Copy(decompressBuffer, bytesPerDecodedFrame, decompressBuffer, 0, bytesPerDecodedFrame);
						num4 = bytesPerDecodedFrame;
					}
					int num5 = Math.Min(num4 - decompressBufferOffset, numBytes - num);
					Array.Copy(decompressBuffer, decompressBufferOffset, sampleBuffer, offset, num5);
					if (num5 + decompressBufferOffset < num4)
					{
						decompressBufferOffset = num5 + decompressBufferOffset;
						decompressLeftovers = num4 - decompressBufferOffset;
					}
					else
					{
						decompressBufferOffset = 0;
						decompressLeftovers = 0;
					}
					offset += num5;
					num += num5;
				}
			}
		}
		position += num;
		return num;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (mp3Stream != null)
			{
				if (ownInputStream)
				{
					mp3Stream.Dispose();
				}
				mp3Stream = null;
			}
			if (decompressor != null)
			{
				decompressor.Dispose();
				decompressor = null;
			}
		}
		base.Dispose(disposing);
	}
}
