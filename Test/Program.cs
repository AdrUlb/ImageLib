using ImageLib;
using ManagedSDL2;
using System.Drawing;

namespace Test
{
	class Program
	{
		static void Main()
		{
			var image = new Image("shapes.png");

			using var window = new SDL.Window("Test", SDL.WindowPos.Undefined, SDL.WindowPos.Undefined, image.Width, image.Height);
			using var renderer = new SDL.Renderer(window);
			using var texture = new SDL.Texture(renderer, SDL.PixelFormat.RGBA8888, SDL.TextureAccess.Streaming, image.Width, image.Height);

			(var pixelsPtr, var pitch) = texture.Lock();

			unsafe
			{
				var __unsafe__pixelsPtr = (byte*)pixelsPtr.ToPointer();

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						var pixelIndex = x * 4 + y * pitch;

						var pixel = image.GetPixel(x, y);

						__unsafe__pixelsPtr[pixelIndex + 0] = pixel.A;
						__unsafe__pixelsPtr[pixelIndex + 1] = pixel.B;
						__unsafe__pixelsPtr[pixelIndex + 2] = pixel.G;
						__unsafe__pixelsPtr[pixelIndex + 3] = pixel.R;
					}
				}
			}
			
			texture.Unlock();

			renderer.Copy(texture);

			var running = true;

			window.CloseRequested += () =>
			{
				window.Hide();
				running = false;
			};

			while (running)
			{
				SDL.ProcessEvents();

				renderer.Present();
			}
		}
	}
}
