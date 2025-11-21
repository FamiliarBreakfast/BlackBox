using Raylib_cs;
using System.Numerics;

namespace BlackBox;

/// <summary>
/// Raylib window with terminal rendering and shader support (hostspace)
/// </summary>
public static class Window
{
	private static Terminal? _terminal;
	private static RenderTexture2D _renderTexture;
	private static Font _font;

	private static int _charWidth = 8;
	private static int _charHeight = 16;
	private static int _windowWidth;
	private static int _windowHeight;

	private static bool _showCursor = true;
	private static float _cursorBlinkTime = 0;
	private static readonly float _cursorBlinkRate = 0.5f;

	// Shader support for post-processing
	private static Shader _postProcessShader;
	private static bool _useShader = false;

	public static void Initialize(int terminalWidth = 80, int terminalHeight = 25, string title = "Black Box", string fontPath = "./JetBrainsMono-Regular.ttf", int fontSize = 16)
	{
		_terminal = new Terminal(terminalWidth, terminalHeight);
		_charHeight = fontSize;

		// Initialize window first to enable font loading
		Raylib.InitWindow(800, 600, title); // Temporary size
		Raylib.SetTargetFPS(60);

		// Load JetBrainsMono font
		if (File.Exists(fontPath))
		{
			_font = Raylib.LoadFontEx(fontPath, _charHeight, null, 0);
			Raylib.SetTextureFilter(_font.Texture, TextureFilter.Anisotropic16X);
		}
		else
		{
			Console.WriteLine("Could not find font file: " + fontPath);
			_font = Raylib.GetFontDefault();
		}

		// Measure character dimensions for the loaded font
		var testChar = Raylib.MeasureTextEx(_font, "M", _charHeight, 0);
		_charWidth = (int)testChar.X;

		// Now set the correct window size
		_windowWidth = terminalWidth * _charWidth;
		_windowHeight = terminalHeight * _charHeight;
		Raylib.SetWindowSize(_windowWidth, _windowHeight);

		// Create render texture for shader effects
		_renderTexture = Raylib.LoadRenderTexture(_windowWidth, _windowHeight);
	}

	public static void LoadShader(string fragmentShaderPath)
	{
		if (File.Exists(fragmentShaderPath))
		{
			_postProcessShader = Raylib.LoadShader(null, fragmentShaderPath);
			_useShader = true;
		}
	}

	public static void UnloadShader()
	{
		if (_useShader)
		{
			Raylib.UnloadShader(_postProcessShader);
			_useShader = false;
		}
	}

	public static bool ShouldClose()
	{
		return Raylib.WindowShouldClose();
	}

	public static void BeginFrame()
	{
		// Begin rendering to texture for post-processing
		Raylib.BeginTextureMode(_renderTexture);
		Raylib.ClearBackground(Color.Black);
	}

	public static void ProcessScrolling()
	{
		if (_terminal == null) return;

		// Check for PageUp/PageDown keys
		if (Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.PageUp))
		{
			_terminal.PageUp();
		}
		else if (Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.PageDown))
		{
			_terminal.PageDown();
		}

		// Check for Ctrl+Up/Ctrl+Down
		bool ctrlPressed = Raylib.IsKeyDown(Raylib_cs.KeyboardKey.LeftControl) ||
		                   Raylib.IsKeyDown(Raylib_cs.KeyboardKey.RightControl);

		if (ctrlPressed)
		{
			if (Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Up))
			{
				_terminal.PageUp();
			}
			else if (Raylib.IsKeyPressed(Raylib_cs.KeyboardKey.Down))
			{
				_terminal.PageDown();
			}
		}
	}

	public static void Render()
	{
		if (_terminal == null) return;

		// Render terminal contents
		for (int y = 0; y < _terminal.Height; y++)
		{
			for (int x = 0; x < _terminal.Width; x++)
			{
				var bgColor = _terminal.GetBackgroundColor(x, y);
				var fgColor = _terminal.GetForegroundColor(x, y);
				var ch = _terminal.GetChar(x, y);

				int posX = x * _charWidth;
				int posY = y * _charHeight;

				// Draw background
				Raylib.DrawRectangle(posX, posY, _charWidth, _charHeight,
					new Color((int)bgColor.r, bgColor.g, bgColor.b, 255));

				// Draw character
				if (ch != ' ')
				{
					Raylib.DrawTextEx(_font, ch.ToString(),
						new Vector2(posX, posY),
						_charHeight,
						0,
						new Color((int)fgColor.r, fgColor.g, fgColor.b, 255));
				}
			}
		}

		// Draw cursor (only if visible in viewport)
		_cursorBlinkTime += Raylib.GetFrameTime();
		if (_cursorBlinkTime >= _cursorBlinkRate)
		{
			_showCursor = !_showCursor;
			_cursorBlinkTime = 0;
		}

		if (_showCursor)
		{
			int cursorScreenY = _terminal.CursorY - _terminal.ViewportOffset;

			// Only draw cursor if it's within the visible viewport
			if (cursorScreenY >= 0 && cursorScreenY < _terminal.Height)
			{
				int cursorX = _terminal.CursorX * _charWidth;
				int cursorY = (cursorScreenY + 1) * _charHeight - 5;
				Raylib.DrawRectangle(cursorX, cursorY, _charWidth, 2, Color.White);
			}
		}

		Raylib.EndTextureMode();
	}

	public static void EndFrame()
	{
		// Render texture to screen (with optional shader)
		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.Black);

		if (_useShader)
		{
			Raylib.BeginShaderMode(_postProcessShader);
		}

		// Draw render texture (flip Y because OpenGL)
		Raylib.DrawTextureRec(
			_renderTexture.Texture,
			new Rectangle(0, 0, _renderTexture.Texture.Width, -_renderTexture.Texture.Height),
			new Vector2(0, 0),
			Color.White
		);

		if (_useShader)
		{
			Raylib.EndShaderMode();
		}

		Raylib.EndDrawing();
	}

	public static void Close()
	{
		Raylib.UnloadRenderTexture(_renderTexture);
		Raylib.UnloadFont(_font);
		if (_useShader)
		{
			Raylib.UnloadShader(_postProcessShader);
		}
		Raylib.CloseWindow();
	}

	public static Terminal? GetTerminal()
	{
		return _terminal;
	}

	public static void Write(string text)
	{
		_terminal?.Write(text);
	}
}
