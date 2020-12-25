using System;
using System.Drawing;
using System.IO;

namespace ImageLib
{
	public class Image
	{
		public int Width { get; private set; } = 0;
		public int Height { get; private set; } = 0;

		Color[][] pixels;

		private Color[][] Init(Stream s)
		{
			if (PNG.ValidateHeader(s))
			{
				var pixels = PNG.DecodeImage(s);
				Height = pixels.Length;
				if (Height > 0)
					Width = pixels[0].Length;

				return pixels;
			}
			else
			{
				throw new NotSupportedException("Image format not supported");
			}
		}

		public Image(Stream s)
		{
			pixels = Init(s);
		}

		public Image(string path)
		{
			using var fs = File.OpenRead(path);
			pixels = Init(fs);
		}

		public Color GetPixel(int x, int y)
		{
			return pixels[y][x];
		}
	}
}
