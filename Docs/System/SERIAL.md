# Serial

**`System/Serial.cs`** | `BlackBox.System` | ✅ Complete

Low-level console I/O for debugging (writes to stdout).

## API

```csharp
Serial.Write(string s)       // Write to stdout
string Read(string s)        // Get buffer (param unused)
string ConsoleBuffer         // Public buffer field
```

## Usage

```csharp
Serial.Write("Debug message\n");
Serial.Write($"Value: {x}\n");
var buffer = Serial.ConsoleBuffer;
```

## Serial vs Terminal

- **Serial** → stdout (debugging, logging)
- **Terminal** → graphical window (user-facing)
