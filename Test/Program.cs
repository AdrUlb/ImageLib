﻿using ManagedSDL2;
using System.Drawing;

namespace Test
{
	class Program
	{
		static void Main(string[] args)
		{
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
