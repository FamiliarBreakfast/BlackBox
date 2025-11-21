namespace BlackBox.Machine;

public static class Host
{
	static Host()
	{
		Window.Write("\n\nShell.Help() for available syscalls\n");
		Shell.Enable();
	}

	public static void Loop()
	{
		Shell.ProcessInput();

		//todo:
		//check sandbox status and error/crash handling
		//update timed events
		//update peripherals/filesystem
		//update serial
		//Sandbox.Run() - now uses continuous execution loop
	}
}