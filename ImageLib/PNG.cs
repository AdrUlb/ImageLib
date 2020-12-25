using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageLib
{
	public static class PNG
	{
		enum CompressionMethod : byte
		{
			Deflate = 0
		}

		enum FilterMethod : byte
		{
			Prediction = 0
		}

		enum InterlaceMethod : byte
		{
			None = 0,
			Adam7 = 1
		}

		readonly struct Chunk
		{
			public readonly string Type;
			public readonly IReadOnlyCollection<byte> Data;
			public readonly IReadOnlyCollection<byte> CRC;

			public Chunk(string type, ReadOnlySpan<byte> data, Span<byte> crc)
			{
				Type = type;
				Data = data.ToArray();
				CRC = crc.ToArray();
			}
		}

		readonly struct IHDRChunkData
		{
			public readonly uint Width;
			public readonly uint Height;
			public readonly byte BitDepth;
			public readonly byte ColorType;
			public readonly CompressionMethod CompressionMethod;
			public readonly FilterMethod FilterMethod;
			public readonly InterlaceMethod InterlaceMethod;

			public IHDRChunkData(uint width, uint height, byte bitDepth, byte colorType, CompressionMethod compressionMethod, FilterMethod filterMethod, InterlaceMethod interlaceMethod)
			{
				Width = width;
				Height = height;
				BitDepth = bitDepth;
				ColorType = colorType;
				CompressionMethod = compressionMethod;
				FilterMethod = filterMethod;
				InterlaceMethod = interlaceMethod;
			}
		}

		public static readonly IReadOnlyCollection<byte> Header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

		public static bool ValidateHeader(Stream s)
		{
			if (s.Length < 8)
				return false;

			var buf = new byte[Header.Count];
			s.Read(buf, 0, buf.Length);

			return Header.SequenceEqual(buf);
		}

		public static bool ValidateHeader(string path)
		{
			using var fs = File.OpenRead(path);
			return ValidateHeader(fs);
		}

		private static Chunk ReadChunk(Stream s)
		{
			void ReadBytesCheckEOS(Span<byte> span)
			{
				if (s.Read(span) != span.Length)
					throw new EndOfStreamException("Unexpected end of stream");
			}

			Span<byte> sizeBytes = stackalloc byte[4];
			ReadBytesCheckEOS(sizeBytes);
			var size = BinaryPrimitives.ReadInt32BigEndian(sizeBytes);

			Span<byte> typeBytes = stackalloc byte[4];
			ReadBytesCheckEOS(typeBytes);
			var type = Encoding.ASCII.GetString(typeBytes);

			Span<byte> data = stackalloc byte[size];
			ReadBytesCheckEOS(data);

			Span<byte> crc = stackalloc byte[4];
			ReadBytesCheckEOS(crc);

			return new Chunk(type, data, crc);
		}

		private static IHDRChunkData ParseIHDRChunk(Chunk chunk)
		{
			if (chunk.Type != "IHDR" || chunk.Data.Count != 13)
				throw new FormatException("Tried parsing a malformed IHDR chunk");

			var data = new Span<byte>(chunk.Data.ToArray());
			//var data = chunk.Data.ToList();

			var width = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(0, 4));
			var height = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4));
			var bitDepth = data[8];
			var colorType = data[9];
			var compressionMethod = (CompressionMethod)data[10];
			var filterMethod = (FilterMethod)data[11];
			var interlaceMethod = (InterlaceMethod)data[12];

			return new IHDRChunkData(width, height, bitDepth, colorType, compressionMethod, filterMethod, interlaceMethod);
		}

		public static void DecodeImage(Stream s)
		{
			Console.WriteLine($"Decoding PNG");

			var end = false;

			while (!end)
			{
				var chunk = ReadChunk(s);
				Console.WriteLine($"  Chunk: {chunk.Type}");
				
				switch (chunk.Type)
				{
					case "IHDR":
						{
							var data = ParseIHDRChunk(chunk);
							Console.WriteLine($"    Width: {data.Width}");
							Console.WriteLine($"    Height: {data.Height}");
							Console.WriteLine($"    Bit depth: {data.BitDepth}");
							Console.WriteLine($"    Color type: {data.ColorType}");
							Console.WriteLine($"    Compression method: {data.CompressionMethod}");
							Console.WriteLine($"    Filter method: {data.FilterMethod}");
							Console.WriteLine($"    Interlace method: {data.InterlaceMethod}");
						}
						break;
					case "IEND":
						end = true;
						break;
					default:
						Console.WriteLine("    [UNKNOWN CHUNK FORMAT]");
						break;
				}
			}
		}
	}
}
