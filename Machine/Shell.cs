using System.Reflection;

namespace BlackBox.Machine;

//defines the shell and contains shell functions for hostspace

public static class Shell
{
	private static string _inputBuffer = "";
	private static bool _enabled = false;
	private static int _cursorPosition = 0;
	private static readonly List<string> _history = new();
	private static int _historyIndex = -1;

	public static void Enable()
	{
		_enabled = true;
		ShowPrompt();
	}

	public static void Disable()
	{
		_enabled = false;
	}

	public static bool IsEnabled => _enabled;

	private static void ShowPrompt()
	{
		Window.Write("\n> ");
	}

	/// <summary>
	/// Process keyboard input for the REPL
	/// </summary>
	public static void ProcessInput()
	{
		if (!_enabled) return;

		// Get keyboard input from Raylib
		int key = Raylib_cs.Raylib.GetCharPressed();

		while (key > 0)
		{
			// Printable character
			if (key >= 32 && key <= 126)
			{
				char c = (char)key;
				_inputBuffer = _inputBuffer.Insert(_cursorPosition, c.ToString());
				_cursorPosition++;
				Window.Write(c.ToString());
			}

			key = Raylib_cs.Raylib.GetCharPressed();
		}

		// Handle special keys
		if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Enter))
		{
			ExecuteLine();
		}
		else if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Backspace))
		{
			if (_cursorPosition > 0 && _inputBuffer.Length > 0)
			{
				_inputBuffer = _inputBuffer.Remove(_cursorPosition - 1, 1);
				_cursorPosition--;
				// Redraw line
				RedrawInputLine();
			}
		}
		else if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Up))
		{
			NavigateHistory(-1);
		}
		else if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Down))
		{
			NavigateHistory(1);
		}
		else if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Left))
		{
			if (_cursorPosition > 0)
			{
				_cursorPosition--;
			}
		}
		else if (Raylib_cs.Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Right))
		{
			if (_cursorPosition < _inputBuffer.Length)
			{
				_cursorPosition++;
			}
		}
	}

	private static void ExecuteLine()
	{
		Window.Write("\n");

		if (string.IsNullOrWhiteSpace(_inputBuffer))
		{
			ShowPrompt();
			return;
		}

		// Add to history
		_history.Add(_inputBuffer);
		_historyIndex = _history.Count;

		string code = _inputBuffer.Trim();
		_inputBuffer = "";
		_cursorPosition = 0;

		// Handle special commands
		var task = Sandbox.Execute(code);
		task.Wait();
		var result = task.Result;

		if (result.Success)
		{
			if (result.ReturnValue != null)
			{
				Window.Write($"=> {result.ReturnValue}\n");
			}
		}
		else
		{
			Window.Write($"Error: {result.ErrorMessage}\n");
		}

		ShowPrompt();
	}

	private static void NavigateHistory(int direction)
	{
		if (_history.Count == 0) return;

		int newIndex = _historyIndex + direction;

		if (newIndex >= 0 && newIndex < _history.Count)
		{
			_historyIndex = newIndex;
			_inputBuffer = _history[_historyIndex];
			_cursorPosition = _inputBuffer.Length;
			RedrawInputLine();
		}
		else if (newIndex >= _history.Count)
		{
			_historyIndex = _history.Count;
			_inputBuffer = "";
			_cursorPosition = 0;
			RedrawInputLine();
		}
	}

	private static void RedrawInputLine()
	{
		// Clear current line by moving cursor back and writing spaces
		// This is a simplified version - in a real terminal we'd use ANSI codes
		// For now, just rewrite the prompt and buffer
		Window.Write($"\r> {_inputBuffer}");
	}
}
