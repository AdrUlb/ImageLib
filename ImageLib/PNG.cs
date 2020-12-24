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

			public Chunk(string type, byte[] data, byte[] crc)
			{
				Type = type;
				Data = data;
				CRC = crc;
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
			var sizeBytes = new byte[4];
			if (s.Read(sizeBytes, 0, sizeBytes.Length) != sizeBytes.Length)
				throw new EndOfStreamException("Unexpected end of stream");
			var size = BinaryPrimitives.ReadUInt32BigEndian(sizeBytes);

			var typeBytes = new byte[4];
			if (s.Read(typeBytes, 0, typeBytes.Length) != typeBytes.Length)
				throw new EndOfStreamException("Unexpected end of stream");
			var type = Encoding.ASCII.GetString(typeBytes);

			var data = new byte[size];
			if (s.Read(data, 0, data.Length) != data.Length)
				throw new EndOfStreamException("Unexpected end of stream");

			var crc = new byte[4];
			if (s.Read(crc, 0, crc.Length) != crc.Length)
				throw new EndOfStreamException("Unexpected end of stream");

			return new Chunk(type, data, crc);
		}

		private static IHDRChunkData ParseIHDRChunk(Chunk chunk)
		{
			if (chunk.Type != "IHDR" || chunk.Data.Count != 13)
				throw new FormatException("Tried parsing a malformed IHDR chunk");

			var data = chunk.Data.ToList();

			var width = BinaryPrimitives.ReadUInt32BigEndian(data.GetRange(0, 4).ToArray());
			var height = BinaryPrimitives.ReadUInt32BigEndian(data.GetRange(4, 4).ToArray());
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
				Console.WriteLine("  Chunk");
				var chunk = ReadChunk(s);
				Console.WriteLine($"    Size: {chunk.Data.Count}");
				Console.WriteLine($"    Type: {chunk.Type}");
				Console.WriteLine($"    CRC: 0x{BinaryPrimitives.ReadUInt32BigEndian(chunk.CRC.ToArray()):X4}");
				
				switch (chunk.Type)
				{
					case "IHDR":
						{
							var data = ParseIHDRChunk(chunk);
							Console.WriteLine("    Data");
							Console.WriteLine($"      Width: {data.Width}");
							Console.WriteLine($"      Height: {data.Height}");
							Console.WriteLine($"      Bit depth: {data.BitDepth}");
							Console.WriteLine($"      Color type: {data.ColorType}");
							Console.WriteLine($"      Compression method: {data.CompressionMethod}");
							Console.WriteLine($"      Filter method: {data.FilterMethod}");
							Console.WriteLine($"      Interlace method: {data.InterlaceMethod}");
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
