# Terminal

Two components: Hostspace emulator + Userspace API

## Hostspace (`BlackBox.Terminal` in `Terminal.cs`)

VT100-like terminal emulator with character buffer and color support.

```csharp
Terminal(int width = 80, int height = 25)
void Clear()
void Write(string text)
void SetCursorPosition(int x, int y)
void SetColor(byte r, byte g, byte b)
void SetBackgroundColor(byte r, byte g, byte b)
char GetChar(int x, int y)
```

## Userspace (`BlackBox.System.Terminal` in `System/Terminal.cs`)

API for sandbox code to write to terminal window.

```csharp
Terminal.Write(string text)
Terminal.WriteLine(string text)
Terminal.Clear()
Terminal.SetColor(byte r, byte g, byte b)
Terminal.SetBackgroundColor(byte r, byte g, byte b)
Terminal.ResetColors()
char GetChar(int x, int y)
int GetWidth()
int GetHeight()
```

## Usage

```csharp
Terminal.Write("Hello\n");
Terminal.SetColor(255, 0, 0);
Terminal.WriteLine("Red text");
```

## See Also

[Window](WINDOW.md) | [Serial](SERIAL.md)
