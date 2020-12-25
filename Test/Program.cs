using ImageLib;
using ManagedSDL2;

namespace Test
{
	class Program
	{
		static void Main()
		{
			var image = new Image("test.png");

			var window = new SDL.Window("Test", SDL.WindowPos.Undefined, SDL.WindowPos.Undefined, 300, 300);
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

				renderer.Present();
			}

			window.Dispose();
		}
	}
}
