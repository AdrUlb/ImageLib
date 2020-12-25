using System;
using System.IO;

namespace ImageLib
{
	public class Image
	{
		public int Width { get; }
		public int Height { get; }

		private static void Init(Stream s)
		{
			if (PNG.ValidateHeader(s))
			{
				PNG.DecodeImage(s);
			}
			else
			{
				throw new NotSupportedException("Image format not supported");
			}
		}

		public Image(Stream s)
		{
			Init(s);
		}

		public Image(string path)
		{
			using var fs = File.OpenRead(path);
			Init(fs);
		}
	}
}
