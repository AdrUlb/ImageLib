using ImageLib;
using ManagedSDL2;

namespace Test
{
	class Program
	{
		static void Main()
		{
			var image = new Image("shapes.png");

			var window = new SDL.Window("Test", SDL.WindowPos.Undefined, SDL.WindowPos.Undefined, image.Width, image.Height);
			var renderer = new SDL.Renderer(window);

			var running = true;

			window.CloseRequested += () =>
			{
				window.Hide();
				running = false;
			};

			while (running)
			{
				SDL.ProcessEvents();

				renderer.Clear();

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						renderer.DrawPoint(image.GetPixel(x, y), x, y);
					}
				}

				renderer.Present();
			}

			window.Dispose();
		}
	}
}
