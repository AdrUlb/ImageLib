using ImageLib;
using ManagedSDL2;

namespace Test
{
	class Program
	{
		static void Main()
		{
			const float scale = 1f;

			var image = new Image("C:\\Users\\Adrian\\Desktop\\screen.png");

			using var window = new SDL.Window("Test", SDL.WindowPos.Undefined, SDL.WindowPos.Undefined, (int)(image.Width * scale), (int)(image.Height * scale));
			//using var renderer = new SDL.Renderer(window);
			var surface = window.GetSurface();

			var running = true;

			window.CloseRequested += () =>
			{
				window.Hide();
				running = false;
			};

			//renderer.Scale = (scale, scale);

			while (running)
			{
				SDL.ProcessEvents();

				/*renderer.Clear(System.Drawing.Color.White);

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						renderer.DrawPoint(image.GetPixel(x, y), x, y);
					}
				}

				renderer.Present();*/
			}
		}
	}
}
