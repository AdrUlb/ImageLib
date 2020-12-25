﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ImageLib
{
	public static class PNG
	{
		enum ColorType : byte
		{
			Grayscale = 0,
			RGB = 2,
			Indexed = 3,
			GrayscaleAlpha = 4,
			RGBA = 6
		}

		enum CompressionMethod : byte
		{
			Deflate = 0
		}

		enum FilterMethod : byte
		{
			Prediction
		}

		enum InterlaceMethod : byte
		{
			None = 0,
			Adam7 = 1
		}

		enum ScanlineFilter : byte
		{
			None = 0,
			Sub = 1,
			Up = 2,
			Average = 3,
			Paeth = 4
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

		public static Color[][] DecodeImage(Stream s)
		{
			Console.WriteLine($"Decoding PNG");

			var end = false;

			var chunk = ReadChunk(s);
			if (chunk.Type != "IHDR" || chunk.Data.Count != 13)
				throw new FormatException("PNG file invalid or corrupted");

			Console.WriteLine($"  Read chunk: {chunk.Type} ({chunk.Data.Count} bytes)");
			var data = new Span<byte>(chunk.Data.ToArray());

			var width = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(0, 4));
			var height = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4));
			var bitDepth = data[8];
			var colorType = (ColorType)data[9];
			var compressionMethod = (CompressionMethod)data[10];
			var filterMethod = (FilterMethod)data[11];
			var interlaceMethod = (InterlaceMethod)data[12];

			if (!Enum.IsDefined(typeof(CompressionMethod), compressionMethod))
				throw new FormatException("Invalid compression method");

			if (!Enum.IsDefined(typeof(FilterMethod), filterMethod))
				throw new FormatException("Invalid filter method");

			if (!Enum.IsDefined(typeof(InterlaceMethod), interlaceMethod))
				throw new FormatException("Invalid interlace method");

			Console.WriteLine($"    Width: {width}");
			Console.WriteLine($"    Height: {height}");
			Console.WriteLine($"    Bit depth: {bitDepth}");
			Console.WriteLine($"    Color type: {colorType}");
			Console.WriteLine($"    Compression method: {compressionMethod}");
			Console.WriteLine($"    Filter method: {filterMethod}");
			Console.WriteLine($"    Interlace method: {interlaceMethod}");

			using var compressedImageData = new MemoryStream();

			while (!end)
			{
				chunk = ReadChunk(s);
				Console.WriteLine($"  Read chunk: {chunk.Type} ({chunk.Data.Count} bytes)");

				switch (chunk.Type)
				{
					case "IHDR":
						throw new Exception("    Duplicate IHDR chunk");
					case "IDAT":
						compressedImageData.Write(chunk.Data.ToArray(), 0, chunk.Data.Count);
						break;
					case "tEXt":
						{
							data = new Span<byte>(chunk.Data.ToArray());

							for (var i = 0; i < data.Length; i++)
							{
								if (data[i] == 0)
								{
									var key = data.Slice(0, i);
									var value = data.Slice(i);
									Console.WriteLine($"    {Encoding.Latin1.GetString(key)}: {Encoding.Latin1.GetString(value)}");
									break;
								}
							}
						}
						break;
					case "IEND":
						end = true;
						break;
					default:
						Console.WriteLine("    [UNKNOWN CHUNK TYPE]");
						break;
				}
			}

			Console.WriteLine("  Decompressing image data");
			Console.WriteLine($"    Compressed size: {compressedImageData.Length} bytes");

			using var imageData = new MemoryStream();

			switch (compressionMethod)
			{
				case CompressionMethod.Deflate:
					DeflateStream ds;
					compressedImageData.Position = 0;
					{
						// This is doing some weird ass janky shit to get access to an internal DeflateStream constructor
						var args = new object[] { compressedImageData, CompressionMode.Decompress, false, 15, -1 };
						var argTypes = new Type[] { typeof(Stream), typeof(CompressionMode), typeof(bool), typeof(int), typeof(long) };
						var constructor = typeof(DeflateStream).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, argTypes, null)!;
						ds = (DeflateStream)constructor.Invoke(args);
					}
					ds.CopyTo(imageData);
					ds.Dispose();
					break;
				default:
					throw new NotImplementedException("Compression method not implemented");
			}

			var bytesPerPixel = 0;

			switch (colorType)
			{
				case ColorType.RGB:
					bytesPerPixel = 3;
					break;
				case ColorType.RGBA:
					bytesPerPixel = 4;
					break;
				default:
					throw new NotImplementedException("Color type not implemented");
			}

			var scanlineWidth = width * bytesPerPixel + 1;
			var expectedSize = scanlineWidth * height;

			Console.WriteLine($"    Expected Decompressed size: {expectedSize} bytes");
			Console.WriteLine($"    Decompressed size: {imageData.Length} bytes");

			if (expectedSize != imageData.Length)
				throw new FormatException("Image data size mismatch");

			Console.WriteLine("  Applying scanline filters");

			var imagePixels = new Color[height][];

			switch (filterMethod)
			{
				case FilterMethod.Prediction:
					for (var y = 0; y < height; y++)
					{
						var lineStart = y * scanlineWidth;
						imageData.Position = lineStart;
						var lineFilter = (ScanlineFilter)imageData.ReadByte();

						var linePixels = new Color[width];
						imagePixels[y] = linePixels;

						var pixelLeft = Color.FromArgb(0, 0, 0, 0);

						switch (lineFilter)
						{
							case ScanlineFilter.Sub:
								for (var x = 0; x < width; x++)
								{
									var r = imageData.ReadByte();
									var g = imageData.ReadByte();
									var b = imageData.ReadByte();
									var a = bytesPerPixel > 3 ? imageData.ReadByte() : 255;

									r += pixelLeft.R;
									g += pixelLeft.G;
									b += pixelLeft.B;
									a += pixelLeft.A;

									r %= 255;
									g %= 255;
									b %= 255;
									a %= 255;

									var pixel = Color.FromArgb(a, r, g, b);
									linePixels[x] = pixel;
									pixelLeft = pixel;
								}
								break;
							case ScanlineFilter.Up:
								for (var x = 0; x < width; x++)
								{
									var r = imageData.ReadByte();
									var g = imageData.ReadByte();
									var b = imageData.ReadByte();
									var a = bytesPerPixel > 3 ? imageData.ReadByte() : 255;

									var pixelUp = y > 0 ? imagePixels[y - 1][x] : Color.FromArgb(0, 0, 0, 0);

									r += pixelUp.R;
									g += pixelUp.R;
									b += pixelUp.R;
									a += pixelUp.R;

									r %= 255;
									g %= 255;
									b %= 255;
									a %= 255;

									var pixel = Color.FromArgb(a, r, g, b);
									linePixels[x] = pixel;
									pixelLeft = pixel;
								}
								break;
							default:
								Console.WriteLine($"    WARNING: Unknown scanline filter {lineFilter} for line {y + 1}");
								break;
						}
					}
					break;
				default:
					throw new NotImplementedException($"Filter method {filterMethod} not implemented");
			}
			Console.WriteLine(imageData.ReadByte() == -1);

			return imagePixels;
		}
	}
}
