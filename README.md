# BlackBox

C# scripting sandbox built on Roslyn with strict userspace/hostspace separation.

## Features

- **Roslyn sandbox** - Secure C# code execution
- **Virtual filesystem** - Sandboxed file operations
- **Process management** - Spawn subprocesses on separate threads
- **System APIs** - Controlled userspace access
- **Terminal** - Raylib window with shader support
- **REPL** - Interactive C# shell

## Architecture

```
┌─────────────────────────────────┐
│  Machine (Hostspace)            │
│  ├─→ Host - Main loop           │
│  ├─→ Sandbox - PID 0            │
│  └─→ Shell - REPL               │
└─────────────────────────────────┘
           │ Security Boundary
┌──────────▼──────────────────────┐
│  System (Userspace)             │
│  ├─→ Terminal, Serial           │
│  ├─→ Filesystem, Process        │
│  └─→ IO, Peripherals            │
└─────────────────────────────────┘
```

## Quick Start

```csharp
// Execute code
var result = await Sandbox.Execute("var x = 10; return x * 2;");

// Spawn subprocess
int pid = Sandbox.Spawn("Serial.Write(\"Hello\");");
await Sandbox.Wait(pid);

// Run main loop
Sandbox.Run(() => { /* per-iteration logic */ });
```

## Status

| Component | Status |
|-----------|--------|
| Sandbox, Terminal, Window, Serial, Shell | ✅ Complete |
| Host, Filesystem, Process, IO, Peripherals | 🚧 Placeholder |

## Documentation

***Note:** Documentation AI generated*

**[Docs/MAIN.md](Docs/MAIN.md)** - Complete documentation

- [Machine Layer](Docs/Machine/MACHINE.md) - Host, Sandbox, Shell
- [System Layer](Docs/System/SYSTEM.md) - Userspace APIs
- [Files](Docs/Files/FILES.md) - Filesystem structure

## Requirements

- .NET 9.0
- Microsoft.CodeAnalysis.CSharp.Scripting 4.12.0

## Security

- No unsafe code, overflow checking enabled
- Limited assembly access
- Virtual filesystem (no direct host access)
- Sandboxed execution
