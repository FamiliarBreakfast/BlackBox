using BlackBox;
using BlackBox.Machine;

Window.Initialize(fontSize: 24);

while (!Window.ShouldClose())
{
	Window.BeginFrame();

	Window.ProcessScrolling();

	Host.Loop();

	Window.Render();
	Window.EndFrame();
}

Window.Close();
