# IO

**`System/IO.cs`** | `BlackBox.System` | 🚧 Placeholder

High-level I/O operations for userspace.

## Planned API

```csharp
// Input
string? ReadLine()
string? ReadLine(string prompt)
char ReadChar()
bool HasInput()

// Output
WriteLine(string text)
Write(string format, params object[] args)
WriteError(string text)

// File operations
string ReadAllText(string path)
WriteAllText(string path, string content)
IEnumerable<string> ReadLines(string path)
AppendAllText(string path, string content)

// Console control
Clear()
SetCursorPosition(int left, int top)
SetForeground(ConsoleColor color)
SetBackground(ConsoleColor color)
int Width { get; }
int Height { get; }
```

## IO vs Serial

- **Serial** - Low-level debugging output
- **IO** - High-level user interaction

## See Also

[Serial](SERIAL.md) | [Filesystem](Filesystem/FILESYSTEM.md)
