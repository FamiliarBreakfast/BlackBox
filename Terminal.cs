namespace BlackBox;

/// <summary>
/// Terminal emulator with VT100-like capabilities (hostspace)
/// </summary>
public class Terminal
{
	public int Width { get; private set; }
	public int Height { get; private set; }

	// Scrollback buffer
	private const int ScrollbackLines = 1000;
	private const int TotalBufferLines = ScrollbackLines;

	private char[,] _buffer;
	private (byte r, byte g, byte b)[,] _fgColors;
	private (byte r, byte g, byte b)[,] _bgColors;

	private int _cursorX;
	private int _cursorY; // Cursor position in buffer (not screen)
	public int CursorX => _cursorX;
	public int CursorY => _cursorY;

	// Viewport control
	private int _viewportOffset; // Line offset for scrolling
	private int _contentLines; // Actual lines with content
	public int ViewportOffset => _viewportOffset;

	// Default colors
	private (byte r, byte g, byte b) _defaultFg = (200, 200, 200);
	private (byte r, byte g, byte b) _defaultBg = (0, 0, 0);
	private (byte r, byte g, byte b) _currentFg;
	private (byte r, byte g, byte b) _currentBg;

	public Terminal(int width = 80, int height = 25)
	{
		Width = width;
		Height = height;
		_buffer = new char[TotalBufferLines, width];
		_fgColors = new (byte, byte, byte)[TotalBufferLines, width];
		_bgColors = new (byte, byte, byte)[TotalBufferLines, width];
		_currentFg = _defaultFg;
		_currentBg = _defaultBg;
		Clear();
	}

	public void Clear()
	{
		for (int y = 0; y < TotalBufferLines; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				_buffer[y, x] = ' ';
				_fgColors[y, x] = _defaultFg;
				_bgColors[y, x] = _defaultBg;
			}
		}
		_cursorX = 0;
		_cursorY = 0;
		_viewportOffset = 0;
		_contentLines = 0;
	}

	public void Write(string text)
	{
		// Auto-scroll to cursor when writing
		ScrollToBottom();

		foreach (char c in text)
		{
			WriteChar(c);
		}

		// Track maximum content lines
		_contentLines = Math.Max(_contentLines, _cursorY + 1);
	}

	private void WriteChar(char c)
	{
		switch (c)
		{
			case '\n':
				_cursorX = 0;
				_cursorY++;
				if (_cursorY >= TotalBufferLines)
				{
					ScrollUp();
					_cursorY = TotalBufferLines - 1;
				}
				break;

			case '\r':
				_cursorX = 0;
				break;

			case '\t':
				_cursorX = (_cursorX + 8) & ~7; // Tab to next 8-char boundary
				if (_cursorX >= Width)
				{
					_cursorX = 0;
					_cursorY++;
					if (_cursorY >= TotalBufferLines)
					{
						ScrollUp();
						_cursorY = TotalBufferLines - 1;
					}
				}
				break;

			default:
				if (_cursorX >= Width)
				{
					_cursorX = 0;
					_cursorY++;
					if (_cursorY >= TotalBufferLines)
					{
						ScrollUp();
						_cursorY = TotalBufferLines - 1;
					}
				}

				_buffer[_cursorY, _cursorX] = c;
				_fgColors[_cursorY, _cursorX] = _currentFg;
				_bgColors[_cursorY, _cursorX] = _currentBg;
				_cursorX++;
				break;
		}
	}

	private void ScrollUp()
	{
		// Shift entire buffer up by one line
		for (int y = 0; y < TotalBufferLines - 1; y++)
		{
			for (int x = 0; x < Width; x++)
			{
				_buffer[y, x] = _buffer[y + 1, x];
				_fgColors[y, x] = _fgColors[y + 1, x];
				_bgColors[y, x] = _bgColors[y + 1, x];
			}
		}

		// Clear last line
		for (int x = 0; x < Width; x++)
		{
			_buffer[TotalBufferLines - 1, x] = ' ';
			_fgColors[TotalBufferLines - 1, x] = _defaultFg;
			_bgColors[TotalBufferLines - 1, x] = _defaultBg;
		}

		// Adjust cursor and viewport
		if (_cursorY > 0)
			_cursorY--;
		if (_viewportOffset > 0)
			_viewportOffset--;
		if (_contentLines > 0)
			_contentLines--;
	}

	/// <summary>
	/// Scroll viewport up by one page (Height - 1 lines)
	/// </summary>
	public void PageUp()
	{
		int scrollAmount = Height - 1;
		_viewportOffset = Math.Max(0, _viewportOffset - scrollAmount);
	}

	/// <summary>
	/// Scroll viewport down by one page (Height - 1 lines)
	/// </summary>
	public void PageDown()
	{
		int scrollAmount = Height - 1;
		int maxOffset = Math.Max(0, _contentLines - Height);
		_viewportOffset = Math.Min(maxOffset, _viewportOffset + scrollAmount);
	}

	/// <summary>
	/// Scroll viewport to show cursor (bottom of buffer)
	/// </summary>
	public void ScrollToBottom()
	{
		int maxOffset = Math.Max(0, _contentLines - Height);
		_viewportOffset = maxOffset;
	}

	/// <summary>
	/// Check if viewport is at the bottom (following cursor)
	/// </summary>
	public bool IsAtBottom()
	{
		int maxOffset = Math.Max(0, _contentLines - Height);
		return _viewportOffset >= maxOffset;
	}

	public void SetCursorPosition(int x, int y)
	{
		_cursorX = Math.Clamp(x, 0, Width - 1);
		_cursorY = Math.Clamp(y, 0, Height - 1);
	}

	public void SetColor(byte r, byte g, byte b)
	{
		_currentFg = (r, g, b);
	}

	public void SetBackgroundColor(byte r, byte g, byte b)
	{
		_currentBg = (r, g, b);
	}

	public void ResetColors()
	{
		_currentFg = _defaultFg;
		_currentBg = _defaultBg;
	}

	public char GetChar(int x, int y)
	{
		int bufferY = y + _viewportOffset;
		if (x < 0 || x >= Width || y < 0 || y >= Height || bufferY >= TotalBufferLines)
			return ' ';
		return _buffer[bufferY, x];
	}

	public (byte r, byte g, byte b) GetForegroundColor(int x, int y)
	{
		int bufferY = y + _viewportOffset;
		if (x < 0 || x >= Width || y < 0 || y >= Height || bufferY >= TotalBufferLines)
			return _defaultFg;
		return _fgColors[bufferY, x];
	}

	public (byte r, byte g, byte b) GetBackgroundColor(int x, int y)
	{
		int bufferY = y + _viewportOffset;
		if (x < 0 || x >= Width || y < 0 || y >= Height || bufferY >= TotalBufferLines)
			return _defaultBg;
		return _bgColors[bufferY, x];
	}
}
