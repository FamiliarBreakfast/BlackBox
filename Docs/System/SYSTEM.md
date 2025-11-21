# System Layer

Userspace-accessible APIs. Auto-imported via `BlackBox.System` namespace.

## Components

| Component | File | Status |
|-----------|------|--------|
| [Window](WINDOW.md) | `Window.cs` (root, hostspace) | ✅ Complete |
| [Terminal](TERMINAL.md) | `System/Terminal.cs` | ✅ Complete |
| [Serial](SERIAL.md) | `System/Serial.cs` | ✅ Complete |
| [IO](IO.md) | `System/IO.cs` | 🚧 Placeholder |
| [Process](PROCESS.md) | `System/Process.cs` | 🚧 Placeholder |
| [Filesystem](Filesystem/FILESYSTEM.md) | `System/Filesystem/` | 🚧 Placeholder |
| [Peripherals](Peripherals/PERIPHERALS.md) | `System/Peripherals/` | 🚧 Placeholder |

## Usage from Userspace

```csharp
// No import needed - BlackBox.System is auto-imported
Terminal.Write("Hello\n");
Serial.Write("Debug\n");
var files = Filesystem.List("/");
int pid = Process.Spawn("code");
```

## Security Model

**Can:** Use System APIs, access virtual filesystem, spawn processes
**Cannot:** Access host filesystem, use unsafe code, access Machine layer
